using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Script.JudgeSystem.Role;
using UnityEngine;

namespace Script.Networking
{
    namespace Game
    {
        public class GamePlayer : NetworkBehaviour
        {
            [SyncVar] public int index;
            [SyncVar] public string displayName;
            [SyncVar] public Role Role;

            private void Start()
            {
                Debug.Log("Game player " + index.ToString() + displayName.ToString());
            }
        }
    }
}