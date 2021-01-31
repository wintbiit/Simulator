using Mirror;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;

namespace Script.Networking
{
    namespace Game
    {
        public class GamePlayer : NetworkBehaviour
        {
            [SyncVar] public int index;
            [SyncVar] public string displayName;
            [SyncVar] public RoleTag Role;

            private static int _slowUpdate;

            private void FixedUpdate()
            {
                if (_slowUpdate % 10 == 0)
                {
                    if (isLocalPlayer)
                        foreach (var robot in FindObjectsOfType<RobotBase>())
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