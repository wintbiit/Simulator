using Mirror;
using Script.JudgeSystem.Role;

namespace Script.JudgeSystem
{
    namespace Robot
    {
        public enum ChassisT
        {
            Armor = 0,
            Power = 1
        }

        public enum GunT
        {
            Burst = 0,
            ColdDown = 1,
            Velocity = 2
        }
        
        public class RobotBase : NetworkBehaviour
        {
            [SyncVar] public int id;
            [SyncVar] public RoleTag Role;
            [SyncVar] public int health;
            [SyncVar] public float damageRate;
            [SyncVar] public float armorRate;
            [SyncVar] public int smallAmmo;
            [SyncVar] public int largeAmmo;
            [SyncVar] public int heat;
            [SyncVar] public int experience;
            [SyncVar] public int powerLimit;
            [SyncVar] public ChassisT chassisType;
            [SyncVar] public GunT gunType;

            public bool isLocalRobot;
        }
    }
}