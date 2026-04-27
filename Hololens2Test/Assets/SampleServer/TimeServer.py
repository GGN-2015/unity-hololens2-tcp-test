from simple_tcp_server import SimpleTcpServer
from datetime import datetime

TEXT_ENCODING = "utf-8"
HOST = "0.0.0.0"
PORT = 8888

def time_now() -> str:
    return datetime.now().strftime("%Y-%m-%d %H:%M:%S")

def time_server_worker(msg: bytes) -> bytes:
    if msg == b"time":
        return time_now().encode(TEXT_ENCODING)
    else:
        return (f"command {msg} unknown").encode(TEXT_ENCODING)

server = SimpleTcpServer(
    HOST, PORT, time_server_worker, quit_token=b"quit")

server.mainloop()
