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
        public enum ChassisT
        {
            Default = 0,
            Armor = 1,
            Power = 2
        }

        /*
         * 机器人发射装置类型（仅地面机器人适用）
         * 爆发型、快速冷却型、高射速型
         */
        public enum GunT
        {
            Default = 0,
            Burst = 1,
            ColdDown = 2,
            Velocity = 3
        }

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
            // 等级-类型-属性
            // 等级-类型-底盘-发射机构-属性
            public static readonly
                Dictionary<int, Dictionary<TypeT, Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>>> Table =
                    new Dictionary<int, Dictionary<TypeT, Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>>>
                    {
                        {
                            1, new Dictionary<TypeT, Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>>
                            {
                                {
                                    TypeT.Hero, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Default, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Default, new RobotLevel(
                                                        0, 0, 150, 100,
                                                        50, 10, 20,
                                                        7.5f, 8)
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 200, 200,
                                                        70, 10, 40,
                                                        7.5f, 8
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 200, 100,
                                                        70, 16, 20,
                                                        7.5f, 8
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 250, 200,
                                                        55, 10, 40,
                                                        7.5f, 8
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 250, 100,
                                                        55, 16, 20,
                                                        7.5f, 8
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.Engineer, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Default, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Default, new RobotLevel(
                                                        0, 0, 500, int.MaxValue,
                                                        90, 0, 0,
                                                        5, float.MaxValue)
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryA, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Default, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Default, new RobotLevel(
                                                        0, 0, 100, 50,
                                                        40, 15, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 150, 150,
                                                        60, 15, 15,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 150, 50,
                                                        60, 15, 40,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 150, 50,
                                                        60, 30, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 200, 150,
                                                        45, 15, 15,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 200, 50,
                                                        45, 15, 40,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 200, 50,
                                                        45, 30, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryB, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Default, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Default, new RobotLevel(
                                                        0, 0, 100, 50,
                                                        40, 15, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 150, 150,
                                                        60, 15, 15,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 150, 50,
                                                        60, 15, 40,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 150, 50,
                                                        60, 30, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 200, 150,
                                                        45, 15, 15,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 200, 50,
                                                        45, 15, 40,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 200, 50,
                                                        45, 30, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryC, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Default, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Default, new RobotLevel(
                                                        0, 0, 100, 50,
                                                        40, 15, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 150, 150,
                                                        60, 15, 15,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 150, 50,
                                                        60, 15, 40,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 150, 50,
                                                        60, 30, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 200, 150,
                                                        45, 15, 15,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 200, 50,
                                                        45, 15, 40,
                                                        2.5f, 3
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 200, 50,
                                                        45, 30, 10,
                                                        2.5f, 3
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.Guard, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Default, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Default, new RobotLevel(
                                                        500, 0, 600, int.MaxValue,
                                                        30, 30, 100,
                                                        7.5f, float.MaxValue
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.Drone, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Default, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Default, new RobotLevel(
                                                        0, 0, int.MaxValue, int.MaxValue,
                                                        int.MaxValue, 30, 100,
                                                        0, float.MaxValue
                                                    )
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },

                        {
                            2, new Dictionary<TypeT, Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>>
                            {
                                {
                                    TypeT.Hero, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 250, 350,
                                                        90, 10, 80,
                                                        10, 12
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 250, 200,
                                                        90, 16, 60,
                                                        10, 12
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 350, 350,
                                                        60, 10, 80,
                                                        10, 12
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 350, 200,
                                                        60, 16, 60,
                                                        10, 12
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryA, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 200, 280,
                                                        80, 15, 25,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 200, 100,
                                                        80, 18, 60,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 200, 100,
                                                        80, 30, 20,
                                                        5, 6
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 300, 280,
                                                        50, 15, 25,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 300, 100,
                                                        50, 18, 60,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 300, 100,
                                                        50, 30, 20,
                                                        5, 6
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryB, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 200, 280,
                                                        80, 15, 25,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 200, 100,
                                                        80, 18, 60,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 200, 100,
                                                        80, 30, 20,
                                                        5, 6
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 300, 280,
                                                        50, 15, 25,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 300, 100,
                                                        50, 18, 60,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 300, 100,
                                                        50, 30, 20,
                                                        5, 6
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryC, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 200, 280,
                                                        80, 15, 25,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 200, 100,
                                                        80, 18, 60,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 200, 100,
                                                        80, 30, 20,
                                                        5, 6
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 300, 280,
                                                        50, 15, 25,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 300, 100,
                                                        50, 18, 60,
                                                        5, 6
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 300, 100,
                                                        50, 30, 20,
                                                        5, 6
                                                    )
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },

                        {
                            3, new Dictionary<TypeT, Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>>
                            {
                                {
                                    TypeT.Hero, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 300, 500,
                                                        120, 10, 120,
                                                        15, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 300, 300,
                                                        120, 16, 100,
                                                        15, float.MaxValue
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 450, 500,
                                                        65, 10, 120,
                                                        15, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 450, 300,
                                                        65, 16, 100,
                                                        15, float.MaxValue
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryA, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 250, 400,
                                                        100, 15, 35,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 250, 150,
                                                        100, 18, 80,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 250, 150,
                                                        100, 30, 30,
                                                        7.5f, float.MaxValue
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 400, 400,
                                                        55, 15, 35,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 400, 150,
                                                        55, 18, 80,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 400, 150,
                                                        55, 30, 30,
                                                        7.5f, float.MaxValue
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryB, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 250, 400,
                                                        100, 15, 35,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 250, 150,
                                                        100, 18, 80,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 250, 150,
                                                        100, 30, 30,
                                                        7.5f, float.MaxValue
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 400, 400,
                                                        55, 15, 35,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 400, 150,
                                                        55, 18, 80,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 400, 150,
                                                        55, 30, 30,
                                                        7.5f, float.MaxValue
                                                    )
                                                }
                                            }
                                        }
                                    }
                                },
                                {
                                    TypeT.InfantryC, new Dictionary<ChassisT, Dictionary<GunT, RobotLevel>>
                                    {
                                        {
                                            ChassisT.Power, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 250, 400,
                                                        100, 15, 35,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 250, 150,
                                                        100, 18, 80,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 250, 150,
                                                        100, 30, 30,
                                                        7.5f, float.MaxValue
                                                    )
                                                }
                                            }
                                        },
                                        {
                                            ChassisT.Armor, new Dictionary<GunT, RobotLevel>
                                            {
                                                {
                                                    GunT.Burst, new RobotLevel(
                                                        0, 0, 400, 400,
                                                        55, 15, 35,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.ColdDown, new RobotLevel(
                                                        0, 0, 400, 150,
                                                        55, 18, 80,
                                                        7.5f, float.MaxValue
                                                    )
                                                },
                                                {
                                                    GunT.Velocity, new RobotLevel(
                                                        0, 0, 400, 150,
                                                        55, 30, 30,
                                                        7.5f, float.MaxValue
                                                    )
                                                }
                                            }
                                        }
                                    }
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

        public class RobotBaseRecord
        {
            public int Id;
            public RoleT Role;
            public int Level;
            public int Health;
            public int SmallAmmo;
            public int LargeAmmo;
            public float Experience;
            public float Heat;
            public Vector3 Position;
            public Quaternion Rotation;
            public List<BuffBase> Buffs = new List<BuffBase>();
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
            [SyncVar] public ChassisT chassisType;
            [SyncVar] public GunT gunType;

            [SyncVar] public int health;
            [SyncVar] public int smallAmmo;
            [SyncVar] public int largeAmmo;
            [SyncVar] public float experience;

            [SyncVar] public float heat;

            public readonly SyncList<BuffBase> Buffs = new SyncList<BuffBase>();

            public GameManager gameManager;
            public bool isLocalRobot;

            [Server]
            protected void RecordFrame(RobotBaseRecord record)
            {
                record.Id = id;
                record.Role = role;
                record.Level = level;
                record.Health = health;
                record.SmallAmmo = smallAmmo;
                record.LargeAmmo = largeAmmo;
                record.Experience = experience;
                record.Heat = heat;
                var t = transform;
                record.Position = t.position;
                record.Rotation = t.rotation;
                foreach (var buff in Buffs)
                    record.Buffs.Add(buff);
            }

            [Client]
            public virtual void ConfirmLocalRobot()
            {
                isLocalRobot = true;
                CmdConfirmed();
            }

            [Command(ignoreAuthority = true)]
            private void CmdConfirmed() => gameManager.confirmedCount++;

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
            private bool _ptzRegistered;

            protected virtual void FixedUpdate()
            {
                if (isClient)
                {
                    if (!registered && isLocalRobot) CmdRegister();
                    if (role.Type == TypeT.Ptz && !_ptzRegistered)
                    {
                        CmdPtzRegister();
                        _ptzRegistered = true;
                    }
                }

                if (isServer)
                {
                    foreach (var b in Buffs.Where(b => Time.time > b.timeOut))
                    {
                        Buffs.Remove(b);
                    }

                    if (chassisType != ChassisT.Default)
                    {
                        if (experience > RobotPerformanceTable.Table[level][role.Type][chassisType][gunType].ExpUpgrade)
                        {
                            if (level < 3)
                            {
                                experience -= RobotPerformanceTable.Table[level][role.Type][chassisType][gunType]
                                    .ExpUpgrade;
                                level++;
                            }
                        }
                    }
                }
            }

            [Command(ignoreAuthority = true)]
            private void CmdRegister()
            {
                if (!registered) gameManager.RobotRegister(this);
                registered = true;
            }

            [Command(ignoreAuthority = true)]
            private void CmdPtzRegister()
            {
                gameManager.PtzRegister();
            }
        }
    }
}