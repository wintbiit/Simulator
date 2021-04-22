using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.JudgeSystem
{
    namespace Robot
    {
        /*
         * 机器人底盘类型（仅地面机器人适用）
         * 重装底盘、大功率底盘、平衡底盘（暂无设计）
         */
        // public enum ChassisT
        // {
        //     Armor = 0,
        //     Power = 1
        // }

        /*
         * 机器人发射装置类型（仅地面机器人适用）
         * 爆发型、快速冷却型、高射速型
         */
        // public enum GunT
        // {
        //     Burst = 0,
        //     ColdDown = 1,
        //     Velocity = 2
        // }

        // 用于存放机器人等级性能数据的结构
        /*
         * smallAmmo: 小弹丸数量
         * largeAmmo: 大弹丸数量
         * healthLimit: 生命上限
         * heatLimit: 枪口热量限制
         * powerLimit: 底盘功率限制
         * velocityLimit: 枪口初速限制
         * coolDownRate: 枪口冷却速度
         * expValue: 机器人经验价值
         * expUpgrade: 升级所需经验值
         */
        [Serializable]
        public class RobotLevel
        {
            public readonly int SmallAmmo;
            public readonly int LargeAmmo;
            public readonly int HealthLimit;
            public readonly int HeatLimit;
            public readonly int PowerLimit;
            public readonly int VelocityLimit;
            public readonly int CoolDownRate;
            public readonly float ExpValue;
            public readonly float ExpUpgrade;

            public RobotLevel(
                int smallAmmo, int largeAmmo,
                int healthLimit, int heatLimit,
                int powerLimit, int velocityLimit,
                int coolDownRate, float expValue, float expUpgrade)
            {
                SmallAmmo = smallAmmo;
                LargeAmmo = largeAmmo;
                HealthLimit = healthLimit;
                HeatLimit = heatLimit;
                PowerLimit = powerLimit;
                VelocityLimit = velocityLimit;
                CoolDownRate = coolDownRate;
                ExpValue = expValue;
                ExpUpgrade = expUpgrade;
            }
        }

        public static class RobotPerformanceTable
        {
            public static readonly Dictionary<int, Dictionary<TypeT, RobotLevel>> Table =
                new Dictionary<int, Dictionary<TypeT, RobotLevel>>
                {
                    {
                        1, new Dictionary<TypeT, RobotLevel>
                        {
                            {
                                TypeT.Hero, new RobotLevel(
                                    0, 0,
                                    200, 100, 70, 16,
                                    20, 7.5f, 8.0f)
                            },
                            {
                                TypeT.Engineer, new RobotLevel(
                                    0, 0,
                                    500, int.MaxValue, 80, 0,
                                    0, 5, 0.0f)
                            },
                            {
                                TypeT.InfantryA, new RobotLevel(
                                    0, 0,
                                    150, 150, 60, 15,
                                    15, 2.5f, 3.0f)
                            },
                            {
                                TypeT.InfantryB, new RobotLevel(
                                    0, 0,
                                    150, 150, 60, 15,
                                    15, 2.5f, 3.0f)
                            },
                            {
                                TypeT.InfantryC, new RobotLevel(
                                    0, 0,
                                    150, 150, 60, 15,
                                    15, 2.5f, 3.0f)
                            },
                            {
                                TypeT.Guard, new RobotLevel(
                                    500, 0,
                                    600, 320, 30, 15,
                                    100, 7.5f, float.MaxValue
                                )
                            },
                            // {TypeT.Ptz, new RobotLevel(0, 0, 0, 0, 0, 0, 0, 0)},
                            {
                                TypeT.Drone, new RobotLevel(
                                    500, 0,
                                    1, int.MaxValue, 0, 0,
                                    100, 0, 0)
                            }
                        }
                    },
                    {
                        2, new Dictionary<TypeT, RobotLevel>
                        {
                            {
                                TypeT.Hero, new RobotLevel(
                                    0, 0,
                                    250, 200, 90, 16,
                                    60, 10.0f, 12.0f)
                            },
                            {
                                TypeT.InfantryA, new RobotLevel(
                                    0, 0,
                                    200, 280, 80, 15,
                                    25, 5.0f, 6.0f)
                            },
                            {
                                TypeT.InfantryB, new RobotLevel(
                                    0, 0,
                                    200, 280, 80, 15,
                                    25, 5.0f, 6.0f)
                            },
                            {
                                TypeT.InfantryC, new RobotLevel(
                                    0, 0,
                                    200, 280, 80, 15,
                                    25, 5.0f, 6.0f)
                            }
                        }
                    },
                    {
                        3, new Dictionary<TypeT, RobotLevel>
                        {
                            {
                                TypeT.Hero, new RobotLevel(
                                    0, 0,
                                    300, 300, 120, 16,
                                    100, 15.0f, 0.0f)
                            },
                            {
                                TypeT.InfantryA, new RobotLevel(
                                    0, 0,
                                    250, 400, 100, 15,
                                    35, 7.5f, 0.0f)
                            },
                            {
                                TypeT.InfantryB, new RobotLevel(
                                    0, 0,
                                    250, 400, 100, 15,
                                    35, 7.5f, 0.0f)
                            },
                            {
                                TypeT.InfantryC, new RobotLevel(
                                    0, 0,
                                    250, 400, 100, 15,
                                    35, 7.5f, 0.0f)
                            }
                        }
                    }
                };
        }

        public class Attr
        {
            public float DamageRate;
            public float ArmorRate;
            public float ColdDownRate;
            public float ReviveRate;
        }

        /*
         * 所有 RobotController 的基类，包含了机器人的基本信息
         * 
         * id: 机器人唯一标识符
         * role: 角色
         * level: 等级
         *
         * 根据比赛变化
         * health: 生命值
         * experience: 经验值
         * damageRate: 伤害加成，默认应为 1.0f
         * armorRate: 护甲加成，默认应为 1.0f
         *
         * gameManager: （服务器端）用于注册
         * isLocalRobot: （客户端）用于确权
         *
         * 暂不实现
         * chassisType: 底盘类型（仅地面机器人适用）
         * gunType: 发射装置类型（仅地面机器人适用）
         */
        public abstract class RobotBase : NetworkBehaviour
        {
            [SyncVar] public int id;
            [SyncVar] public RoleT role;
            [SyncVar] public int level;

            [SyncVar] public int health;
            [SyncVar] public int smallAmmo;
            [SyncVar] public int largeAmmo;
            [SyncVar] public float experience;
            [SyncVar] public float velocityLimit;

            [SyncVar] public float heat;

            public readonly SyncList<BuffBase> Buffs = new SyncList<BuffBase>();
            // [SyncVar] public ChassisT chassisType;
            // [SyncVar] public GunT gunType;

            public GameManager gameManager;
            public bool isLocalRobot;

            [Client]
            public virtual void ConfirmLocalRobot() => isLocalRobot = true;

            public Attr GetAttr()
            {
                var damage = 1.0f;
                var armor = 0.0f;
                var coldDown = 1.0f;
                var revive = 0.0f;
                foreach (var b in Buffs)
                {
                    if (b.damageRate > damage) damage = b.damageRate;
                    if (b.armorRate > armor) armor = b.armorRate;
                    if (b.coolDownRate > coldDown) coldDown = b.coolDownRate;
                    if (b.reviveRate > revive) revive = b.reviveRate;
                }

                var attr = new Attr
                {
                    DamageRate = damage, ArmorRate = armor, ColdDownRate = coldDown, ReviveRate = revive
                };
                return attr;
            }

            [SyncVar] public bool registered = true;

            protected virtual void FixedUpdate()
            {
                if (isClient)
                {
                    if (!registered && isLocalRobot) CmdRegister();
                }

                if (isServer)
                {
                    foreach (var b in Buffs.Where(b => Time.time > b.timeOut))
                    {
                        Buffs.Remove(b);
                    }
                }
            }

            [Command(ignoreAuthority = true)]
            private void CmdRegister()
            {
                if (!registered) gameManager.RobotRegister(this);
                registered = true;
            }
        }
    }
}