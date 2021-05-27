using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Script.JudgeSystem.Role;
using UnityEngine;
using UnityWebSocket;

namespace Script.JudgeSystem
{
    public class Strategy
    {
        public string Title;
        public Dictionary<TypeT, string> Messages;
    }

    public static class StrategyTable
    {
        public static readonly Dictionary<int, Strategy> Table = new Dictionary<int, Strategy>
        {
            {
                1, new Strategy
                {
                    Title = "七分钟常规开局", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "前期：前哨站和环形高地防守。\n打符步兵：高地进行骚扰。"},
                        {TypeT.Engineer, "前期：取小资源岛矿石。\n到资源岛。"},
                        {TypeT.Hero, "前期：前哨站进攻！"}
                    }
                }
            },
            {
                2, new Strategy
                {
                    Title = "七分钟常规式中期", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "中期：注意打击地面。\n攻击哨兵！"},
                        {TypeT.Hero, "中期：注意输出前哨站！"},
                        {TypeT.Engineer, "中期：注意前哨站防守！"}
                    }
                }
            },
            {
                3, new Strategy
                {
                    Title = "七分钟常规式末期", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "末期：注意抢血！"},
                        {TypeT.Hero, "末期：注意抢血！"}
                    }
                }
            },
            {
                4, new Strategy
                {
                    Title = "第一波矿石", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.Engineer, "2、4号大矿石准备掉落！"}
                    }
                }
            },
            {
                5, new Strategy
                {
                    Title = "第二波矿石", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.Engineer, "1、3、5号大矿石准备掉落！"}
                    }
                }
            },
            {
                6, new Strategy
                {
                    Title = "飞镖警告", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "对方导弹已经发射！"},
                        {TypeT.Hero, "对方导弹已经发射！"},
                        {TypeT.Engineer, "对方导弹已经发射！"}
                    }
                }
            },
            {
                7, new Strategy
                {
                    Title = "基地禁区被突破", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "警告！基地禁区被突破！"},
                        {TypeT.Hero, "警告！基地禁区被突破！"},
                        {TypeT.Engineer, "警告！基地禁区被突破！"}
                    }
                }
            },
            {
                8, new Strategy
                {
                    Title = "己方血量低", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "xx（兵种）血量低，注意保护！"},
                        {TypeT.Hero, "xx（兵种）血量低，注意保护！"},
                        {TypeT.Engineer, "xx（兵种）血量低，注意保护！"}
                    }
                }
            },
            {
                9, new Strategy
                {
                    Title = "哨兵血量低", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "警告！哨兵血量低！！"},
                        {TypeT.Hero, "警告！哨兵血量低！！"},
                        {TypeT.Engineer, "警告！哨兵血量低！！"}
                    }
                }
            },
            {
                10, new Strategy
                {
                    Title = "前哨站血量低", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "警告！前哨站血量低！！"},
                        {TypeT.Hero, "警告！前哨站血量低！！"},
                        {TypeT.Engineer, "警告！前哨站血量低！！"}
                    }
                }
            },
            {
                11, new Strategy
                {
                    Title = "敌方残血", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "敌xx（兵种）血量低！"},
                        {TypeT.Hero, "敌xx（兵种）血量低！"}
                    }
                }
            },
            {
                12, new Strategy
                {
                    Title = "打符、增益", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "神符可被激活！"},
                        {TypeT.Hero, "神符可被激活！"}
                    }
                }
            }
        };
    }

    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Situation
    {
        public int RemainTime;
        public int BuffAvailable;
        public int AHP;
        public int FHP;
        public int SHP;
        public int inInvasion;
    }

    public class Decision
    {
        private readonly WebSocket _socket;
        public int Code = -1;

        public Decision()
        {
            try
            {
                _socket = new WebSocket("ws://127.0.0.1:8080");
                _socket.OnMessage += OnMessage;
                _socket.ConnectAsync();
            }
            catch
            {
                Debug.Log("No Decision Service.");
            }
        }

        public void Decide(Situation situation)
        {
            _socket?.SendAsync(JsonUtility.ToJson(situation));
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                Code = int.Parse(e.Data);
            }
            catch
            {
                Debug.Log("Invalid Decision Response.");
            }
        }
    }
}