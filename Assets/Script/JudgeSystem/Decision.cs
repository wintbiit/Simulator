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
                        {TypeT.InfantryA, "DEFENSE"},
                        {TypeT.Engineer, "MINERAL"},
                        {TypeT.Hero, "OUTPOST"}
                    }
                }
            },
            {
                2, new Strategy
                {
                    Title = "七分钟常规式中期", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "ATTACK"},
                        {TypeT.Hero, "OUTPOST"},
                        {TypeT.Engineer, "DEFENSE OUTPOST"}
                    }
                }
            },
            {
                3, new Strategy
                {
                    Title = "七分钟常规式末期", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "ROBLOOD"},
                        {TypeT.Hero, "ROBLOOD"}
                    }
                }
            },
            {
                4, new Strategy
                {
                    Title = "第一波矿石", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.Engineer, "NO.1 MINERAL"}
                    }
                }
            },
            {
                5, new Strategy
                {
                    Title = "第二波矿石", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.Engineer, "NO.2 MINERAL"}
                    }
                }
            },
            {
                6, new Strategy
                {
                    Title = "飞镖警告", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "MISSILE"},
                        {TypeT.Hero, "MISSILE"},
                        {TypeT.Engineer, "MISSILE"}
                    }
                }
            },
            {
                7, new Strategy
                {
                    Title = "基地禁区被突破", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "BASE UNDER ATTACK"},
                        {TypeT.Hero, "BASE UNDER ATTACK"},
                        {TypeT.Engineer, "BASE UNDER ATTACK"}
                    }
                }
            },
            {
                8, new Strategy
                {
                    Title = "己方血量低", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "LOW BLOOD"},
                        {TypeT.Hero, "LOW BLOOD"},
                        {TypeT.Engineer, "LOW BLOOD"}
                    }
                }
            },
            {
                9, new Strategy
                {
                    Title = "哨兵血量低", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "SE LOW BLOOD"},
                        {TypeT.Hero, "SE LOW BLOOD"},
                        {TypeT.Engineer, "SE LOW BLOOD"}
                    }
                }
            },
            {
                10, new Strategy
                {
                    Title = "前哨站血量低", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "OP LOW BLOOD"},
                        {TypeT.Hero, "OP LOW BLOOD"},
                        {TypeT.Engineer, "OP LOW BLOOD"}
                    }
                }
            },
            {
                11, new Strategy
                {
                    Title = "敌方残血", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "EN LOW BLOOD"},
                        {TypeT.Hero, "EN LOW BLOOD"}
                    }
                }
            },
            {
                12, new Strategy
                {
                    Title = "打符、增益", Messages = new Dictionary<TypeT, string>
                    {
                        {TypeT.InfantryA, "BUFF!"},
                        {TypeT.Hero, "BUFF!"}
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