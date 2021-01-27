using System.Collections.Generic;
using Mirror;
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
            private readonly Dictionary<int, NetworkConnection> _connections = new Dictionary<int, NetworkConnection>();

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
                if (sceneName != "Assets/Scenes/Lobby.unity") return;
                _lobbyManager = GameObject.Find("Main Camera").GetComponent<LobbyManager>();
                _lobbyManager.RoomManagerRegister(this);
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
            public void StartGame() => ServerChangeScene(GameplayScene);

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