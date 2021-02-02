using System;
using System.Collections.Generic;
using Mirror;
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

            // 客户端侧成员
            // 在客户端侧将本地用户名从登陆页面传递给本地大厅玩家
            public string LocalDisplayName { set; get; }

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
                _connections.Remove(conn.connectionId);
            }

            [Server]
            public void StartGame(IDictionary<int, RoleT> roles)
            {
                // 在进入下一个场景前（LobbyManager将销毁）保存角色选择数据
                _roles = new Dictionary<int, RoleT>(roles);
                ServerChangeScene(GameplayScene);
            }

            /*
             * GamePlayer 和 Robot 是两个概念
             * 由于存在裁判、云台手、飞手这样的角色，故将机器人和操作手的概念分离
             * GamePlayer 存储了用户的身份数据
             * Robot 通过确认本地用户的控制权来被操控
             */
            public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
            {
                // 创建 GamePlayer
                var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                var gamePlayer = player.GetComponent<GamePlayer>();
                var roomPlayerComponent = roomPlayer.GetComponent<RoomPlayer>();
                // 传递用户标识
                gamePlayer.index = roomPlayerComponent.id;
                gamePlayer.displayName = roomPlayerComponent.displayName;
                gamePlayer.Role = _roles[roomPlayerComponent.id];

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
                        break;
                    case TypeT.Drone:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!robotInstance) return player;
                var robotComponent = robotInstance.GetComponent<RobotBase>();
                robotComponent.Role = role;
                robotComponent.id = roomPlayerComponent.id;
                // 将生成的机器人对象同步生成到所有客户端中
                NetworkServer.Spawn(robotInstance, conn);

                return player;
            }

            // 可调用的接口
            public void Disconnect(int connectionId)
            {
                _lobbyManager.PlayerLeave(connectionId);
                _connections[connectionId].Disconnect();
            }

            #endregion

            #region Client

            #endregion

            public override void OnRoomStartHost() => IsHost = true;

            public override void OnRoomStartServer() => IsServer = true;

            public override void OnGUI()
            {
                // 重载防止绘制默认GUI
            }
        }
    }
}