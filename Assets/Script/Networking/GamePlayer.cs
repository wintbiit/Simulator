using Mirror;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;

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
            [SyncVar] public RoleT Role;

            // 此处应实现为在合适的时机进行一次权限确认，循环没有必要
            private static int _slowUpdate;

            private void FixedUpdate()
            {
                if (_slowUpdate % 10 == 0)
                {
                    if (isLocalPlayer)
                        // 该行有性能问题
                        foreach (var robot in FindObjectsOfType<RobotBase>())
                            // 如果是对应的 Robot，则进行确权
                            if (robot.id == index)
                            {
                                robot.isLocalRobot = true;
                                break;
                            }

                    _slowUpdate = 0;
                }
                else _slowUpdate++;
            }
        }
    }
}