using System;
using Mirror;
using Script.Networking.Lobby;
using UnityEngine;

namespace Script.Networking
{
    namespace Game
    {
        [Serializable]
        public class CampStart
        {
            public Transform hero;
            public Transform engineer;
            public Transform infantryA;
            public Transform infantryB;
            public Transform infantryC;
            public Transform drone;
        }

        public class GameManager : NetworkBehaviour
        {
            private RoomManager _roomManager;

            public CampStart redStart;
            public CampStart blueStart;

            #region Server

            public void RoomManagerRegister(RoomManager roomManager)
            {
                _roomManager = roomManager;
            }

            #endregion
        }
    }
}