using System;
using System.IO;
using System.Net.Sockets;

/// <summary>
/// Universal TCP Client for .NET
/// Functionality is fully consistent with the Python version: persistent connection, escaping, terminator, exception safety, packet sticking handling
/// </summary>
public class SimpleTcpClient : IDisposable
{
    private readonly Socket _socket;
    private readonly MemoryStream _receiveBuffer;
    public bool IsConnected { get; private set; }

    public SimpleTcpClient(string host, int port)
    {
        _receiveBuffer = new MemoryStream();
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        try
        {
            _socket.Connect(host, port);
            IsConnected = true;
        }
        catch
        {
            IsConnected = false;
        }
    }

    /// <summary>
    /// Core Method: Send a request and wait for a response (fully consistent with Python request)
    /// </summary>
    public byte[] Request(byte[] msg)
    {
        if (!IsConnected) return null;

        try
        {
            // 1. Escape + Append terminator $$
            byte[] sendData = Utils.Escape(msg);
            byte[] eoq = Utils.Eoq();
            byte[] fullMsg = new byte[sendData.Length + eoq.Length];
            Buffer.BlockCopy(sendData, 0, fullMsg, 0, sendData.Length);
            Buffer.BlockCopy(eoq, 0, fullMsg, sendData.Length, eoq.Length);

            // 2. Send all data
            _socket.Send(fullMsg);
        }
        catch
        {
            Close();
            return null;
        }

        // 3. Receive in a loop until $$ is received
        byte[] tempBuffer = new byte[Utils.MAX_BUFFER];
        byte[] endMarker = Utils.Eoq();

        try
        {
            while (!BufferContains(_receiveBuffer.ToArray(), endMarker) && IsConnected)
            {
                int read = _socket.Receive(tempBuffer);
                if (read <= 0)
                {
                    Close();
                    return null;
                }

                _receiveBuffer.Write(tempBuffer, 0, read);
            }
        }
        catch
        {
            Close();
            return null;
        }

        // 4. Split the message by $$ (split only once)
        byte[] totalBuffer = _receiveBuffer.ToArray();
        var (response, remaining) = SplitOnce(totalBuffer, endMarker);

        // Reset the buffer
        _receiveBuffer.SetLength(0);
        _receiveBuffer.Write(remaining, 0, remaining.Length);

        // 5. Unescape and return
        return Utils.Unescape(response);
    }

    /// <summary>
    /// Safe Close
    /// </summary>
    public void Close()
    {
        IsConnected = false;
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch { }
    }

    #region IDisposable Resource Release
    public void Dispose()
    {
        Close();
        _receiveBuffer?.Dispose();
    }
    #endregion

    #region Helper Methods
    private static bool BufferContains(byte[] buffer, byte[] marker)
    {
        if (marker.Length == 0 || buffer.Length < marker.Length) return false;
        for (int i = 0; i <= buffer.Length - marker.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < marker.Length; j++)
            {
                if (buffer[i + j] != marker[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return true;
        }
        return false;
    }

    private static (byte[] part1, byte[] part2) SplitOnce(byte[] buffer, byte[] separator)
    {
        int index = -1;
        int sepLen = separator.Length;

        for (int i = 0; i <= buffer.Length - sepLen; i++)
        {
            bool match = true;
            for (int j = 0; j < sepLen; j++)
            {
                if (buffer[i + j] != separator[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                index = i;
                break;
            }
        }

        if (index == -1) return (buffer, Array.Empty<byte>());

        byte[] part1 = new byte[index];
        byte[] part2 = new byte[buffer.Length - index - sepLen];
        Array.Copy(buffer, 0, part1, 0, index);
        Array.Copy(buffer, index + sepLen, part2, 0, part2.Length);

        return (part1, part2);
    }
    #endregion
}