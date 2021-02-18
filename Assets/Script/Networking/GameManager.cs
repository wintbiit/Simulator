using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.Controller.Bullet;
using Script.JudgeSystem.Event;
using Script.JudgeSystem.Facility;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Lobby;
using TMPro;
using UnityEngine;
using TypeT = Script.JudgeSystem.Role.TypeT;

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
            public Transform campBase;
        }

        [Serializable]
        public class TimeEventTrigger
        {
            public int time;
            public JudgeSystem.Event.TypeT e;
            public bool triggered;
        }

        [Serializable]
        public class HealthDisplay
        {
            public TypeT type;
            public RectTransform bar;
            public float width;
        }

        /*
         * 比赛管理器
         * + 保存队伍出生点
         * + 倒计时
         * （以下待实现）
         * + 表驱动的赛场事件
         * + 比赛状态记录
         * + 比赛中可能需要的服务器调用等
         */
        public class GameManager : NetworkBehaviour
        {
            // Server side
            private RoomManager _roomManager;
            private readonly Dictionary<int, RobotBase> _robotBases = new Dictionary<int, RobotBase>();
            private readonly Dictionary<int, FacilityBase> _facilityBases = new Dictionary<int, FacilityBase>();
            private readonly List<RoleT> _roles = new List<RoleT>();
            private readonly List<GamePlayer> _players = new List<GamePlayer>();
            private readonly Queue<GameEventBase> _eventQueue = new Queue<GameEventBase>();
            private RobotBase _localRobot;

            public int gameTime;

            public CampStart redStart;
            public CampStart blueStart;

            public List<HealthDisplay> redHealthDisplays = new List<HealthDisplay>();
            public List<HealthDisplay> blueHealthDisplays = new List<HealthDisplay>();

            public TMP_Text countDownDisplay;
            public TMP_Text smallAmmoDisplay;
            public TMP_Text largeAmmoDisplay;
            public TMP_Text expDisplay;

            public GameObject resultPanel;
            public TMP_Text resultTitle;

            private readonly List<TimeEventTrigger> _timeEventTriggers = new List<TimeEventTrigger>
            {
                new TimeEventTrigger {time = 6 * 60, e = JudgeSystem.Event.TypeT.SixMinute},
                new TimeEventTrigger {time = 5 * 60, e = JudgeSystem.Event.TypeT.FiveMinute},
                new TimeEventTrigger {time = 4 * 60, e = JudgeSystem.Event.TypeT.FourMinute},
                new TimeEventTrigger {time = 3 * 60, e = JudgeSystem.Event.TypeT.ThreeMinute},
                new TimeEventTrigger {time = 2 * 60, e = JudgeSystem.Event.TypeT.TwoMinute},
                new TimeEventTrigger {time = 1 * 60, e = JudgeSystem.Event.TypeT.OneMinute},
                new TimeEventTrigger {time = 0, e = JudgeSystem.Event.TypeT.GameOver}
            };

            [SyncVar] private int _countDown;
            [SyncVar] private bool _playing;
            [SyncVar] private bool _finished;
            [SyncVar] private int _startTime;
            [SyncVar] private int _finishTime;

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

                if (_roles.Contains((robotBase.role)))
                    _roles.Remove(robotBase.role);
                // 判断满
                if (_roles.Where(
                        r => r.Camp != CampT.Judge
                             && r.Camp != CampT.Unknown)
                    .Count(
                        r => r.Type != TypeT.Unknown
                             && r.Type < TypeT.Ptz) == 0)
                    Emit(new TimeEvent(JudgeSystem.Event.TypeT.GameStart));
            }

            [Server]
            public void FacilityRegister(FacilityBase facilityBase)
            {
                _facilityBases.Add(facilityBase.id, facilityBase);
            }

            [Server]
            public void PlayerRegister(GamePlayer gamePlayer)
            {
                _players.Add(gamePlayer);
                _roles.Add(gamePlayer.role);
            }

            [Server]
            public void Emit(GameEventBase e)
            {
                _eventQueue.Enqueue(e);
            }

            [Server]
            private void ServerStart()
            {
                _countDown = gameTime;
            }

            [Server]
            private bool IsRedWin()
            {
                var redBase = _facilityBases
                    .First(
                        f =>
                            f.Value.role.Equals(
                                new RoleT(CampT.Red, TypeT.Base))).Value;
                var blueBase = _facilityBases
                    .First(
                        f =>
                            f.Value.role.Equals(
                                new RoleT(CampT.Blue, TypeT.Base))).Value;
                return redBase.health > blueBase.health;
            }

            [Server]
            private void ServerFixedUpdate()
            {
                // 倒计时
                if (_playing || _finished)
                {
                    _countDown = gameTime - ((int) Time.time - _startTime);
                    // 时序事件
                    if (_timeEventTriggers.Any(t => t.time == _countDown && !t.triggered))
                    {
                        var trigger = _timeEventTriggers.First(t => t.time == _countDown);
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
                        case JudgeSystem.Event.TypeT.Unknown:
                            break;
                        case JudgeSystem.Event.TypeT.Hit:
                            var hitEvent = (HitEvent) e;
                            var caliberDamage = hitEvent.Caliber == CaliberT.Small ? 10 : 100;
                            // 计算攻击力与护甲加成效果
                            var damage = _robotBases[hitEvent.Hitter].damageRate * caliberDamage;

                            if (_robotBases.Keys.Contains(hitEvent.Target))
                            {
                                if (_robotBases[hitEvent.Target].health <= 0) break;
                                var protect = damage / _robotBases[hitEvent.Target].armorRate;
                                _robotBases[hitEvent.Target].health -= (int) protect;
                                if (_robotBases[hitEvent.Target].health <= 0)
                                {
                                    _robotBases[hitEvent.Target].health = 0;
                                    _robotBases[hitEvent.Hitter].experience +=
                                        RobotPerformanceTable.Table[_robotBases[hitEvent.Target].level][
                                            _robotBases[hitEvent.Target].role.Type].ExpValue;
                                }
                            }

                            if (_facilityBases.Keys.Contains(hitEvent.Target))
                            {
                                var protect = damage / _facilityBases[hitEvent.Target].armorRate;
                                _facilityBases[hitEvent.Target].health -= (int) protect;
                                if (_facilityBases[hitEvent.Target].health <= 0)
                                {
                                    if (_facilityBases[hitEvent.Target].role.Type == TypeT.Base)
                                        Emit(new TimeEvent(JudgeSystem.Event.TypeT.GameOver));
                                }
                            }

                            break;
                        case JudgeSystem.Event.TypeT.GameStart:
                            _startTime = (int) Time.time;
                            _playing = true;
                            foreach (var player in _players)
                                player.OnAllReady();
                            break;
                        case JudgeSystem.Event.TypeT.SixMinute:
                            break;
                        case JudgeSystem.Event.TypeT.FiveMinute:
                            break;
                        case JudgeSystem.Event.TypeT.FourMinute:
                            break;
                        case JudgeSystem.Event.TypeT.ThreeMinute:
                            break;
                        case JudgeSystem.Event.TypeT.TwoMinute:
                            break;
                        case JudgeSystem.Event.TypeT.OneMinute:
                            break;
                        case JudgeSystem.Event.TypeT.GameOver:
                            if (_finished) break;
                            _playing = false;
                            _finished = true;
                            _finishTime = (int) Time.time;
                            RpcOnClientGameOver(IsRedWin());
                            break;
                        case JudgeSystem.Event.TypeT.Reset:
                            _roomManager.ResetServer();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            [Command(ignoreAuthority = true)]
            private void CmdReset()
            {
                Emit(new TimeEvent(JudgeSystem.Event.TypeT.Reset));
            }

            #endregion

            #region Client

            [Client]
            public void LocalRobotRegister(RobotBase robot) => _localRobot = robot;

            private List<RobotBase> _clientRobotBases;
            private List<FacilityBase> _clientFacilityBases;
            
            [Client]
            private void ClientStart()
            {
                resultTitle.text = "";
                resultPanel.SetActive(false);
                foreach (var hd in redHealthDisplays)
                {
                    hd.width = hd.bar.rect.width;
                    hd.bar.sizeDelta = new Vector2(hd.width * -2, 0);
                    hd.bar.offsetMin = Vector2.zero;
                }

                foreach (var hd in blueHealthDisplays)
                {
                    hd.width = hd.bar.rect.width;
                    hd.bar.sizeDelta = new Vector2(hd.width * -2, 0);
                    hd.bar.offsetMax = Vector2.zero;
                }

                _clientRobotBases = new List<RobotBase>(FindObjectsOfType<RobotBase>());
                _clientFacilityBases = new List<FacilityBase>(FindObjectsOfType<FacilityBase>());
            }

            [ClientRpc]
            private void RpcOnClientGameOver(bool redWin)
            {
                foreach (var robot in _clientRobotBases)
                    robot.isLocalRobot = false;

                if (_localRobot != null)
                {
                    if (_localRobot.role.Camp == CampT.Red && redWin
                        || _localRobot.role.Camp == CampT.Blue && !redWin)
                        resultTitle.text = "胜利";
                    else
                        resultTitle.text = "失败";
                }

                resultPanel.SetActive(true);
            }

            [Client]
            private void ClientFixedUpdate()
            {
                foreach (var r in _clientRobotBases)
                {
                    if (r.role.Type == TypeT.Guard) continue;
                    var healthDisplay = r.role.Camp == CampT.Red
                        ? redHealthDisplays.First(hd => hd.type == r.role.Type)
                        : blueHealthDisplays.First(hd => hd.type == r.role.Type);
                    var healthRate = (float) r.health / RobotPerformanceTable.Table[r.level][r.role.Type].HealthLimit;
                    healthDisplay.bar.sizeDelta = new Vector2(
                        healthDisplay.width * (healthRate - 1), 0);
                    healthDisplay.bar.offsetMin = Vector2.zero;
                }
                foreach (var f in _clientFacilityBases)
                {
                    var healthDisplay = f.role.Camp == CampT.Red
                        ? redHealthDisplays.First(hd => hd.type == f.role.Type)
                        : blueHealthDisplays.First(hd => hd.type == f.role.Type);
                    var healthRate = (float) f.health / 2000;
                    healthDisplay.bar.sizeDelta = new Vector2(
                        healthDisplay.width * (healthRate - 1), 0);
                    healthDisplay.bar.offsetMin = Vector2.zero;
                }

                var minute = (int) Math.Floor(_countDown / 60.0f);
                var second = _countDown % 60;
                if (minute == 0 && second <= 10)
                    countDownDisplay.color = Color.red;
                if (_finished)
                {
                    countDownDisplay.color = Color.red;
                    minute = 0;
                    second = 10 + (_countDown - (gameTime - (_finishTime - _startTime)));
                    if (second == 0)
                        CmdReset();
                }

                countDownDisplay.text = minute + ":" + (second < 10 ? "0" : "") + second;

                if (_localRobot == null) return;
                smallAmmoDisplay.text = "17mm: " + _localRobot.smallAmmo;
                largeAmmoDisplay.text = "42mm: " + _localRobot.largeAmmo;
                expDisplay.text = "EXP: " + _localRobot.experience;
            }

            #endregion

            private void Start()
            {
                if (isServer) ServerStart();
                if (isClient) ClientStart();
            }

            private void FixedUpdate()
            {
                if (isServer) ServerFixedUpdate();
                if (isClient) ClientFixedUpdate();
            }
        }
    }
}