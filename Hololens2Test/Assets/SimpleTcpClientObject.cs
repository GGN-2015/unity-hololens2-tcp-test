using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleTcpClientObject : MonoBehaviour
{
    private SimpleTcpClient _client;
    public string host = "127.0.0.1";
    public int port = 8888;

    // Start is called before the first frame update
    void Start()
    {
        _client = new SimpleTcpClient(host, port);
    }

    // Update is called once per frame
    void Update()
    {
        byte[] sample_text = _client.Request(System.Text.Encoding.UTF8.GetBytes("time"));
        if(sample_text != null)
        {
            string response = System.Text.Encoding.UTF8.GetString(sample_text);
            Debug.Log("Received: " + response);
        }
    }
}
