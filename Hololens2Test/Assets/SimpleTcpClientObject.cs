using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class SimpleTcpClientObject : MonoBehaviour
{
    // 在Inspector面板，把场景里的 ServerText TMP 拖进来绑定
    [SerializeField]
    public TextMeshProUGUI ServerText;

    [SerializeField]
    public string host = "127.0.0.1";

    [SerializeField]
    public int port = 8888;

    private SimpleTcpClient _client;
    private Thread _daemonThread;
    private bool _isRunning = true;

    private LatestMessageHolder _latestMessageHolder;

    // Start is called before the first frame update
    void Start()
    {
        _client = new SimpleTcpClient(host, port);
        _latestMessageHolder = new LatestMessageHolder();

        _daemonThread = new Thread(BackgroundDaemonLoop)
        {
            IsBackground = true, // 设为后台线程：游戏退出时随进程自动结束
            Name = "MyUnityDaemonThread"
        };
        _daemonThread.Start();
    }

    // 子线程 无限循环守护逻辑
    private void BackgroundDaemonLoop()
    {
        while (_isRunning)
        {
            byte[] sample_text = _client.Request(System.Text.Encoding.UTF8.GetBytes("time"));
            if (sample_text != null)
            {
                string response = System.Text.Encoding.UTF8.GetString(sample_text);
                _latestMessageHolder.Set(response);
            }
            Thread.Sleep(100);
        }
    }

    // Update is called once per frame
    void Update()
    {
        string ui_data = _latestMessageHolder.Get(); 
        if (!string.IsNullOrEmpty(ui_data))
        {
            ServerText.text = ui_data;
        }
    }

    // 退出时关闭线程
    void OnDestroy()
    {
        _isRunning = false;

        // 等待线程正常退出
        if (_daemonThread != null && _daemonThread.IsAlive)
        {
            _daemonThread.Join(1000);
        }

        Debug.Log("Daemon Quit.");
    }
}
