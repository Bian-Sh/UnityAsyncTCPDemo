# UnityAsyncTCPDemo

# 1.简要说明
1. 测试基于async/await(TAP) 模式下的TCP 通信。
2. 这个Demo 是一个基于TCP通信的Remotecontroller for video player，实现了客户端控制服务端视频播放器的播放、暂停、停止，也实现了MP4文件备注信息的获取。
3. Windows10 & Unity 2019.2.12f1 

# 2.工程结构
> Common 
- 包括通信消息数据模型，简易事件系统，UnitySynchronizationContext 扩展
- 以UnityPackage 的形式管理的客户端与服务端的公共组件，方便了消息数据模型一端修改两端同步。

> Controller
- 这是TCP 客户端
- 视频播放器的远程控制器，实现了从服务端获取播放列表、视频的播放、暂停、与停止

> Player 
- 这是TCP 服务端
- 基于UnityEngine.Video.VideoPlayer 的视频播放器
- 实现了从 SteamingAssets 获取文件播放列表和媒体文件描述
- 媒体文件描述信息使用 Interop.Shell32.dll 获取
- 本Demo提供了测试视频，但请勿用于商业用途

# 3. 演示
![](Doc/Demo.gif)
