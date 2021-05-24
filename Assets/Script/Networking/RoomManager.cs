using System;
using System.Collections.Generic;
using Mirror;
using Script.JudgeSystem.Facility;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Networking
{
    namespace Lobby
    {
        /*
         * 全局网络管理器脚本
         * + 传递用户名信息
         * + 为客户端分配ID
         * + 管理客户端连接
         * + 获取客户端角色
         * + 进行从大厅场景到游戏场景的切换
         * + 根据角色类型生成机器人
         */
        public class RoomManager : NetworkRoomManager
        {
            // 机器人预制件
            public GameObject heroPrefab;
            public GameObject engineerPrefab;
            public GameObject infantryPrefab;
            public GameObject dronePrefab;
            public GameObject guardPrefab;
            public GameObject basePrefab;
            public GameObject outpostPrefab;
            public GameObject buffPrefab;
            public GameObject silverPrefab;
            public GameObject goldPrefab;
            public GameObject blockPrefab;

            // 客户端侧成员
            // 在客户端侧将本地用户名从登陆页面传递给本地大厅玩家
            public string LocalDisplayName { set; get; }

            // WebSocket 服务
            private WsApi _wsApi;

            // 服务端侧成员
            private LobbyManager _lobbyManager;

            private GameManager _gameManager;

            // 服务端类型标识
            public bool IsHost { private set; get; }
            public bool IsServer { private set; get; }

            // 从客户端ID到所选角色的映射
            private Dictionary<int, RoleT> _roles;

            // 从网络连接ID到网络连接对象的映射
            private readonly Dictionary<int, NetworkConnection> _connections = new Dictionary<int, NetworkConnection>();

            public override void Awake()
            {
                if (FindObjectsOfType<RoomManager>().Length > 1)
                    Destroy(this);
                else
                    base.Awake();
            }

            // 在服务端侧执行的函数

            #region Server

            /*
             * RoomPlayer 存在于准备大厅中
             * 其作用主要是保存客户端基本信息，方便后续创建 GamePlayer
             */
            public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnection conn)
            {
                var player = Instantiate(roomPlayerPrefab.gameObject, Vector3.zero, Quaternion.identity);
                var roomPlayer = player.GetComponent<RoomPlayer>();
                // 将时序递增的 clientIndex 作为本场游戏中客户端的唯一ID
                roomPlayer.id = clientIndex;
                roomPlayer.connectionId = conn.connectionId;
                return player;
            }

            public override void OnRoomServerPlayersReady()
            {
                // 重载防止直接进入游戏的默认行为
            }

            public override void OnRoomServerSceneChanged(string sceneName)
            {
                // 根据场景切换获取相应单例，并分发自身实例
                // 由于第一个场景（Index）无法捕获，IndexManager需要自行获取本实例
                switch (sceneName)
                {
                    case "Assets/Scenes/Lobby.unity":
                        _lobbyManager = GameObject.Find("Main Camera").GetComponent<LobbyManager>();
                        _lobbyManager.RoomManagerRegister(this);
                        break;
                    case "Assets/Scenes/Game.unity":
                        _gameManager = GameObject.Find("Main Camera").GetComponent<GameManager>();
                        _gameManager.RoomManagerRegister(this);
                        if (_wsApi != null)
                            _wsApi.SetGameManager(_gameManager);
                        break;
                }
            }

            public override void OnRoomServerConnect(NetworkConnection conn)
            {
                _connections.Add(conn.connectionId, conn);
            }

            public override void OnRoomServerDisconnect(NetworkConnection conn)
            {
                // 在服务端回调中删除数据以保证没有僵尸玩家
                _lobbyManager.PlayerLeave(conn.connectionId);
                if (_gameManager)
                    _gameManager.PlayerLeave(conn.connectionId);
                _connections.Remove(conn.connectionId);
            }

            [Server]
            public void StartGame(IDictionary<int, RoleT> roles)
            {
                // 在进入下一个场景前（LobbyManager将销毁）保存角色选择数据
                _roles = new Dictionary<int, RoleT>(roles);
                ServerChangeScene(GameplayScene);
            }

            [Server]
            public RoleT GetRole(int id)
            {
                if (_lobbyManager != null)
                    return _lobbyManager.GetRole(id);
                return new RoleT(CampT.Unknown, TypeT.Unknown);
            }

            /*
             * GamePlayer 和 Robot 是两个概念
             * 由于存在裁判、云台手、飞手这样的角色，故将机器人和操作手的概念分离
             * GamePlayer 存储了用户的身份数据
             * Robot 通过确认本地用户的控制权来被操控
             */
            private bool _facilitiesInitiated;

            public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
            {
                // 创建 GamePlayer
                var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                var gamePlayer = player.GetComponent<GamePlayer>();
                var roomPlayerComponent = roomPlayer.GetComponent<RoomPlayer>();
                // 传递用户标识
                gamePlayer.index = roomPlayerComponent.id;
                gamePlayer.displayName = roomPlayerComponent.displayName;
                gamePlayer.role = _roles[roomPlayerComponent.id];
                Debug.Log("Creating Game Player:" + gamePlayer.displayName);

                // 创建 Robot
                GameObject robotInstance = null;
                Transform target;
                var role = _roles[roomPlayerComponent.id];
                switch (role.Type)
                {
                    case TypeT.Unknown:
                        if (role.Camp != CampT.Judge)
                            throw new ArgumentOutOfRangeException();
                        break;
                    case TypeT.Hero:
                        // 根据阵营与角色信息确定使用的预制件和出生点
                        target = role.Camp == CampT.Blue
                            ? _gameManager.blueStart.hero
                            : _gameManager.redStart.hero;
                        robotInstance = Instantiate(heroPrefab, target.position, target.rotation);
                        break;
                    case TypeT.Engineer:
                        target = role.Camp == CampT.Blue
                            ? _gameManager.blueStart.engineer
                            : _gameManager.redStart.engineer;
                        robotInstance = Instantiate(engineerPrefab, target.position, target.rotation);
                        break;
                    case TypeT.InfantryA:
                        target = role.Camp == CampT.Blue
                            ? _gameManager.blueStart.infantryA
                            : _gameManager.redStart.infantryA;
                        robotInstance = Instantiate(infantryPrefab, target.position, target.rotation);
                        break;
                    case TypeT.InfantryB:
                        target = role.Camp == CampT.Blue
                            ? _gameManager.blueStart.infantryB
                            : _gameManager.redStart.infantryB;
                        robotInstance = Instantiate(infantryPrefab, target.position, target.rotation);
                        break;
                    case TypeT.InfantryC:
                        target = role.Camp == CampT.Blue
                            ? _gameManager.blueStart.infantryC
                            : _gameManager.redStart.infantryC;
                        robotInstance = Instantiate(infantryPrefab, target.position, target.rotation);
                        break;
                    case TypeT.Ptz:
                        _gameManager.ptzCount++;
                        Debug.Log("New ptz:" + _gameManager.ptzCount);
                        break;
                    case TypeT.Drone:
                        target = role.Camp == CampT.Blue
                            ? _gameManager.blueStart.drone
                            : _gameManager.redStart.drone;
                        robotInstance = Instantiate(dronePrefab, target.position, target.rotation);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (robotInstance)
                {
                    var robotComponent = robotInstance.GetComponent<RobotBase>();
                    robotComponent.role = role;
                    robotComponent.id = roomPlayerComponent.id;
                    robotComponent.level = 1;
                    robotComponent.chassisType = ChassisT.Default;
                    robotComponent.gunType = GunT.Default;
                    robotComponent.health = RobotPerformanceTable.Table[1][role.Type][ChassisT.Default][GunT.Default]
                        .HealthLimit;
                    robotComponent.experience = 0;
                    robotComponent.smallAmmo = RobotPerformanceTable.Table[1][role.Type][ChassisT.Default][GunT.Default]
                        .SmallAmmo;
                    robotComponent.largeAmmo = RobotPerformanceTable.Table[1][role.Type][ChassisT.Default][GunT.Default]
                        .LargeAmmo;

                    robotComponent.gameManager = _gameManager;
                    robotComponent.registered = false;
                    // 将生成的机器人对象同步生成到所有客户端中
                    NetworkServer.Spawn(robotInstance, conn);
                }

                // 自动化机器人、建筑等初始化
                if (_facilitiesInitiated) return player;
                _facilitiesInitiated = true;

                var fid = gamePlayer.index + 20;

                // 哨兵
                {
                    var t = _gameManager.blueStart.guard;
                    var f = Instantiate(guardPrefab, t.position, t.rotation);
                    var c = f.GetComponent<RobotBase>();
                    c.role = new RoleT(CampT.Blue, TypeT.Guard);
                    c.id = ++fid;
                    c.level = 1;
                    c.chassisType = ChassisT.Default;
                    c.gunType = GunT.Default;
                    c.health = RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default].HealthLimit;
                    c.experience = 0;
                    c.smallAmmo = RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default].SmallAmmo;
                    c.largeAmmo = RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default].LargeAmmo;
                    c.gameManager = _gameManager;
                    _gameManager.RobotRegister(c);
                    NetworkServer.Spawn(f);

                    var t1 = _gameManager.redStart.guard;
                    var f1 = Instantiate(guardPrefab, t1.position, t1.rotation);
                    var c1 = f1.GetComponent<RobotBase>();
                    c1.role = new RoleT(CampT.Red, TypeT.Guard);
                    c1.id = ++fid;
                    c1.level = 1;
                    c.chassisType = ChassisT.Default;
                    c.gunType = GunT.Default;
                    c1.health = RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default].HealthLimit;
                    c1.experience = 0;
                    c1.smallAmmo = RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default]
                        .SmallAmmo;
                    c1.largeAmmo = RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default]
                        .LargeAmmo;
                    c1.gameManager = _gameManager;
                    _gameManager.RobotRegister(c1);
                    NetworkServer.Spawn(f1);
                }
                // 基地
                {
                    var t = _gameManager.blueStart.campBase;
                    var f = Instantiate(basePrefab, t.position, t.rotation);
                    var c = f.GetComponent<FacilityBase>();
                    c.id = ++fid;
                    c.role = new RoleT(CampT.Blue, TypeT.Base);
                    c.gameManager = _gameManager;
                    c.health = 5500;
                    c.healthLimit = 5500;
                    _gameManager.FacilityRegister(c);
                    NetworkServer.Spawn(f);

                    var t1 = _gameManager.redStart.campBase;
                    var f1 = Instantiate(basePrefab, t1.position, t1.rotation);
                    var c1 = f1.GetComponent<FacilityBase>();
                    c1.id = ++fid;
                    c1.role = new RoleT(CampT.Red, TypeT.Base);
                    c1.gameManager = _gameManager;
                    c1.health = 5000;
                    c1.healthLimit = 5000;
                    _gameManager.FacilityRegister(c1);
                    NetworkServer.Spawn(f1);
                }
                // 前哨站
                {
                    var t = _gameManager.blueStart.campOutpost;
                    var f = Instantiate(outpostPrefab, t.position, t.rotation);
                    var c = f.GetComponent<FacilityBase>();
                    c.id = ++fid;
                    c.role = new RoleT(CampT.Blue, TypeT.Outpost);
                    c.gameManager = _gameManager;
                    c.health = 2000;
                    c.healthLimit = 2000;
                    _gameManager.FacilityRegister(c);
                    NetworkServer.Spawn(f);

                    var t1 = _gameManager.redStart.campOutpost;
                    var f1 = Instantiate(outpostPrefab, t1.position, t1.rotation);
                    var c1 = f1.GetComponent<FacilityBase>();
                    c1.id = ++fid;
                    c1.role = new RoleT(CampT.Red, TypeT.Outpost);
                    c1.gameManager = _gameManager;
                    c1.health = 2000;
                    c1.healthLimit = 2000;
                    _gameManager.FacilityRegister(c1);
                    NetworkServer.Spawn(f1);
                }
                // 神符
                {
                    var t = _gameManager.blueStart.campBuff;
                    var f = Instantiate(buffPrefab, t.position, t.rotation);
                    var c = f.GetComponent<FacilityBase>();
                    c.id = ++fid;
                    c.role = new RoleT(CampT.Blue, TypeT.EnergyMechanism);
                    c.gameManager = _gameManager;
                    c.health = 2000;
                    c.healthLimit = 2000;
                    _gameManager.FacilityRegister(c);
                    NetworkServer.Spawn(f);

                    var t1 = _gameManager.redStart.campBuff;
                    var f1 = Instantiate(buffPrefab, t1.position, t1.rotation);
                    var c1 = f1.GetComponent<FacilityBase>();
                    c1.id = ++fid;
                    c1.role = new RoleT(CampT.Red, TypeT.EnergyMechanism);
                    c1.gameManager = _gameManager;
                    c1.health = 2000;
                    c1.healthLimit = 2000;
                    _gameManager.FacilityRegister(c1);
                    NetworkServer.Spawn(f1);
                }
                // 矿物
                {
                    foreach (var m in _gameManager.silverStart)
                        NetworkServer.Spawn(Instantiate(silverPrefab, m.position, m.rotation));
                    foreach (var m in _gameManager.goldStart)
                        NetworkServer.Spawn(Instantiate(goldPrefab, m.position, m.rotation));
                }
                // 障碍块
                {
                    foreach (var b in _gameManager.blockStart)
                        NetworkServer.Spawn(Instantiate(blockPrefab, b.position, b.rotation));
                }
                return player;
            }

            // 可调用的接口
            public void Disconnect(int connectionId)
            {
                _lobbyManager.PlayerLeave(connectionId);
                _connections[connectionId].Disconnect();
            }

            private void FixedUpdate()
            {
                if (!IsServer) return;
                _wsApi?.OnFixedUpdate();
                if (_gameManager == null) return;
                if (_connections.Count != 0) return;
                ResetServer();
            }

            [Server]
            public void ResetServer()
            {
                if (IsHost)
                    StopHost();
                else
                    StopServer();
                LocalDisplayName = "";
                _lobbyManager = null;
                _gameManager = null;
                IsHost = false;
                IsServer = false;
                _roles.Clear();
                _connections.Clear();
                _facilitiesInitiated = false;
                _wsApi.Stop();
            }

            #endregion

            #region Client

            #endregion

            public override void OnRoomStartHost() => IsHost = true;

            public override void OnRoomStartServer()
            {
                IsServer = true;
                if (_wsApi == null)
                    _wsApi = new WsApi(this);
            }

            public override void OnGUI()
            {
                // 重载防止绘制默认GUI
            }
        }
    }
}