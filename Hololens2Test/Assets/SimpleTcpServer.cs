using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// Delegate: Equivalent to Python Callable[[bytes], bytes]
/// </summary>
/// <param name="request">Original client request</param>
/// <returns>Server response data</returns>
public delegate byte[] BytesToBytes(byte[] request);

/// <summary>
/// C# Version of SimpleTcpServer
/// Fully aligned with Python logic: non-blocking, connection pool, timeout cleanup, $$ protocol, escape parsing
/// </summary>
public class SimpleTcpServer : IDisposable
{
    #region Private Fields
    private readonly string _host;
    private readonly int _port;
    private readonly BytesToBytes _workerFunction;
    private readonly byte[] _quitToken;
    private readonly int _maxListen;
    private readonly double _clientTimeout;
    private readonly int _maxBuffer = Utils.MAX_BUFFER;

    private Socket _serverSocket;
    private bool _running;

    private readonly Dictionary<IPEndPoint, Socket> _connPool = new();
    private readonly Dictionary<IPEndPoint, byte[]> _connBuff = new();
    private readonly Dictionary<IPEndPoint, DateTime> _lastSeen = new();
    #endregion

    #region Constructor
    public SimpleTcpServer(
        string host,
        int port,
        BytesToBytes workerFunction,
        byte[] quitToken,
        int maxListen = 5,
        double clientTimeout = 10.0)
    {
        _host = host;
        _port = port;
        _workerFunction = workerFunction;
        _quitToken = quitToken;
        _maxListen = maxListen;
        _clientTimeout = clientTimeout;

        _running = true;
        InitServerSocket();
    }
    #endregion

    #region Initialize Socket
    private void InitServerSocket()
    {
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(_host), _port));
        _serverSocket.Listen(_maxListen);
        _serverSocket.Blocking = false;
    }
    #endregion

    #region Close Single Connection
    private void ConnClose(IPEndPoint addr)
    {
        if (!_connPool.ContainsKey(addr)) return;

        try
        {
            _connPool[addr].Shutdown(SocketShutdown.Both);
            _connPool[addr].Close();
        }
        catch { }

        _connPool.Remove(addr);
        _connBuff.Remove(addr);
        _lastSeen.Remove(addr);
    }
    #endregion

    #region Handle New Connection
    private void HandleClient(Socket conn, IPEndPoint addr)
    {
        if (addr == null) return;

        if (_connPool.ContainsKey(addr))
            ConnClose(addr);

        conn.Blocking = false;
        _connPool[addr] = conn;
        _connBuff[addr] = Array.Empty<byte>();
        _lastSeen[addr] = DateTime.Now;
    }
    #endregion

    #region Try Accept New Connections
    private void TryAccept()
    {
        if (_serverSocket == null)
        {
            return;
        }
        try
        {
            if (_serverSocket.Poll(0, SelectMode.SelectRead))
            {
                Socket client = _serverSocket.Accept();
                IPEndPoint remote = client.RemoteEndPoint as IPEndPoint;
                HandleClient(client, remote);
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
        { }
        catch { }
    }
    #endregion

    #region Read Client Data Once
    private bool AcquireOnce(IPEndPoint addr)
    {
        if (!_connPool.ContainsKey(addr)) return true;
        Socket conn = _connPool[addr];
        bool died = false;

        try
        {
            if (conn.Available <= 0) return false;

            byte[] buffer = new byte[_maxBuffer];
            int read = conn.Receive(buffer);

            if (read <= 0)
                died = true;
            else
            {
                byte[] data = new byte[read];
                Buffer.BlockCopy(buffer, 0, data, 0, read);
                _connBuff[addr] = _connBuff[addr].Concat(data).ToArray();
                _lastSeen[addr] = DateTime.Now;
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
        { died = false; }
        catch
        { died = true; }

        return died;
    }
    #endregion

    #region Count Occurrences of Terminator $$
    private int CountBytes(byte[] data, byte[] sub)
    {
        if (data == null || sub == null || sub.Length == 0) return 0;
        int count = 0;
        int i = 0;
        int len = sub.Length;

        while (i <= data.Length - len)
        {
            bool match = true;
            for (int j = 0; j < len; j++)
                if (data[i + j] != sub[j]) { match = false; break; }

            if (match) { count++; i += len; }
            else i++;
        }
        return count;
    }
    #endregion

    #region Calculate and Respond to Client
    private bool CalcAndResp(IPEndPoint addr, byte[] msgNow)
    {
        bool died = false;
        try
        {
            byte[] msgGet = Utils.Unescape(msgNow);
            byte[] retAns;

            try { retAns = _workerFunction(msgGet); }
            catch (Exception ex) { retAns = System.Text.Encoding.UTF8.GetBytes(ex.Message); }

            byte[] sendAns = Utils.Escape(retAns);
            byte[] eoq = Utils.Eoq();
            byte[] fullResp = new byte[sendAns.Length + eoq.Length];
            Buffer.BlockCopy(sendAns, 0, fullResp, 0, sendAns.Length);
            Buffer.BlockCopy(eoq, 0, fullResp, sendAns.Length, eoq.Length);

            _connPool[addr].Send(fullResp);
            _lastSeen[addr] = DateTime.Now;
        }
        catch { died = true; }

        return died;
    }
    #endregion

    #region Respond to Client Message Once
    private bool ResponseOnce(IPEndPoint addr)
    {
        if (!_connBuff.ContainsKey(addr)) return true;
        byte[] buff = _connBuff[addr];
        if (buff.Length == 0) return false;

        byte[] eoq = Utils.Eoq();
        if (CountBytes(buff, eoq) == 0) return false;

        var (msgNow, remaining) = SplitOnce(buff, eoq);
        _connBuff[addr] = remaining;

        if (CompareBytes(msgNow, _quitToken))
        {
            _running = false;
            return false;
        }

        return CalcAndResp(addr, msgNow);
    }
    #endregion

    #region Timeout Check
    private bool CheckTimeout(IPEndPoint addr)
    {
        if (!_lastSeen.ContainsKey(addr)) return true;
        return (DateTime.Now - _lastSeen[addr]).TotalSeconds >= _clientTimeout;
    }
    #endregion

    #region Iterate All Clients
    private void CheckAll(Func<IPEndPoint, bool> worker)
    {
        var allAddr = new List<IPEndPoint>(_connPool.Keys);
        foreach (var addr in allAddr)
            if (worker(addr))
                ConnClose(addr);
    }
    #endregion

    #region Batch Operations
    private void AcquireAll() => CheckAll(AcquireOnce);
    private void ResponseAll() => CheckAll(ResponseOnce);
    private void KickTimeout() => CheckAll(CheckTimeout);
    #endregion

    #region Kick All Clients
    private void KickAll()
    {
        var allAddr = new List<IPEndPoint>(_connPool.Keys);
        foreach (var addr in allAddr) ConnClose(addr);
    }
    #endregion

    #region Main Loop
    public void MainLoop()
    {
        while (_running)
        {
            TryAccept();
            AcquireAll();
            ResponseAll();
            KickTimeout();
        }
        KickAll();
    }
    #endregion

    #region Utility: Split Once
    private (byte[] part1, byte[] part2) SplitOnce(byte[] buffer, byte[] separator)
    {
        int sepLen = separator.Length;
        for (int i = 0; i <= buffer.Length - sepLen; i++)
        {
            bool match = true;
            for (int j = 0; j < sepLen; j++)
                if (buffer[i + j] != separator[j]) { match = false; break; }

            if (match)
            {
                byte[] part1 = new byte[i];
                byte[] part2 = new byte[buffer.Length - i - sepLen];
                Buffer.BlockCopy(buffer, 0, part1, 0, i);
                Buffer.BlockCopy(buffer, i + sepLen, part2, 0, part2.Length);
                return (part1, part2);
            }
        }
        return (buffer, Array.Empty<byte>());
    }
    #endregion

    #region Utility: Compare Bytes
    private bool CompareBytes(byte[] a, byte[] b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
    #endregion

    #region Dispose
    public void Dispose()
    {
        _running = false;
        KickAll();

        if (_serverSocket != null)
        {
            _serverSocket.Close();
            _serverSocket.Dispose();
        }
    }
    #endregion
}