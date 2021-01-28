using Mirror;
using Script.JudgeSystem.Role;
using Script.Networking.Lobby;
using UnityEngine;

namespace Script.Networking
{
    namespace Game
    {
        public class GameManager : NetworkBehaviour
        {
            private RoomManager _roomManager;
            private readonly SyncDictionary<int, Role> _roles = new SyncDictionary<int, Role>();

            private void Awake()
            {
                Debug.Log("Game scene");
            }

            #region Server

            public void RoomManagerRegister(RoomManager roomManager)
            {
                _roomManager = roomManager;
                foreach (var role in _roomManager.Roles)
                    _roles.Add(role);
                Debug.Log(_roles.Count);
            }

            #endregion
        }
    }
}