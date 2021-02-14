# Simulator

RoboMaster比赛模拟器第三版。

[第一版 RoboSim](https://github.com/scutbot/RoboSim) [第二版 Realive](https://github.com/scutbot/Realive)

## 主要功能

+ 用于选择角色的准备大厅
+ 全兵种可选
+ 整场赛制模拟，包括经济、经验、等级、神符等
+ 增加裁判角色
+ 内置准备室和团队语音
+ 倒计时触发时序事件

![GUI](GUI.png)

## 开发进度

- [x] 准备大厅
- [x] 地面车辆运动模型
- [ ] 无人机运动模型
- [ ] 机器人特有功能（救援、取资源等）
- [x] 子弹击中检测
- [x] 伤害反馈
- [ ] 赛制模拟
- [ ] 裁判角色

## 操作流程

+ 运行软件，显示首页（登录界面）
  + 输入用户名，按回车键，以单机跑图模式运行
  + 输入用户名，输入服务器地址，按回车键，以客户端模式连接服务器
  + 按 F1 键，以纯服务端模式运行（无后续操作）
  + 按 Esc 键，退出软件
+ 进入准备大厅页面
  + 等待音频设备初始化
  + 单机跑图模式下，点击选择角色，按 R 开始游戏
  + 客户端模式下，点击选择角色，按 R 准备，等待裁判开始游戏
  + 若音频设备初始化失败，可选择断开连接重连重试，或在无语音情况下继续运行
  + 按 Esc 键，退出软件
+ 进入比赛场景
  + 按照特定角色操作进行操控（目前只完成步兵角色，操控同 Realive）

## 项目结构

项目分为三个场景：

+ **Index** 登录界面，选择单机跑图、纯服务器或联机运行方式。
  + `Assets/Scenes/Index.unity`
+ **Lobby** 准备大厅，操作手进行角色选择。
  + `Assets/Scenes/Lobby.unity`
+ **Game** 比赛场景。
  + `Assets/Scenes/Game.unity`

每一个场景对应一个场景管理器，负责处理对应场景的 **UI 组件，键盘事件，对象变化（如倒计时影响神符）等**，分别为：

+ `Assets/Script/Networking/IndexManager.cs`
+ `Assets/Script/Networking/LobbyManager.cs`
+ `Assets/Script/Networking/GameManager.cs`

项目存在一个全局单例的房间管理器，用于 **在场景之间传递数据，生成玩家和机器人等**：

+ `Assets/Script/Networking/RoomManager.cs`

赛场上的每一个单元（如机器人、装甲板、子弹等）对应一种控制器。机器人控制器主要负责对应角色的移动、射击和特定功能实现。其他单元的控制器于机器人控制器相互配合完成赛制模拟。目前实现的控制器包括：

+ `Assets/Script/Controller/InfantryController.cs`
+ `Assets/Script/Controller/ArmorController.cs`
+ `Assets/Script/Controller/BulletController.cs`

具体代码实现中还包括其他类型和接口的定义。

## 运行流程

+ 启动软件，进入 **Index** 场景
+ RoomManager 初始化，IndexManager 初始化
+ 获得用户名等信息，以指定方式运行（Host、Server 或 Client）
+ IndexManager 销毁

-----

+ 进入 **Lobby** 场景
+ LobbyManager 初始化
+ 服务端 RoomManager 生成 RoomPlayer 对象
+ 本地 RoomPlayer 对象从本地 RoomManager 取得用户名
+ 本地 RoomPlayer 对象向 LobbyManager 请求登记
+ 登记完成后，本地 LobbyManager 执行音频设备初始化
+ 等待音频初始化结果
+ 操作手选择角色，裁判或操作手选择开始游戏
+ RoomManager 从 LobbyManager 处取得角色选择信息
+ LobbyManager 销毁

-----

+ 进入 **Game** 场景
+ GameManager 初始化
+ 服务端 RoomManager 生成 GamePlayer 和 Robot 对象
+ 本地 GamePlayer 对象认领本地 Robot 对象
+ （待续）

## 开发安排

+ 取得新地图模型
+ 基础功能补全（准备室在线玩家显示等）
+ 代码优化（ClientRpc 改 TargetRpc，UI操作提取到 Update 函数等）
+ 设计赛制状态机
+ 设计建筑物控制器
+ 设计空中机器人运动模型