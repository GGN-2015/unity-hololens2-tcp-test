using System;

public class LatestMessageHolder
{
    private static readonly Lazy<LatestMessageHolder> _inst =
        new Lazy<LatestMessageHolder>(() => new LatestMessageHolder());
    public static LatestMessageHolder Instance => _inst.Value;

    private readonly object _lock = new object();
    private string _latestMsg;

    public LatestMessageHolder() { }

    // 子线程调用：直接覆盖最新数据
    public void Set(string msg)
    {
        lock (_lock)
        {
            _latestMsg = msg;
        }
    }

    // 主线程调用：拿到当前最新
    public string Get()
    {
        lock (_lock)
        {
            return _latestMsg;
        }
    }
}