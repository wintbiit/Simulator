using System;
using System.Collections;
using System.Collections.Generic;
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
        public class HealthDisplay
        {
            public TypeT type;
            public RectTransform bar;
            public float width;
        }

        [Serializable]
        public class CampStatus
        {
            public int money;
            public bool em;
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

            public List<Transform> silverStart = new List<Transform>();
            public List<Transform> goldStart = new List<Transform>();

            public List<Transform> blockStart = new List<Transform>();

            public List<HealthDisplay> redHealthDisplays = new List<HealthDisplay>();
            public List<HealthDisplay> blueHealthDisplays = new List<HealthDisplay>();

            public TMP_Text countDownDisplay;
            public TMP_Text smallAmmoDisplay;
            public TMP_Text largeAmmoDisplay;
            public TMP_Text expDisplay;
            public TMP_Text mineDisplay;
            public TMP_Text moneyDisplay;
            public TMP_Text extraDisplay;
            public GameObject infantrySupplyHint;
            public GameObject heroSupplyHint;

            public GameObject resultPanel;
            public TMP_Text resultTitle;

            public RawImage overHeat;
            public Image heatProcess;

            public GameObject optionsPanel;
            public Slider sensitivitySlide;

            public GameObject deadHint;

            public RawImage hurtHint;

            public GameObject loadingHint;

            public Image operationProcess;

            private int _redInfantrySupplyAmount;
            private int _blueInfantrySupplyAmount;
            private int _redHeroSupplyAmount;
            private int _blueHeroSupplyAmount;
            private int _redAirRaidAmount;
            private int _blueAirRaidAmount;

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

            [SyncVar] private int _redMoney;
            [SyncVar] private int _blueMoney;

            [SyncVar] private bool _redVirtualShield = true;
            [SyncVar] private bool _blueVirtualShield = true;

            [SyncVar] private bool _smallBuffStart;
            [SyncVar] private bool _smallBuffEnable;
            [SyncVar] private bool _largeBuffStart;
            [SyncVar] private bool _largeBuffEnable;
            [SyncVar] private float _smallBuffColdDown;
            [SyncVar] private float _largeBuffColdDown;

            #region Server

            [Server]
            public List<GamePlayer> GetPlayers()
            {
                return _players;
            }

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
                             && r.Type != TypeT.Ptz && r.Type <= TypeT.Drone) == 0)
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
                                new RoleT(CampT.Red, TypeT.Base)))
                    .Value;
                var blueBase = _facilityBases
                    .First(
                        f =>
                            f.Value.role.Equals(
                                new RoleT(CampT.Blue, TypeT.Base)))
                    .Value;
                if (redBase.health != blueBase.health)
                    return redBase.health > blueBase.health;
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
                    return redOutpost.health > blueOutpost.health;
                // TODO: 伤害统计
                return true;
            }

            [Command(ignoreAuthority = true)]
            private void CmdExchange(CampT camp, int value)
            {
                switch (camp)
                {
                    case CampT.Unknown:
                        break;
                    case CampT.Red:
                        _redMoney += value;
                        break;
                    case CampT.Blue:
                        _blueMoney += value;
                        break;
                    case CampT.Judge:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(camp), camp, null);
                }
            }

            [Command(ignoreAuthority = true)]
            private void CmdSupply(RoleT role)
            {
                if (role.IsInfantry())
                {
                    var i = _robotBases.First(r => r.Value.role.Equals(role)).Value;
                    if (role.Camp == CampT.Red && _redMoney >= 50 || role.Camp == CampT.Blue && _blueMoney >= 50)
                    {
                        if (role.Camp == CampT.Red && _redInfantrySupplyAmount < 1500)
                        {
                            _redMoney -= 50;
                            _redInfantrySupplyAmount += 50;
                            i.smallAmmo += 50;
                        }
                        else if (role.Camp == CampT.Blue && _blueInfantrySupplyAmount < 1500)
                        {
                            _blueMoney -= 50;
                            _blueInfantrySupplyAmount += 50;
                            i.smallAmmo += 50;
                        }
                    }
                }

                if (role.Type == TypeT.Hero)
                {
                    var h = _robotBases.First(r => r.Value.role.Equals(role)).Value;
                    if (role.Camp == CampT.Red && _redMoney >= 75 || role.Camp == CampT.Blue && _blueMoney >= 75)
                    {
                        if (role.Camp == CampT.Red && _redHeroSupplyAmount < 100)
                        {
                            _redMoney -= 75;
                            _redHeroSupplyAmount += 5;
                            h.largeAmmo += 5;
                        }
                        else if (role.Camp == CampT.Blue && _blueHeroSupplyAmount < 100)
                        {
                            _blueMoney -= 75;
                            _blueHeroSupplyAmount += 5;
                            h.largeAmmo += 5;
                        }
                    }
                }
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

                if (!isServerOnly) _clientRobotBases = new List<RobotBase>(FindObjectsOfType<RobotBase>());

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
                                var protect = damage * (1 - _robotBases[hitEvent.Target].GetAttr().ArmorRate);
                                _robotBases[hitEvent.Target].health -= (int) protect;
                                if (_robotBases[hitEvent.Target].health <= 0)
                                {
                                    _robotBases[hitEvent.Target].health = 0;
                                    _robotBases[hitEvent.Hitter].experience +=
                                        RobotPerformanceTable.Table[_robotBases[hitEvent.Target].level][
                                            _robotBases[hitEvent.Target].role.Type].ExpValue;
                                    if (_robotBases[hitEvent.Target].role.Type == TypeT.Engineer)
                                    {
                                        var engineer = _robotBases[hitEvent.Target];
                                        if (engineer.Buffs.All(b => b.type != BuffT.EngineerRevive))
                                            engineer.Buffs.Add(new EngineerReviveBuff());
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
                                    {
                                        break;
                                    }
                                }

                                float protect;
                                if (hitEvent.Caliber != CaliberT.Dart)
                                {
                                    if (hitEvent.Caliber == CaliberT.Large)
                                        damage += _robotBases[hitEvent.Hitter].GetAttr().DamageRate * 100;
                                    protect = damage * (1 - _facilityBases[hitEvent.Target].GetArmorRate());
                                }
                                else protect = new Random().Next(3) == 0 ? 0 : 1000;

                                _facilityBases[hitEvent.Target].health -= (int) protect;
                                if (_facilityBases[hitEvent.Target].health <= 0)
                                {
                                    if (_facilityBases[hitEvent.Target].role.Type == TypeT.Base)
                                        Emit(new TimeEvent(JudgeSystem.Event.TypeT.GameOver));
                                    if (_facilityBases[hitEvent.Target].role.Type == TypeT.Outpost)
                                    {
                                        if (_robotBases.First(rb =>
                                            rb.Value.role.Type == TypeT.Guard && rb.Value.role.Camp ==
                                            _facilityBases[hitEvent.Target].role.Camp).Value.health <= 0)
                                        {
                                            if (_facilityBases[hitEvent.Target].role.Camp == CampT.Red &&
                                                _redVirtualShield
                                                || _facilityBases[hitEvent.Target].role.Camp == CampT.Blue &&
                                                _blueVirtualShield)
                                            {
                                                _facilityBases.First(fb =>
                                                    fb.Value.role.Type == TypeT.Base && fb.Value.role.Camp ==
                                                    _facilityBases[hitEvent.Target].role.Camp).Value.health -= 500;
                                                switch (_facilityBases[hitEvent.Target].role.Camp)
                                                {
                                                    case CampT.Unknown:
                                                        break;
                                                    case CampT.Red:
                                                        _redVirtualShield = false;
                                                        break;
                                                    case CampT.Blue:
                                                        _blueVirtualShield = false;
                                                        break;
                                                    case CampT.Judge:
                                                        break;
                                                    default:
                                                        throw new ArgumentOutOfRangeException();
                                                }
                                            }
                                        }
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

                            break;
                        case JudgeSystem.Event.TypeT.GameStart:
                            _startTime = (int) Time.time;
                            _playing = true;
                            _redMoney = 200;
                            _blueMoney = 200;
                            // 单机无限金钱
                            if (_roomManager.IsHost && _roomManager.roomSlots.Count == 1)
                            {
                                _redMoney = 10000;
                                _blueMoney = 10000;
                            }

                            foreach (var player in _players)
                                player.OnAllReady();
                            GameStartRpc();
                            break;
                        case JudgeSystem.Event.TypeT.SixMinute:
                            _redMoney += 100;
                            _blueMoney += 100;
                            _smallBuffStart = true;
                            break;
                        case JudgeSystem.Event.TypeT.FiveMinute:
                            _redMoney += 100;
                            _blueMoney += 100;
                            break;
                        case JudgeSystem.Event.TypeT.FourMinute:
                            _redMoney += 100;
                            _blueMoney += 100;
                            foreach (var r in _robotBases)
                                r.Value.Buffs.RemoveAll(b => b.type == BuffT.SmallEnergy);
                            _smallBuffStart = false;
                            break;
                        case JudgeSystem.Event.TypeT.ThreeMinute:
                            _redMoney += 100;
                            _blueMoney += 100;
                            _largeBuffStart = true;
                            break;
                        case JudgeSystem.Event.TypeT.TwoMinute:
                            break;
                        case JudgeSystem.Event.TypeT.OneMinute:
                            _redMoney += 200;
                            _blueMoney += 200;
                            break;
                        case JudgeSystem.Event.TypeT.GameOver:
                            if (_finished) break;
                            _playing = false;
                            _finished = true;
                            _finishTime = (int) Time.time;
                            RpcOnClientGameOver(IsRedWin());
                            break;
                        case JudgeSystem.Event.TypeT.BuffActivate:
                            foreach (var f in _facilityBases.Where(f => f.Value.role.Type == TypeT.EnergyMechanism))
                            {
                                var b = (EnergyMechanismController) f.Value;
                                b.Disable();
                            }

                            var buffEvent = (BuffActivateEvent) e;

                            if (buffEvent.Large && _largeBuffEnable)
                            {
                                foreach (var r in _robotBases.Where(r => r.Value.role.Camp == buffEvent.Camp))
                                    if (r.Value.Buffs.All(b => b.type != BuffT.LargeEnergy))
                                        r.Value.Buffs.Add(new LargeEnergyBuff());
                                _largeBuffEnable = false;
                                _largeBuffColdDown = Time.time + 75;
                            }

                            if (!buffEvent.Large && _smallBuffEnable)
                            {
                                foreach (var r in _robotBases.Where(r => r.Value.role.Camp == buffEvent.Camp))
                                    if (r.Value.Buffs.All(b => b.type != BuffT.SmallEnergy))
                                        r.Value.Buffs.Add(new SmallEnergyBuff());
                                _smallBuffEnable = false;
                                _smallBuffColdDown = Time.time + 75;
                            }

                            break;
                        case JudgeSystem.Event.TypeT.AirRaid:
                            var aR = (AirRaidEvent) e;
                            switch (aR.Camp)
                            {
                                case CampT.Red:
                                    if (_redMoney >= 400 && _redAirRaidAmount < 3)
                                    {
                                        _redMoney -= 400;
                                        var d = (DroneController) _robotBases.First(r =>
                                                r.Value.role.Equals(new RoleT(CampT.Red, TypeT.Drone)))
                                            .Value;
                                        d.raidTill = Time.time + 30;
                                        d.smallAmmo = 500;
                                        _redAirRaidAmount++;
                                    }

                                    break;
                                case CampT.Blue:
                                    if (_blueMoney >= 400 && _blueAirRaidAmount < 3)
                                    {
                                        _blueMoney -= 400;
                                        var d = (DroneController) _robotBases.First(r =>
                                                r.Value.role.Equals(new RoleT(CampT.Blue, TypeT.Drone)))
                                            .Value;
                                        d.raidTill = Time.time + 30;
                                        d.smallAmmo = 500;
                                        _blueAirRaidAmount++;
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

                // 神符
                if (_smallBuffStart)
                {
                    if (!_smallBuffEnable && Time.time > _smallBuffColdDown)
                    {
                        foreach (var f in _facilityBases.Where(f => f.Value.role.Type == TypeT.EnergyMechanism))
                        {
                            var b = (EnergyMechanismController) f.Value;
                            b.Enable(false);
                        }

                        _smallBuffEnable = true;
                    }
                }
                else if (_largeBuffStart)
                {
                    if (!_largeBuffEnable && Time.time > _largeBuffColdDown)
                    {
                        foreach (var f in _facilityBases.Where(f => f.Value.role.Type == TypeT.EnergyMechanism))
                        {
                            var b = (EnergyMechanismController) f.Value;
                            b.Enable(true);
                        }

                        _largeBuffEnable = true;
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

            [Command(ignoreAuthority = true)]
            private void CmdReset()
            {
                Emit(new TimeEvent(JudgeSystem.Event.TypeT.Reset));
            }

            #endregion

            #region Client

            [Client]
            public void LocalRobotRegister(RobotBase robot)
            {
                _localRobot = robot;
                StartCoroutine(HideLoading());
            }

            private IEnumerator HideLoading()
            {
                yield return new WaitForSeconds(0.5f);
                loadingHint.SetActive(false);
            }

            private List<RobotBase> _clientRobotBases = new List<RobotBase>();
            private List<FacilityBase> _clientFacilityBases = new List<FacilityBase>();

            [Client]
            public float GetSensitivity()
            {
                return sensitivitySlide.value * 2;
            }

            [Client]
            public void Hurt() => hurtHint.color = new Color(1, 0, 0, 1);

            [Client]
            public void Exchange(CampT camp, int value)
            {
                CmdExchange(camp, value);
            }

            [Client]
            public void Supply(RoleT role)
            {
                CmdSupply(role);
            }

            [Client]
            private void ClientStart()
            {
                mineDisplay.text = "";
                resultTitle.text = "";
                resultPanel.SetActive(false);
                infantrySupplyHint.SetActive(false);
                heroSupplyHint.SetActive(false);
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

                var mesh = GameObject.Find("Arena21").GetComponent<MeshFilter>().sharedMesh;
                var vertices = mesh.vertices;
                var uvs = new Vector2[vertices.Length];
                var normals = mesh.normals;

                //Cubic Unfold
                for (var i = 0; i < normals.Length; i++)
                {
                    //X-Plane
                    if (Mathf.Abs(normals[i].x) > Mathf.Abs(normals[i].y) &&
                        Mathf.Abs(normals[i].x) > Mathf.Abs(normals[i].z))
                    {
                        uvs[i] = new Vector2(vertices[i].y, vertices[i].z);
                    }

                    //Y-Plane
                    if (Mathf.Abs(normals[i].y) > Mathf.Abs(normals[i].x) &&
                        Mathf.Abs(normals[i].y) > Mathf.Abs(normals[i].z))
                    {
                        uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
                    }

                    //Z-Plane
                    if (Mathf.Abs(normals[i].z) > Mathf.Abs(normals[i].x) &&
                        Mathf.Abs(normals[i].z) > Mathf.Abs(normals[i].y))
                    {
                        uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
                    }
                }

                mesh.uv = uvs;
            }

            [ClientRpc]
            private void GameStartRpc()
            {
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
                if (_clientFacilityBases.Count > 0)
                {
                    // 信息显示更新
                    if (_localRobot)
                    {
                        optionsPanel.SetActive(Cursor.lockState != CursorLockMode.Locked);
                        deadHint.SetActive(_localRobot.health == 0);
                        if (_localRobot.role.Type != TypeT.Engineer)
                        {
                            var heatLimit = RobotPerformanceTable.Table[_localRobot.level][_localRobot.role.Type]
                                .HeatLimit;
                            var processColor = _localRobot.heat < heatLimit
                                ? (heatLimit - _localRobot.heat) / heatLimit
                                : 0;
                            heatProcess.color = new Color(1, processColor, processColor);
                            heatProcess.fillAmount = _localRobot.heat < heatLimit ? _localRobot.heat / heatLimit : 1;
                            overHeat.color = new Color(1, 1, 1,
                                _localRobot.heat > heatLimit ? (_localRobot.heat - heatLimit) / (heatLimit / 4.0f) : 0);
                        }
                        else
                        {
                            heatProcess.fillAmount = 0;
                            overHeat.color = new Color(1, 1, 1, 0);
                        }

                        hurtHint.color = hurtHint.color.a >= 0.04f
                            ? new Color(1, 0, 0, hurtHint.color.a - 0.04f)
                            : new Color(1, 0, 0, 0);
                    }

                    foreach (var r in _clientRobotBases)
                    {
                        if (r.role.Type == TypeT.Drone) continue;
                        var healthDisplay = r.role.Camp == CampT.Red
                            ? redHealthDisplays.First(hd => hd.type == r.role.Type)
                            : blueHealthDisplays.First(hd => hd.type == r.role.Type);
                        var healthRate = (float) r.health /
                                         RobotPerformanceTable.Table[r.level][r.role.Type].HealthLimit;
                        healthDisplay.bar.sizeDelta = new Vector2(
                            healthDisplay.width * (healthRate - 1), 0);
                        if (r.role.Camp == CampT.Red)
                            healthDisplay.bar.offsetMax = Vector2.zero;
                        else
                            healthDisplay.bar.offsetMin = Vector2.zero;
                    }

                    foreach (var f in _clientFacilityBases)
                    {
                        if (f.role.Type == TypeT.EnergyMechanism) continue;
                        var healthDisplay = f.role.Camp == CampT.Red
                            ? redHealthDisplays.First(hd => hd.type == f.role.Type)
                            : blueHealthDisplays.First(hd => hd.type == f.role.Type);
                        var healthRate = (float) f.health / f.healthLimit;
                        healthDisplay.bar.sizeDelta = new Vector2(
                            healthDisplay.width * (healthRate - 1), 0);
                        if (f.role.Camp == CampT.Red)
                            healthDisplay.bar.offsetMax = Vector2.zero;
                        else
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

                    extraDisplay.text = "";
                    extraDisplay.text += "蓝方基地无敌：" +
                                         (_clientFacilityBases.First(fb =>
                                             fb.role.Equals(new RoleT(CampT.Blue, TypeT.Outpost))).health > 0
                                             ? "是"
                                             : "否") + '\n';
                    extraDisplay.text += "红方基地无敌：" +
                                         (_clientFacilityBases.First(fb =>
                                             fb.role.Equals(new RoleT(CampT.Red, TypeT.Outpost))).health > 0
                                             ? "是"
                                             : "否") + '\n';
                    extraDisplay.text += "蓝方虚拟护盾：" + (_blueVirtualShield ? "是" : "否") + '\n';
                    extraDisplay.text += "红方虚拟护盾：" + (_redVirtualShield ? "是" : "否") + '\n';

                    if (_localRobot == null) return;
                    smallAmmoDisplay.text = "17mm: " + _localRobot.smallAmmo;
                    largeAmmoDisplay.text = "42mm: " + _localRobot.largeAmmo;
                    expDisplay.text = "经验值：" + _localRobot.experience;
                    moneyDisplay.text =
                        "团队金钱：" + (_localRobot.role.Camp == CampT.Red ? _redMoney : _blueMoney);
                    operationProcess.fillAmount = 0;
                    if (_localRobot.role.Type == TypeT.Engineer)
                    {
                        var engineer = _localRobot.GetComponent<EngineerController>();
                        mineDisplay.text = "矿物价值：" + engineer.MineValue();
                        if (engineer.Buffs.Any(b => b.type == BuffT.EngineerRevive))
                        {
                            var timeLeft = 20 - (Time.time -
                                                 ((EngineerReviveBuff) engineer.Buffs.First(b =>
                                                     b.type == BuffT.EngineerRevive))
                                                 .StartTime);
                            extraDisplay.text += Mathf.RoundToInt(timeLeft) + "秒后自动复活\n";
                        }

                        operationProcess.fillAmount = engineer.opProcess;
                    }

                    if (_localRobot.role.IsInfantry())
                    {
                        var infantry = _localRobot.GetComponent<InfantryController>();
                        infantrySupplyHint.SetActive(infantry.atSupply);
                    }

                    if (_localRobot.role.Type == TypeT.Hero)
                    {
                        var hero = _localRobot.GetComponent<HeroController>();
                        heroSupplyHint.SetActive(hero.atSupply);
                    }

                    if (_localRobot.role.Type == TypeT.Drone)
                    {
                        var drone = _localRobot.GetComponent<DroneController>();
                        if (drone.raidStart > 0)
                            extraDisplay.text +=
                                "空中支援剩余" + Mathf.RoundToInt(30 - (Time.time - drone.raidStart)) + "秒\n";
                        else if (drone.role.Camp == CampT.Red && _redMoney >= 400 ||
                                 drone.role.Camp == CampT.Blue && _blueMoney >= 400)
                            extraDisplay.text += "按H兑换空中支援\n";
                        extraDisplay.text += "导弹剩余" + (4 - drone.dartCount) + "次\n";
                        if (drone.dartCount < 4)
                        {
                            if (drone.dartTill > Time.time)
                                extraDisplay.text += Mathf.RoundToInt(drone.dartTill - Time.time) + "秒后导弹就绪\n";
                            else
                                extraDisplay.text += "按Y发射导弹\n";
                        }
                    }

                    var a = _localRobot.GetAttr();
                    if (Math.Abs(a.DamageRate - 1) > 1e-2) extraDisplay.text += "攻击加成" + a.DamageRate * 100 + "%\n";
                    if (Math.Abs(a.ArmorRate - 0) > 1e-2) extraDisplay.text += "防御加成" + a.ArmorRate * 100 + "%\n";
                    if (Math.Abs(a.ColdDownRate - 1) > 1e-2) extraDisplay.text += "冷却速度" + a.ColdDownRate * 100 + "%\n";
                    if (Math.Abs(a.ReviveRate - 0) > 1e-2) extraDisplay.text += "生命回复" + a.ReviveRate * 100 + "%\n";

                    if (_localRobot.Buffs.Any(b => b.type == BuffT.SmallEnergy)) extraDisplay.text += "小神符" + '\n';
                    if (_localRobot.Buffs.Any(b => b.type == BuffT.LargeEnergy)) extraDisplay.text += "大神符" + '\n';
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
        }
    }
}