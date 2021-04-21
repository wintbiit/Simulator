using System;
using Mirror;
using UnityEngine;

namespace Script.Networking
{
    namespace Lobby
    {
        /*
         * 存在于准备大厅中的玩家实例
         */
        public class RoomPlayer : NetworkRoomPlayer
        {
            [SyncVar] public int id;
            [SyncVar] public int connectionId;
            [SyncVar] public string displayName;

            // 客户端侧成员
            // 记录是否已登记过
            private bool _registered;

            public override void OnClientEnterRoom()
            {
                if (!isLocalPlayer || _registered) return;
                _registered = true;
                // 从本地 RoomManager 获取本地用户名
                displayName = GameObject.Find("RoomManager").GetComponent<RoomManager>().LocalDisplayName;
                UpdateDisplayName(displayName);
                GameObject.Find("Main Camera").GetComponent<LobbyManager>().PlayerRegister(this);
            }

            [Command(ignoreAuthority = true)]
            private void UpdateDisplayName(string n)
            {
                displayName = n;
            }

            public override void OnGUI()
            {
            }
        }
    }
}