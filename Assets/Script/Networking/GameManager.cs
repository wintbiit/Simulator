using Mirror;
using Script.Networking.Lobby;

namespace Script.Networking
{
    namespace Game
    {
        public class GameManager : NetworkBehaviour
        {
            private RoomManager _roomManager;

            #region Server

            public void RoomManagerRegister(RoomManager roomManager)
            {
                _roomManager = roomManager;
            }

            #endregion
        }
    }
}