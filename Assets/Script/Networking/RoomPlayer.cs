using Mirror;
using UnityEngine;

namespace Script.Networking
{
    namespace Lobby
    {
        public class RoomPlayer : NetworkRoomPlayer
        {
            public string displayName;
            [SyncVar] public int connectionId;

            private LobbyManager _lobbyManager;
            private RoomManager _roomManager;
            private bool _registered;

            public override void OnClientEnterRoom()
            {
                if (!isLocalPlayer || _registered) return;
                _registered = true;
                _lobbyManager = GameObject.Find("Main Camera").GetComponent<LobbyManager>();
                _roomManager = GameObject.Find("RoomManager").GetComponent<RoomManager>();
                displayName = _roomManager.SelfDisplayName;
                _lobbyManager.PlayerRegister(this);
            }

            public override void OnGUI()
            {
            }
        }
    }
}