using Mirror;
using Script.JudgeSystem.Role;

namespace Script.JudgeSystem
{
    namespace Robot
    {
        /*
         * 机器人底盘类型（仅地面机器人适用）
         * 重装底盘、大功率底盘、平衡底盘（暂无设计）
         */
        public enum ChassisT
        {
            Armor = 0,
            Power = 1
        }

        /*
         * 机器人发射装置类型（仅地面机器人适用）
         * 爆发型、快速冷却型、高射速型
         */
        public enum GunT
        {
            Burst = 0,
            ColdDown = 1,
            Velocity = 2
        }
        
        /*
         * 所有 RobotController 的基类，包含了机器人的基本信息
         * 
         * id: 机器人唯一标识符
         * Role: 角色
         * health: 生命值
         * damageRate: 伤害加成，默认应为 1.0f
         * armorRate: 护甲加成，默认应为 1.0f
         * smallAmmo: 小弹丸数量
         * largeAmmo: 大弹丸数量
         * heatLimit: 枪口热量限制
         * experience: 经验值
         * powerLimit: 底盘功率限制
         * chassisType: 底盘类型（仅地面机器人适用）
         * gunType: 发射装置类型（仅地面机器人适用）
         * （以下未设计）
         * 机器人等级
         * 
         * isLocalRobot: 用于确权
         */
        public class RobotBase : NetworkBehaviour
        {
            [SyncVar] public int id;
            [SyncVar] public RoleT Role;
            [SyncVar] public int health;
            [SyncVar] public float damageRate;
            [SyncVar] public float armorRate;
            [SyncVar] public int smallAmmo;
            [SyncVar] public int largeAmmo;
            [SyncVar] public int heatLimit;
            [SyncVar] public int experience;
            [SyncVar] public int powerLimit;
            [SyncVar] public ChassisT chassisType;
            [SyncVar] public GunT gunType;

            public bool isLocalRobot;
        }
    }
}