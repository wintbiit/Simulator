using System;
using System.Linq;
using Mirror;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using UnityEngine;

namespace Script.Networking
{
    namespace Game
    {
        /*
         * 存在于游戏场景中的玩家实例
         */
        public class GamePlayer : NetworkBehaviour
        {
            [SyncVar] public int index;
            [SyncVar] public string displayName;
            [SyncVar] public RoleT role;

            // 此处应实现为在合适的时机进行一次权限确认，循环没有必要
            private static int _slowUpdate;

            private void Start()
            {
                if (isServer)
                    FindObjectOfType<GameManager>().PlayerRegister(this);
            }

            [Server]
            public void OnAllReady()
            {
                OnAllReadyRpc();
            }

            [ClientRpc]
            private void OnAllReadyRpc()
            {
                if (!isLocalPlayer) return;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            private void FixedUpdate()
            {
                if (_slowUpdate > 30)
                {
                    _slowUpdate = 0;
                    if (isLocalPlayer && isClient)
                    {
                        foreach (var robot in FindObjectsOfType<RobotBase>())
                            if (!robot.registered)
                            {
                                if (robot.id != index) continue;
                                robot.ConfirmLocalRobot();
                                break;
                            }
                    }
                }

                _slowUpdate++;
            }
        }
    }
}