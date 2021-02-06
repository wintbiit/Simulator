using System;
using Mirror;
using Script.Networking.Lobby;
using UnityEngine;

namespace Script.Networking
{
    namespace Game
    {
        // 用于存放队伍出生点的结构
        [Serializable]
        public class CampStart
        {
            public Transform hero;
            public Transform engineer;
            public Transform infantryA;
            public Transform infantryB;
            public Transform infantryC;
            public Transform drone;
            public Transform guard;
        }

        /*
         * 比赛管理器
         * + 保存队伍出生点
         * （以下待实现）
         * + 倒计时
         * + 表驱动的赛场事件
         * + 比赛状态记录
         * + 比赛中可能需要的服务器调用等
         */
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