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
5. 编译、部署、启动项目：
    - 参考 [./README_ZH.md](./README_ZH.md) 中 “部署项目到 Hololens2” 
    - 或者 [./README_EN.md](./README_EN.md) 中 “Deploy the Project to HoloLens 2”

## 自己实现时的一些注意事项

> [!TIP]
> 常见编译错误请参考：
> - [./README_ZH.md](./README_ZH.md) 中 “编译报错的常见解决方案”
> - 或者 [./README_EN.md](./README_EN.md) “Common Solutions to Compilation Errors”

如果您打算自己实现 TCP 连接功能，记得做如下处理：
1. 检查 File → Build Settings → Player Settings（UWP）→ Publishing Settings → Capabilities
    - 需要勾选 InternetClientServer 与 PrivateNetworkClientServer
2. 如果你忘记了做 1 就已经编译过一次了，需要将上次编译的项目删除再重新编译
    - 否则 `.sln` 中的 `Package.appxmanifest` 可能不会被覆盖

## 本项目所使用的一些内容

- simple_tcp_server_cs：dotnet/unity 通用的 TCP 客户端服务端框架
    - https://github.com/GGN-2015/simple_tcp_server_cs
- simple_tcp_server：python 的 TCP 客户端服务端框架
    - https://github.com/GGN-2015/simple_tcp_server
- Unity + hololens2 + MRTK3 项目模板
    - https://github.com/GGN-2015/unity-hololens2-project-template

## 网络延迟

- 在 5G WIFI 局域网中
    - 静止状态，测得平均延迟 `15ms`
    - 剧烈运动会显著影响延迟
- 使用 Hololens2 自带的数据线测试（USB Ethernet）
    - 平均延时 `2ms`
    - 最大延时通常不超过 `5ms`

## 如何制作可以用手操作的模型

1. 拖拽 OBJ 文件进入 Asserts 框
2. 将 Asserts 框中的 OBJ 模型拖拽到层次结构中
3. 在层次结构中，找到该 OBJ 模型对应的对象（一般是父子两个对象一组，取父对象）
4. 给 OBJ 对象添加 Mesh Collision/Box Coliision 脚本（确定绿色线框包裹了）
    - Mesh Collision 记得选择正确的 Mesh 类型（一般取对象自身即可）
    - Mesh Collision 需要勾选 Convex
5. 给 OBJ 对象添加 Object Manipulator 脚本
