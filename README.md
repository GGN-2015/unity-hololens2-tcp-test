> [!NOTE]
> - Project Template：
>    - https://github.com/GGN-2015/unity-hololens2-project-template

> [!IMPORTANT]
> - 从本项目出发编译复现请参考：[./README_ZH.md](./README_ZH.md)
> - For compilation and reproduction based on this project, please refer to: [./README_EN.md](./README_EN.md)

# unity-hololens2-tcp-test

一个带有 TCP 测试的 Hololens2 MRTK3 示例 Unity 项目。

在本项目中，我们需要在局域网中的一台主机上启动时间服务器 `./Hololens2Test/Asserts/SampleServer/TimeServer.py` 然后再在 AR 眼镜中启动游戏，就能实时与服务器通信获取服务器上的系统时间。

## 前置

- 你需要拥有 python 解释器
- 你需要使用 `pip install simple_tcp_server` 安装必要的 python 通信库

## 启动步骤

1. 启动 `./Hololens2Test/Asserts/SampleServer/TimeServer.py`（一直运行）
2. 使用 Unity Editor 打开项目 `./Hololens2Test`
3. 打开创景 `./Hololens2Test/Asserts/Scene/Hololen2Test.unity`
4. 在层级目录中找到对象 `SimpleTcpClientObject` 并配置其 Host 属性（与主机 IP 地址一致）
5. 参考 [./README_ZH.md](./README_ZH.md) 或者 [./README_EN.md](./README_EN.md) 中的步骤，编译并启动项目。
