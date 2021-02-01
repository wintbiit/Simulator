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
        public class RoomManager : NetworkRoomManager
        {
            public string SelfDisplayName { set; get; }
            public bool IsHost { private set; get; }
            public bool IsServer { private set; get; }

            public GameObject heroPrefab;
            public GameObject engineerPrefab;
            public GameObject infantryPrefab;
            public GameObject dronePrefab;
            public GameObject guardPrefab;

            // Server side
            private LobbyManager _lobbyManager;
            private Dictionary<int, RoleTag> _roles;
            private readonly Dictionary<int, NetworkConnection> _connections = new Dictionary<int, NetworkConnection>();

            private GameManager _gameManager;

            #region Server

            public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnection conn)
            {
                var player = Instantiate(roomPlayerPrefab.gameObject, Vector3.zero, Quaternion.identity);
                var roomPlayer = player.GetComponent<RoomPlayer>();
                roomPlayer.id = clientIndex;
                roomPlayer.connectionId = conn.connectionId;
                return player;
            }

            public override void OnRoomServerPlayersReady()
            {
            }

            public override void OnRoomServerSceneChanged(string sceneName)
            {
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
                _lobbyManager.PlayerLeave(conn.connectionId);
                _connections.Remove(conn.connectionId);
            }

            [Server]
            public void StartGame(IDictionary<int, RoleTag> roles)
            {
                _roles = new Dictionary<int, RoleTag>(roles);
                ServerChangeScene(GameplayScene);
            }

            public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
            {
                var player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                var gamePlayer = player.GetComponent<GamePlayer>();
                var roomPlayerComponent = roomPlayer.GetComponent<RoomPlayer>();
                gamePlayer.index = roomPlayerComponent.id;
                gamePlayer.displayName = roomPlayerComponent.displayName;
                gamePlayer.Role = _roles[roomPlayerComponent.id];

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
                NetworkServer.Spawn(robotInstance, conn);

                return player;
            }

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
            }
        }
    }
}