using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.Event;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Lobby;
using UnityEngine;
using TypeT = Script.JudgeSystem.Event.TypeT;

namespace Script.Networking
{
    namespace Game
    {
        // 用于存放队伍出生点的结构
        [Serializable]
        public class CampStart
        {
            public Transform hero;
            public Transform engineer;
            public Transform infantryA;
            public Transform infantryB;
            public Transform infantryC;
            public Transform drone;
            public Transform guard;
        }

        [Serializable]
        public class TimeEventTrigger
        {
            public int time;
            public TypeT e;
            public bool triggered;

            public TimeEventTrigger(int time, TypeT e)
            {
                this.time = time;
                this.e = e;
                triggered = false;
            }
        }

        /*
         * 比赛管理器
         * + 保存队伍出生点
         * （以下待实现）
         * + 倒计时
         * + 表驱动的赛场事件
         * + 比赛状态记录
         * + 比赛中可能需要的服务器调用等
         */
        public class GameManager : NetworkBehaviour
        {
            private RoomManager _roomManager;
            private readonly Dictionary<int, RobotBase> _robotBases = new Dictionary<int, RobotBase>();
            private readonly List<RoleT> _roles = new List<RoleT>();
            private readonly Queue<GameEventBase> _eventQueue = new Queue<GameEventBase>();

            public CampStart redStart;
            public CampStart blueStart;

            public List<TimeEventTrigger> timeEventTriggers = new List<TimeEventTrigger>
            {
                new TimeEventTrigger(6*60, TypeT.SixMinute),
                new TimeEventTrigger(5*60, TypeT.FiveMinute),
                new TimeEventTrigger(4*60, TypeT.FourMinute),
                new TimeEventTrigger(3*60, TypeT.ThreeMinute),
                new TimeEventTrigger(2*60, TypeT.TwoMinute),
                new TimeEventTrigger(1*60, TypeT.OneMinute),
                new TimeEventTrigger(0, TypeT.GameOver)
            };

            [SyncVar] private int _countDown;
            private bool _playing;
            private int _startTime;

            #region Server

            [Server]
            public void RoomManagerRegister(RoomManager roomManager)
            {
                _roomManager = roomManager;
            }

            [Server]
            public void RobotRegister(RobotBase robotBase)
            {
                _robotBases.Add(robotBase.id, robotBase);
                if (_roles.Contains((robotBase.Role)))
                    _roles.Remove(robotBase.Role);
                // 判断满
                if (_roles.Where(
                        r => r.Camp != CampT.Judge
                             && r.Camp != CampT.Unknown)
                    .Count(
                        r => r.Type != JudgeSystem.Role.TypeT.Unknown
                             && r.Type != JudgeSystem.Role.TypeT.Ptz) == 0)
                    Emit(new TimeEvent(TypeT.GameStart));
            }

            [Server]
            public void PlayerRegister(GamePlayer gamePlayer)
            {
                _roles.Add(gamePlayer.Role);
            }

            [Server]
            public void Emit(GameEventBase e)
            {
                _eventQueue.Enqueue(e);
            }

            private void FixedUpdate()
            {
                if (!isServer) return;
                // 倒计时
                if (_playing)
                {
                    _countDown = 7 * 60 - ((int) Time.time - _startTime);
                    // 时序事件
                    if (timeEventTriggers.Any(t => t.time == _countDown && !t.triggered))
                    {
                        var trigger = timeEventTriggers.First(t => t.time == _countDown);
                        Emit(new TimeEvent(trigger.e));
                        trigger.triggered = true;
                    }
                }

                // 处理事件队列
                for (var i = 0; i < 5; i++)
                {
                    if (_eventQueue.Count <= 0) return;
                    var e = _eventQueue.Dequeue();
                    switch (e.Type)
                    {
                        case TypeT.Unknown:
                            break;
                        case TypeT.Hit:
                            var hitEvent = (HitEvent) e;
                            var caliberDamage = hitEvent.Caliber == CaliberT.Small ? 10 : 100;
                            // 计算攻击力与护甲加成效果
                            var damage = _robotBases[hitEvent.Hitter].damageRate * caliberDamage;
                            var protect = damage / _robotBases[hitEvent.Target].armorRate;
                            _robotBases[hitEvent.Target].health -= (int) protect;
                            Debug.Log(hitEvent.Target.ToString()
                                      + _robotBases[hitEvent.Target].health);
                            break;
                        case TypeT.GameStart:
                            _startTime = (int) Time.time;
                            _playing = true;
                            Debug.Log(e.Type);
                            break;
                        case TypeT.SixMinute:
                            Debug.Log(e.Type);
                            break;
                        case TypeT.FiveMinute:
                            Debug.Log(e.Type);
                            break;
                        case TypeT.FourMinute:
                            Debug.Log(e.Type);
                            break;
                        case TypeT.ThreeMinute:
                            Debug.Log(e.Type);
                            break;
                        case TypeT.TwoMinute:
                            Debug.Log(e.Type);
                            break;
                        case TypeT.OneMinute:
                            Debug.Log(e.Type);
                            break;
                        case TypeT.GameOver:
                            _playing = false;
                            Debug.Log("Game over!");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            #endregion
        }
    }
}