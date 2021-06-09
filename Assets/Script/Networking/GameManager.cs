using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mirror;
using Script.Controller;
using Script.Controller.Bullet;
using Script.Controller.Engineer;
using Script.Controller.Hero;
using Script.Controller.Infantry;
using Script.JudgeSystem;
using Script.JudgeSystem.Event;
using Script.JudgeSystem.Facility;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Lobby;
using Script.UI;
using Script.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = System.Random;
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
            public Transform campOutpost;
            public Transform campBuff;
        }

        [Serializable]
        public class TimeEventTrigger
        {
            public int time;
            public JudgeSystem.Event.TypeT e;
            public bool triggered;
        }


        [Serializable]
        public class CampStatus
        {
            public int money;
            public bool virtualShield = true;
            public int infantrySupplyAmount;
            public int heroSupplyAmount;
            public int moneyObtainAmount;
            public int airRaidAmount;
            public int damage;

            public CampStatus DeepCopy()
            {
                return new CampStatus
                {
                    money = money,
                    virtualShield = virtualShield,
                    infantrySupplyAmount = infantrySupplyAmount,
                    heroSupplyAmount = heroSupplyAmount,
                    moneyObtainAmount = moneyObtainAmount,
                    airRaidAmount = airRaidAmount,
                    damage = damage
                };
            }
        }

        [Serializable]
        public class GlobalStatus
        {
            public int countDown;
            public bool playing;
            public bool finished;
            public int startTime;
            public int finishTime;
            public bool smallBuffStart;
            public bool smallBuffEnable;
            public bool largeBuffStart;
            public bool largeBuffEnable;
            public float smallBuffColdDown;
            public float largeBuffColdDown;

            public GlobalStatus DeepCopy()
            {
                return new GlobalStatus
                {
                    countDown = countDown,
                    playing = playing,
                    finished = finished,
                    startTime = startTime,
                    finishTime = finishTime,
                    smallBuffStart = smallBuffStart,
                    smallBuffEnable = smallBuffEnable,
                    largeBuffStart = largeBuffStart,
                    largeBuffEnable = largeBuffEnable,
                    smallBuffColdDown = smallBuffColdDown,
                    largeBuffColdDown = largeBuffColdDown
                };
            }
        }

        public class RecordFrame
        {
            public GlobalStatus GlobalStatus;
            public CampStatus RedStatus;
            public CampStatus BlueStatus;
            public readonly List<RobotBaseRecord> RobotBaseRecords = new List<RobotBaseRecord>();
            public readonly List<FacilityBaseRecord> FacilityBaseRecords = new List<FacilityBaseRecord>();
            public readonly List<BlockControllerRecord> BlockControllerRecords = new List<BlockControllerRecord>();
            public readonly List<MineControllerRecord> MineControllers = new List<MineControllerRecord>();
        }

        /*
         * 比赛管理器
         * + 保存队伍出生点
         * + 倒计时
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
            private int _gameTime;

            public JudgeController judge;

            [HideInInspector] public int ptzCount;
            [HideInInspector] public int confirmedCount;

            public CampStart redStart;
            public CampStart blueStart;
            public Transform judgeStart;

            public List<Transform> silverStart = new List<Transform>();
            public List<Transform> goldStart = new List<Transform>();
            public List<Transform> blockStart = new List<Transform>();

            public HUDManager hudManager;

            public GameObject resultPanel;
            public TMP_Text resultTitle;

            public GameObject optionsPanel;
            public Slider sensitivitySlide;
            public TMP_Dropdown chassisTypeSelect;
            public TMP_Dropdown gunTypeSelect;
            public Button typeConfirm;

            public GameObject loadingHint;
            public GameObject blurLayer;

            [HideInInspector] public GroundControllerBase observing;

            [Header("Drone Camera")] public GameObject redDCam;
            public GameObject blueDCam;

            [Header("Radar Camera")] public GameObject redRCam;
            public GameObject blueRCam;
            public GameObject redRCCam;
            public GameObject blueRCCam;

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

            private bool _started;
            public List<RobotBase> clientRobotBases = new List<RobotBase>();
            public List<FacilityBase> clientFacilityBases = new List<FacilityBase>();

            public List<int> mineDropTimes = new List<int>();

            // Data Refactor
            [SyncVar] public GlobalStatus globalStatus = new GlobalStatus();

            public readonly SyncDictionary<CampT, CampStatus> CampStatusMap = new SyncDictionary<CampT, CampStatus>
            {
                {CampT.Red, new CampStatus()},
                {CampT.Blue, new CampStatus()}
            };

            // private readonly List<RecordFrame> _recordFrames = new List<RecordFrame>();

            #region Server

            [Server]
            public List<GamePlayer> GetPlayers()
            {
                return _players;
            }

            [Server]
            public void PlayerLeave(int id)
            {
                _players.RemoveAll(p => p.index == id);
            }

            [Server]
            public void RoomManagerRegister(RoomManager roomManager)
            {
                _roomManager = roomManager;
            }

            [Server]
            public void PtzRegister()
            {
                ptzCount--;
            }

            [Server]
            public void RobotRegister(RobotBase robotBase)
            {
                if (!_robotBases.ContainsKey(robotBase.id))
                    _robotBases.Add(robotBase.id, robotBase);

                if (_roles.Contains((robotBase.role)))
                    _roles.Remove(robotBase.role);
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
                var rand = new Random();
                _gameTime = _roomManager.roomSlots.Count == 1 ? 430 : 450;
                globalStatus.countDown = _gameTime;
                mineDropTimes.Add(405);
                mineDropTimes.Add(405);
                var seed = rand.Next(0, 2);
                for (var i = 0; i < 2; i++)
                    mineDropTimes[i] -= (seed + i) % 2 == 0 ? 5 : 0;
                mineDropTimes.Add(240);
                mineDropTimes.Add(240);
                mineDropTimes.Add(240);
                seed = rand.Next(0, 3);
                for (var i = 0; i < 3; i++)
                {
                    var order = i - seed;
                    if (order < 0) order = 2 + order + 1;
                    mineDropTimes[i + 2] -= 5 * order;
                }

                var tmp = mineDropTimes[3];
                mineDropTimes[3] = mineDropTimes[0];
                mineDropTimes[0] = tmp;
            }

            [Server]
            private int IsRedWin()
            {
                var redBase = _facilityBases
                    .First(
                        f =>
                            f.Value.role.Equals(
                                new RoleT(CampT.Red, TypeT.Base)))
                    .Value;
                var blueBase = _facilityBases
                    .First(
                        f =>
                            f.Value.role.Equals(
                                new RoleT(CampT.Blue, TypeT.Base)))
                    .Value;
                if (redBase.health != blueBase.health)
                    return redBase.health > blueBase.health ? 1 : -1;
                var redGuard = _robotBases
                    .First(
                        r =>
                            r.Value.role.Equals(
                                new RoleT(CampT.Red, TypeT.Guard))).Value;
                var blueGuard = _robotBases
                    .First(
                        r =>
                            r.Value.role.Equals(
                                new RoleT(CampT.Blue, TypeT.Guard))).Value;
                if (redGuard.health != blueGuard.health)
                    return redGuard.health > blueGuard.health ? 1 : -1;
                var redOutpost = _facilityBases
                    .First(
                        f =>
                            f.Value.role.Equals(
                                new RoleT(CampT.Red, TypeT.Outpost)))
                    .Value;
                var blueOutpost = _facilityBases
                    .First(
                        f =>
                            f.Value.role.Equals(
                                new RoleT(CampT.Blue, TypeT.Outpost)))
                    .Value;
                if (redOutpost.health != blueOutpost.health)
                    return redOutpost.health > blueOutpost.health ? 1 : -1;
                if (CampStatusMap[CampT.Red].damage != CampStatusMap[CampT.Blue].damage)
                    return CampStatusMap[CampT.Red].damage > CampStatusMap[CampT.Blue].damage ? 1 : -1;
                return 0;
            }

            [Command(requiresAuthority = false)]
            private void CmdExchange(CampT camp, int value)
            {
                if (CampStatusMap.ContainsKey(camp))
                {
                    CampStatusMap[camp].money += value;
                    CampStatusMap[camp].moneyObtainAmount += value;
                }
            }

            [Command(requiresAuthority = false)]
            private void CmdSupply(RoleT role, int origin)
            {
                if (role.IsInfantry())
                {
                    var i = _robotBases.First(r => r.Value.role.Equals(role)).Value;
                    if (CampStatusMap[role.Camp].money >= 50)
                    {
                        if (CampStatusMap[role.Camp].infantrySupplyAmount < 1500)
                        {
                            CampStatusMap[role.Camp].money -= 50;
                            CampStatusMap[role.Camp].infantrySupplyAmount += 50;
                            i.smallAmmo = origin + 50;
                            ((InfantryController) i).Supply(origin + 50);
                        }
                    }
                }

                if (role.Type == TypeT.Hero)
                {
                    var h = _robotBases.First(r => r.Value.role.Equals(role)).Value;
                    if (CampStatusMap[role.Camp].money >= 75)
                    {
                        if (CampStatusMap[role.Camp].heroSupplyAmount < 100)
                        {
                            CampStatusMap[role.Camp].money -= 75;
                            CampStatusMap[role.Camp].heroSupplyAmount += 5;
                            h.largeAmmo = origin + 5;
                            ((HeroController) h).Supply(origin + 5);
                        }
                    }
                }
            }

            [Command(requiresAuthority = false)]
            public void CmdPunish(CampT camp, int time) => PunishRpc(camp, time);

            // [Server]
            // private void ResumeRecord(int frame)
            // {
            //     if (frame >= 0 && frame < _recordFrames.Count)
            //     {
            //         if (frame < _recordFrames.Count - 1)
            //             _recordFrames.RemoveRange(frame + 1, _recordFrames.Count - frame - 1);
            //         // Resume Global & Camp
            //         globalStatus = _recordFrames[frame].GlobalStatus;
            //         _campStatus[CampT.Red] = _recordFrames[frame].RedStatus;
            //         _campStatus[CampT.Blue] = _recordFrames[frame].BlueStatus;
            //         var smallBuffCdOffset = globalStatus.smallBuffColdDown - globalStatus.startTime;
            //         var largeBuffCdOffset = globalStatus.largeBuffColdDown - globalStatus.startTime;
            //         globalStatus.startTime = (int) Time.time - (gameTime - globalStatus.countDown);
            //         globalStatus.smallBuffColdDown = globalStatus.startTime + smallBuffCdOffset;
            //         globalStatus.largeBuffColdDown = globalStatus.startTime + largeBuffCdOffset;
            //         // Resume Robots
            //         // Resume Facilities
            //         // Resume Mine & Blocks
            //         // Other status
            //         // TimeEvents
            //     }
            // }

            [Server]
            private void RecordFrame()
            {
                var newRecord = new RecordFrame
                {
                    GlobalStatus = globalStatus.DeepCopy(),
                    RedStatus = CampStatusMap[CampT.Red].DeepCopy(),
                    BlueStatus = CampStatusMap[CampT.Blue].DeepCopy(),
                };
                // 强制数据同步   
                globalStatus = newRecord.GlobalStatus;
                CampStatusMap[CampT.Red] = newRecord.RedStatus;
                CampStatusMap[CampT.Blue] = newRecord.BlueStatus;
                // foreach (var rb in FindObjectsOfType<RobotBase>())
                // {
                //     if (rb.role.IsInfantry())
                //         newRecord.RobotBaseRecords.Add(((InfantryController) rb).RecordFrame());
                //     else
                //         switch (rb.role.Type)
                //         {
                //             case TypeT.Engineer:
                //                 newRecord.RobotBaseRecords.Add(((EngineerController) rb).RecordFrame());
                //                 break;
                //             case TypeT.Hero:
                //                 newRecord.RobotBaseRecords.Add(((HeroController) rb).RecordFrame());
                //                 break;
                //             case TypeT.Drone:
                //                 newRecord.RobotBaseRecords.Add(((DroneController) rb).RecordFrame());
                //                 break;
                //             case TypeT.Guard:
                //                 newRecord.RobotBaseRecords.Add(((GuardController) rb).RecordFrame());
                //                 break;
                //         }
                // }
                //
                // foreach (var fb in FindObjectsOfType<FacilityBase>())
                // {
                //     switch (fb.role.Type)
                //     {
                //         case TypeT.Base:
                //             newRecord.FacilityBaseRecords.Add(((BaseController) fb).RecordFrame());
                //             break;
                //         case TypeT.EnergyMechanism:
                //             newRecord.FacilityBaseRecords.Add(((EnergyMechanismController) fb).RecordFrame());
                //             break;
                //         case TypeT.Outpost:
                //             newRecord.FacilityBaseRecords.Add(((OutpostController) fb).RecordFrame());
                //             break;
                //     }
                // }
                //
                // foreach (var bc in FindObjectsOfType<BlockController>())
                // {
                //     newRecord.BlockControllerRecords.Add(bc.RecordFrame());
                // }
                //
                // foreach (var mc in FindObjectsOfType<MineController>())
                // {
                //     newRecord.MineControllers.Add(mc.RecordFrame());
                // }
                //
                // _recordFrames.Add(newRecord);
                // Debug.Log(_recordFrames.Count);
                // FacilityBase ok
                //  BaseController ok
                //  EnergyMechanismController ok
                //  OutpostController ok
                // RobotBase ok
                //  GroundControllerBase ok
                //      EngineerController ok
                //      HeroController ok
                //      InfantryController ok
                //  DroneController ok
                //  GuardController ok
                // BlockController ok
                // GoldIndicatorController skip?
                // MineController ok
            }

            [Server]
            private void ServerFixedUpdate()
            {
                if (_started) // && !globalStatus.finished)
                {
                    RecordFrame();
                }
                //
                // if (Input.GetKeyDown(KeyCode.P))
                // {
                //     ResumeRecord(300);
                // }

                if (!_started && confirmedCount == _roomManager.roomSlots.Count)
                {
                    _started = true;
                    Emit(new TimeEvent(JudgeSystem.Event.TypeT.GameStart));
                }

                // 倒计时
                if (globalStatus.playing || globalStatus.finished)
                {
                    if (!globalStatus.finished)
                        globalStatus.countDown = _gameTime - ((int) Time.time - globalStatus.startTime);
                    else
                        globalStatus.countDown = 16 - (int) (Time.time - globalStatus.finishTime);
                    // 时序事件
                    if (_timeEventTriggers.Any(t => t.time == globalStatus.countDown && !t.triggered))
                    {
                        var trigger = _timeEventTriggers.First(t => t.time == globalStatus.countDown);
                        Emit(new TimeEvent(trigger.e));
                        trigger.triggered = true;
                    }
                }

                if (!isServerOnly) clientRobotBases = new List<RobotBase>(FindObjectsOfType<RobotBase>());

                // 处理事件队列
                for (var i = 0; i < 5; i++)
                {
                    if (_eventQueue.Count <= 0) break;
                    var e = _eventQueue.Dequeue();
                    switch (e.Type)
                    {
                        case JudgeSystem.Event.TypeT.Unknown:
                            break;
                        case JudgeSystem.Event.TypeT.Hit:
                            var hitEvent = (HitEvent) e;
                            var caliberDamage = hitEvent.Caliber == CaliberT.Small ? 10 : 100;
                            // 计算攻击力与护甲加成效果
                            float damage = 0;
                            if (hitEvent.Caliber != CaliberT.Dart)
                                damage = _robotBases[hitEvent.Hitter].GetAttr().DamageRate * caliberDamage;

                            if (_robotBases.Keys.Contains(hitEvent.Target))
                            {
                                if (_robotBases[hitEvent.Target].health <= 0) break;
                                if (_robotBases[hitEvent.Target].role.Type == TypeT.Guard)
                                {
                                    if (_facilityBases.First(f =>
                                        f.Value.role.Equals(new RoleT(_robotBases[hitEvent.Target].role.Camp,
                                            TypeT.Outpost))).Value.health > 0)
                                        break;
                                }

                                var protect = damage * (1 - _robotBases[hitEvent.Target].GetAttr().ArmorRate);
                                if (_robotBases[hitEvent.Target].health > 0)
                                {
                                    _robotBases[hitEvent.Target].health -= (int) protect;
                                    CampStatusMap[_robotBases[hitEvent.Hitter].role.Camp].damage += (int) protect;
                                    if (_robotBases.ContainsKey(hitEvent.Hitter) &&
                                        _robotBases[hitEvent.Hitter].role.Type == TypeT.Guard &&
                                        _robotBases[hitEvent.Hitter].health > 0)
                                    {
                                        _robotBases[hitEvent.Hitter].health += (int) protect / 5;
                                        if (_robotBases[hitEvent.Hitter].health >
                                            RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default]
                                                .HealthLimit)
                                            _robotBases[hitEvent.Hitter].health =
                                                RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][
                                                        GunT.Default]
                                                    .HealthLimit;
                                    }

                                    if (_robotBases[hitEvent.Target].health <= 0)
                                    {
                                        _robotBases[hitEvent.Target].health = 0;
                                        KillRpc(_robotBases[hitEvent.Hitter].role, _robotBases[hitEvent.Target].role,
                                            "击杀");
                                        _robotBases[hitEvent.Hitter].experience +=
                                            RobotPerformanceTable.Table[_robotBases[hitEvent.Target].level][
                                                _robotBases[hitEvent.Target].role.Type][
                                                _robotBases[hitEvent.Target].chassisType][
                                                _robotBases[hitEvent.Target].gunType].ExpValue;
                                        if (_robotBases[hitEvent.Target].role.Type == TypeT.Engineer)
                                        {
                                            var engineer = _robotBases[hitEvent.Target];
                                            if (engineer.Buffs.All(b => b.type != BuffT.EngineerRevive))
                                                engineer.Buffs.Add(new EngineerReviveBuff());
                                        }
                                    }
                                }
                            }

                            if (_facilityBases.Keys.Contains(hitEvent.Target))
                            {
                                // 基地无敌
                                if (_facilityBases[hitEvent.Target].role.Type == TypeT.Base)
                                {
                                    if (_facilityBases.First(fb =>
                                        fb.Value.role.Type == TypeT.Outpost && fb.Value.role.Camp ==
                                        _facilityBases[hitEvent.Target].role.Camp).Value.health > 0)
                                        break;
                                    if (_robotBases.First(rb =>
                                            rb.Value.role.Type == TypeT.Guard && rb.Value.role.Camp ==
                                            _facilityBases[hitEvent.Target].role.Camp).Value.health > 0 &&
                                        !hitEvent.IsTriangle)
                                        break;
                                }

                                float protect;
                                if (hitEvent.Caliber != CaliberT.Dart)
                                {
                                    if (hitEvent.Caliber == CaliberT.Large)
                                        damage = _robotBases[hitEvent.Hitter].GetAttr().DamageRate *
                                                 (hitEvent.IsTriangle ? 300 : 200);
                                    else
                                        damage = 5 * _robotBases[hitEvent.Hitter].GetAttr().DamageRate;
                                    protect = damage * (1 - _facilityBases[hitEvent.Target].GetArmorRate());
                                }
                                else protect = new Random().Next(3) == 0 ? 0 : 1000;

                                if (_facilityBases[hitEvent.Target].health > 0)
                                {
                                    if (_facilityBases[hitEvent.Target].role.Type == TypeT.Outpost &&
                                        hitEvent.Caliber == CaliberT.Dart)
                                        _facilityBases[hitEvent.Target].health -= (int) protect / 2;
                                    else
                                        _facilityBases[hitEvent.Target].health -= (int) protect;
                                    if (hitEvent.Caliber != CaliberT.Dart && _robotBases.ContainsKey(hitEvent.Hitter))
                                        CampStatusMap[_robotBases[hitEvent.Hitter].role.Camp].damage += (int) protect;
                                    if (_facilityBases[hitEvent.Target].health <= 0)
                                    {
                                        _facilityBases[hitEvent.Target].health = 0;
                                        var killer = hitEvent.Caliber == CaliberT.Dart
                                            ? new RoleT(
                                                _facilityBases[hitEvent.Target].role.Camp == CampT.Red
                                                    ? CampT.Blue
                                                    : CampT.Red, TypeT.Ptz)
                                            : _robotBases[hitEvent.Hitter].role;
                                        KillRpc(killer, _facilityBases[hitEvent.Target].role, "击杀");
                                        if (_facilityBases[hitEvent.Target].role.Type == TypeT.Base)
                                            Emit(new TimeEvent(JudgeSystem.Event.TypeT.GameOver));
                                        if (_facilityBases[hitEvent.Target].role.Type == TypeT.Outpost)
                                        {
                                            if (hitEvent.Caliber != CaliberT.Dart)
                                                _robotBases[hitEvent.Hitter].experience += 5;
                                            // if (_robotBases.First(rb =>
                                            //     rb.Value.role.Type == TypeT.Guard && rb.Value.role.Camp ==
                                            //     _facilityBases[hitEvent.Target].role.Camp).Value.health <= 0)
                                            // {
                                            //     if (_facilityBases[hitEvent.Target].role.Camp == CampT.Red &&
                                            //         CampStatusMap[CampT.Red].virtualShield
                                            //         || _facilityBases[hitEvent.Target].role.Camp == CampT.Blue &&
                                            //         CampStatusMap[CampT.Blue].virtualShield)
                                            //     {
                                            //         _facilityBases.First(fb =>
                                            //             fb.Value.role.Type == TypeT.Base && fb.Value.role.Camp ==
                                            //             _facilityBases[hitEvent.Target].role.Camp).Value.health -= 500;
                                            //         switch (_facilityBases[hitEvent.Target].role.Camp)
                                            //         {
                                            //             case CampT.Unknown:
                                            //                 break;
                                            //             case CampT.Red:
                                            //                 CampStatusMap[CampT.Red].virtualShield = false;
                                            //                 break;
                                            //             case CampT.Blue:
                                            //                 CampStatusMap[CampT.Blue].virtualShield = false;
                                            //                 break;
                                            //             case CampT.Judge:
                                            //                 break;
                                            //             default:
                                            //                 throw new ArgumentOutOfRangeException();
                                            //         }
                                            //     }
                                            // }
                                        }
                                    }
                                    else
                                    {
                                        if (hitEvent.Caliber == CaliberT.Large && _robotBases[hitEvent.Hitter].Buffs
                                            .Any(b => b.type == BuffT.HeroSnipe))
                                        {
                                            switch (_facilityBases[hitEvent.Target].role.Type)
                                            {
                                                case TypeT.Base:
                                                    if (_facilityBases[hitEvent.Target].Buffs
                                                        .All(b => b.type != BuffT.OutpostBaseSnipe))
                                                        _facilityBases[hitEvent.Target].Buffs
                                                            .Add(new OutpostBaseSnipeBuff());
                                                    break;
                                                case TypeT.Outpost:
                                                    if (_facilityBases[hitEvent.Target].Buffs
                                                        .All(b => b.type != BuffT.OutpostBaseSnipe))
                                                        _facilityBases[hitEvent.Target].Buffs
                                                            .Add(new OutpostBaseSnipeBuff());
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        case JudgeSystem.Event.TypeT.GameStart:
                            Debug.Log("Starting game with " + confirmedCount + " players.");
                            globalStatus.startTime = (int) Time.time;
                            globalStatus.playing = true;
                            CampStatusMap[CampT.Red].money = 200;
                            CampStatusMap[CampT.Blue].money = 200;
                            CampStatusMap[CampT.Red].moneyObtainAmount = 200;
                            CampStatusMap[CampT.Blue].moneyObtainAmount = 200;
                            // 单机无限金钱
                            if (_roomManager.IsHost && _roomManager.roomSlots.Count == 1)
                            {
                                CampStatusMap[CampT.Red].money = 3000;
                                CampStatusMap[CampT.Blue].money = 3000;
                                CampStatusMap[CampT.Red].moneyObtainAmount = 0;
                                CampStatusMap[CampT.Blue].moneyObtainAmount = 0;
                            }

                            foreach (var player in _players)
                                player.OnAllReady();
                            GameStartRpc();

                            break;
                        case JudgeSystem.Event.TypeT.SixMinute:
                            CampStatusMap[CampT.Red].money += 100;
                            CampStatusMap[CampT.Blue].money += 100;
                            CampStatusMap[CampT.Red].moneyObtainAmount += 100;
                            CampStatusMap[CampT.Blue].moneyObtainAmount += 100;
                            globalStatus.smallBuffStart = true;
                            break;
                        case JudgeSystem.Event.TypeT.FiveMinute:
                            CampStatusMap[CampT.Red].money += 100;
                            CampStatusMap[CampT.Blue].money += 100;
                            CampStatusMap[CampT.Red].moneyObtainAmount += 100;
                            CampStatusMap[CampT.Blue].moneyObtainAmount += 100;
                            break;
                        case JudgeSystem.Event.TypeT.FourMinute:
                            CampStatusMap[CampT.Red].money += 100;
                            CampStatusMap[CampT.Blue].money += 100;
                            CampStatusMap[CampT.Red].moneyObtainAmount += 100;
                            CampStatusMap[CampT.Blue].moneyObtainAmount += 100;
                            foreach (var r in _robotBases)
                                r.Value.Buffs.RemoveAll(b => b.type == BuffT.SmallEnergy);
                            globalStatus.smallBuffStart = false;
                            break;
                        case JudgeSystem.Event.TypeT.ThreeMinute:
                            CampStatusMap[CampT.Red].money += 100;
                            CampStatusMap[CampT.Blue].money += 100;
                            CampStatusMap[CampT.Red].moneyObtainAmount += 100;
                            CampStatusMap[CampT.Blue].moneyObtainAmount += 100;
                            globalStatus.largeBuffStart = true;
                            break;
                        case JudgeSystem.Event.TypeT.TwoMinute:
                            break;
                        case JudgeSystem.Event.TypeT.OneMinute:
                            CampStatusMap[CampT.Red].money += 200;
                            CampStatusMap[CampT.Blue].money += 200;
                            CampStatusMap[CampT.Red].moneyObtainAmount += 200;
                            CampStatusMap[CampT.Blue].moneyObtainAmount += 200;
                            break;
                        case JudgeSystem.Event.TypeT.GameOver:
                            if (globalStatus.finished) break;
                            globalStatus.playing = false;
                            globalStatus.finished = true;
                            globalStatus.finishTime = (int) Time.time;
                            globalStatus.countDown = 16;
                            RpcOnClientGameOver(IsRedWin());
                            break;
                        case JudgeSystem.Event.TypeT.BuffActivate:
                            foreach (var f in _facilityBases.Where(f => f.Value.role.Type == TypeT.EnergyMechanism))
                            {
                                var b = (EnergyMechanismController) f.Value;
                                b.Disable();
                            }

                            var buffEvent = (BuffActivateEvent) e;

                            if (buffEvent.Large && globalStatus.largeBuffEnable)
                            {
                                foreach (var r in _robotBases.Where(r => r.Value.role.Camp == buffEvent.Camp))
                                    if (r.Value.Buffs.All(b => b.type != BuffT.LargeEnergy))
                                        r.Value.Buffs.Add(new LargeEnergyBuff());
                                globalStatus.largeBuffEnable = false;
                                globalStatus.largeBuffColdDown = Time.time + 75;
                            }

                            if (!buffEvent.Large && globalStatus.smallBuffEnable)
                            {
                                foreach (var r in _robotBases.Where(r => r.Value.role.Camp == buffEvent.Camp))
                                    if (r.Value.Buffs.All(b => b.type != BuffT.SmallEnergy))
                                        r.Value.Buffs.Add(new SmallEnergyBuff());
                                globalStatus.smallBuffEnable = false;
                                globalStatus.smallBuffColdDown = Time.time + 75;
                            }

                            EmActivateRpc(buffEvent.Camp);

                            break;
                        case JudgeSystem.Event.TypeT.AirRaid:
                            var aR = (AirRaidEvent) e;
                            switch (aR.Camp)
                            {
                                case CampT.Red:
                                    if (CampStatusMap[CampT.Red].money >= 400 &&
                                        CampStatusMap[CampT.Red].airRaidAmount < 3)
                                    {
                                        CampStatusMap[CampT.Red].money -= 400;
                                        var d = (DroneController) _robotBases.First(r =>
                                                r.Value.role.Equals(new RoleT(CampT.Red, TypeT.Drone)))
                                            .Value;
                                        d.raidTill = Time.time + 30;
                                        d.smallAmmo = 500;
                                        CampStatusMap[CampT.Red].airRaidAmount++;
                                    }

                                    break;
                                case CampT.Blue:
                                    if (CampStatusMap[CampT.Blue].money >= 400 &&
                                        CampStatusMap[CampT.Blue].airRaidAmount < 3)
                                    {
                                        CampStatusMap[CampT.Blue].money -= 400;
                                        var d = (DroneController) _robotBases.First(r =>
                                                r.Value.role.Equals(new RoleT(CampT.Blue, TypeT.Drone)))
                                            .Value;
                                        d.raidTill = Time.time + 30;
                                        d.smallAmmo = 500;
                                        CampStatusMap[CampT.Blue].airRaidAmount++;
                                    }

                                    break;
                            }

                            break;
                        case JudgeSystem.Event.TypeT.Dart:
                            var dR = (DartEvent) e;
                            var op = _facilityBases.First(f =>
                                f.Value.role.Equals(new RoleT(dR.Camp == CampT.Blue ? CampT.Red : CampT.Blue,
                                    TypeT.Outpost))).Value;
                            var ba = _facilityBases.First(f =>
                                f.Value.role.Equals(new RoleT(dR.Camp == CampT.Blue ? CampT.Red : CampT.Blue,
                                    TypeT.Base))).Value;
                            Emit(op.health > 0
                                ? new HitEvent(0, op.id, CaliberT.Dart)
                                : new HitEvent(0, ba.id, CaliberT.Dart));
                            break;
                        case JudgeSystem.Event.TypeT.Reset:
                            _roomManager.ResetServer();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (_started && globalStatus.countDown > 420 && globalStatus.countDown <= 425)
                    foreach (var rb in FindObjectsOfType<GroundControllerBase>())
                        if (rb.Buffs.All(b => b.type != BuffT.Base) && rb.health > 0)
                        {
                            rb.health = 0;
                            KillRpc(rb.role, rb.role, "抢跑死亡");
                        }

                // 神符
                if (globalStatus.smallBuffStart)
                {
                    if (!globalStatus.smallBuffEnable && Time.time > globalStatus.smallBuffColdDown)
                    {
                        foreach (var f in _facilityBases.Where(f => f.Value.role.Type == TypeT.EnergyMechanism))
                        {
                            var b = (EnergyMechanismController) f.Value;
                            b.Enable(false);
                        }

                        globalStatus.smallBuffEnable = true;
                    }
                }
                else if (globalStatus.largeBuffStart)
                {
                    if (!globalStatus.largeBuffEnable && Time.time > globalStatus.largeBuffColdDown)
                    {
                        foreach (var f in _facilityBases.Where(f => f.Value.role.Type == TypeT.EnergyMechanism))
                        {
                            var b = (EnergyMechanismController) f.Value;
                            b.Enable(true);
                        }

                        globalStatus.largeBuffEnable = true;
                    }
                }
                else
                {
                    foreach (var f in _facilityBases.Where(f => f.Value.role.Type == TypeT.EnergyMechanism))
                    {
                        var b = (EnergyMechanismController) f.Value;
                        b.Disable();
                    }
                }
            }

            [Command(requiresAuthority = false)]
            private void CmdConfirmType(int id, ChassisT c, GunT g)
            {
                var robot = _robotBases[id];
                if (robot.health ==
                    RobotPerformanceTable.Table[robot.level][robot.role.Type][robot.chassisType][robot.gunType]
                        .HealthLimit)
                {
                    robot.chassisType = c;
                    robot.gunType = g;
                    robot.health =
                        RobotPerformanceTable.Table[robot.level][robot.role.Type][robot.chassisType][robot.gunType]
                            .HealthLimit;
                }
                else
                {
                    robot.chassisType = c;
                    robot.gunType = g;
                }
            }

            [Command(requiresAuthority = false)]
            public void CmdReset()
            {
                Emit(new TimeEvent(JudgeSystem.Event.TypeT.Reset));
            }

            [Command(requiresAuthority = false)]
            public void CmdKill(RoleT killer, RoleT victim, string method) => KillRpc(killer, victim, method);

            #endregion

            #region Client

            [ClientRpc]
            private void PunishRpc(CampT camp, int time)
            {
                if (_localRobot && _localRobot.role.Camp == camp)
                {
                    FindObjectOfType<PunishUI>().Punish(time);
                }
            }

            [ClientRpc]
            private void EmActivateRpc(CampT camp)
            {
                if (_localRobot && _localRobot.role.Camp == camp)
                    FindObjectOfType<EmUI>().Activate();
                else if (judge)
                    FindObjectOfType<EmUI>().Activate();
                else if (observing && observing.role.Camp == camp)
                    FindObjectOfType<EmUI>().Activate();
            }

            [ClientRpc]
            private void KillRpc(RoleT killer, RoleT victim, string method)
            {
                var dh = FindObjectOfType<KillHintUI>();
                if (dh)
                    dh.Hint(killer, victim, method);
            }

            [Client]
            public void LocalRobotRegister(RobotBase robot)
            {
                _localRobot = robot;
            }

            [Client]
            public void LocalJudgeRegister()
            {
                judge = FindObjectOfType<JudgeController>();
            }

            private IEnumerator HideLoading()
            {
                yield return new WaitForSeconds(0.5f);
                loadingHint.SetActive(false);
            }

            [Client]
            public float GetSensitivity()
            {
                return sensitivitySlide.value * 2;
            }

            [Client]
            public void Hurt() => FindObjectOfType<HurtUI>().Hurt();

            [Client]
            public void Exchange(CampT camp, int value)
            {
                CmdExchange(camp, value);
            }

            [Client]
            public void Supply(RoleT role, int origin)
            {
                CmdSupply(role, origin);
            }

            [Client]
            public void TypeConfirmClicked()
            {
                if (!_localRobot) return;
                if (chassisTypeSelect.value == 0 || gunTypeSelect.value == 0) return;
                if (_localRobot.role.Type == TypeT.Hero && gunTypeSelect.value == 2) return;
                typeConfirm.interactable = false;
                chassisTypeSelect.interactable = false;
                gunTypeSelect.interactable = false;
                CmdConfirmType(_localRobot.id, (ChassisT) chassisTypeSelect.value, (GunT) gunTypeSelect.value);
            }


            [Client]
            private void ClientStart()
            {
                // new Thread(StartDecisionSystem).Start();
            }

            private IEnumerator PlayStartGameMusic()
            {
                yield return new WaitUntil(() => globalStatus.countDown == 425);
                GameObject.Find("cdSound").GetComponent<AudioSource>().Play();
                yield return new WaitForSeconds(7.5f);
                GameObject.Find("inGameMusic").GetComponent<AudioSource>().Play();
            }

            [ClientRpc]
            private void GameStartRpc()
            {
                clientRobotBases = new List<RobotBase>(FindObjectsOfType<RobotBase>());
                clientFacilityBases = new List<FacilityBase>(FindObjectsOfType<FacilityBase>());

                resultTitle.text = "";
                resultPanel.SetActive(false);
                typeConfirm.interactable = false;
                chassisTypeSelect.interactable = false;
                gunTypeSelect.interactable = false;

                StartCoroutine(PlayStartGameMusic());

                if (judge)
                {
                    blurLayer.SetActive(false);
                }

                var mesh = GameObject.Find("Arena21").GetComponent<MeshFilter>().sharedMesh;
                var vertices = mesh.vertices;
                var uvs = new Vector2[vertices.Length];
                var normals = mesh.normals;
                for (var i = 0; i < normals.Length; i++)
                {
                    if (Mathf.Abs(normals[i].x) > Mathf.Abs(normals[i].y) &&
                        Mathf.Abs(normals[i].x) > Mathf.Abs(normals[i].z))
                    {
                        uvs[i] = new Vector2(vertices[i].y, vertices[i].z);
                    }

                    if (Mathf.Abs(normals[i].y) > Mathf.Abs(normals[i].x) &&
                        Mathf.Abs(normals[i].y) > Mathf.Abs(normals[i].z))
                    {
                        uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
                    }

                    if (Mathf.Abs(normals[i].z) > Mathf.Abs(normals[i].x) &&
                        Mathf.Abs(normals[i].z) > Mathf.Abs(normals[i].y))
                    {
                        uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
                    }
                }

                mesh.uv = uvs;
                if (_localRobot || judge)
                {
                    if (_localRobot && (_localRobot.role.Type == TypeT.Hero || _localRobot.role.IsInfantry()))
                    {
                        typeConfirm.interactable = true;
                        chassisTypeSelect.interactable = true;
                        gunTypeSelect.interactable = true;
                    }

                    StartCoroutine(HideLoading());
                }
            }

            [ClientRpc]
            private void RpcOnClientGameOver(int redWin)
            {
                foreach (var robot in clientRobotBases)
                    robot.isLocalRobot = false;

                if (_localRobot != null)
                {
                    if (_localRobot.role.Camp == CampT.Red && redWin == 1
                        || _localRobot.role.Camp == CampT.Blue && redWin == -1)
                        resultTitle.text = "胜利";
                    else
                        resultTitle.text = redWin == 0 ? "平局" : "失败";
                }

                GameObject.Find("inGameMusic").GetComponent<AudioSource>().Stop();
                GameObject.Find("endMusic").GetComponent<AudioSource>().Play();
                resultPanel.SetActive(true);
            }

            [Client]
            private void ClientFixedUpdate()
            {
                // 场景已成功加载
                if (clientFacilityBases.Count > 0)
                {
                    optionsPanel.SetActive(Cursor.lockState != CursorLockMode.Locked);
                    hudManager.Refresh(
                        _localRobot && !judge ? _localRobot : judge ? observing ? observing : null : null);
                    foreach (var map in FindObjectsOfType<MapUI>())
                    {
                        if (map.CompareTag("InfoDisplay")) map.Refresh(null);
                        else map.Refresh(_localRobot && !judge ? _localRobot : null);
                    }

                    // 转动飞手摄像机
                    if (_localRobot)
                    {
                        if (_localRobot.role.Type == TypeT.Drone)
                        {
                            if (!((DroneController) _localRobot).isPtz)
                            {
                                if (_localRobot.role.Camp == CampT.Red)
                                {
                                    redDCam.transform.LookAt(_localRobot.transform);
                                }
                                else
                                {
                                    blueDCam.transform.LookAt(_localRobot.transform);
                                }
                            }
                        }
                    }
                    else
                    {
                        var player = FindObjectsOfType<GamePlayer>().First(p => p.isLocalPlayer);
                        if (player.role.Type == TypeT.Ptz)
                        {
                            loadingHint.SetActive(true);
                        }
                    }
                }
            }

            private void ClientUpdate()
            {
                // 解锁鼠标
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            [Client]
            public void Disconnect()
            {
                if (_roomManager && _roomManager.IsHost)
                    _roomManager.ResetServer();
                FindObjectOfType<RoomManager>().StopClient();
                SceneManager.LoadScene("Index");
            }

            #endregion

            private void Start()
            {
#if UNITY_EDITOR
                GameObject.Find("Directional Light").SetActive(false);
#endif
                if (isServer) ServerStart();
                if (isClient) ClientStart();
            }

            private void FixedUpdate()
            {
                if (isServer) ServerFixedUpdate();
                if (isClient) ClientFixedUpdate();
            }

            private void Update()
            {
                if (isClient) ClientUpdate();
            }

            private void OnDestroy()
            {
                foreach (var p in Process.GetProcessesByName("SD"))
                {
                    p.Kill();
                }
            }
        }
    }
}