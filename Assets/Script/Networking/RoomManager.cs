using System.Collections.Generic;
using Mirror;
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

            // Server side
            private LobbyManager _lobbyManager;
            public Dictionary<int, Role> Roles { private set; get; }
            private readonly Dictionary<int, NetworkConnection> _connections = new Dictionary<int, NetworkConnection>();

            private GameManager _gameManager;

            #region Server

            public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnection conn)
            {
                var player = Instantiate(roomPlayerPrefab.gameObject, Vector3.zero, Quaternion.identity);
                var roomPlayer = player.GetComponent<RoomPlayer>();
                roomPlayer.index = clientIndex;
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
            public void StartGame(IDictionary<int, Role> roles)
            {
                Roles = new Dictionary<int, Role>(roles);
                ServerChangeScene(GameplayScene);
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