using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.JudgeSystem;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    namespace Engineer
    {
        public class EngineerReviveBuff : BuffBase
        {
            public EngineerReviveBuff()
            {
                startTime = Time.time;
                type = BuffT.EngineerRevive;
                timeOut = float.MaxValue;
            }
        }

        public class EngineerControllerRecord : GroundControllerBaseRecord
        {
            public int ReviveTime;
        }

        public class EngineerController : GroundControllerBase
        {
            private readonly List<int> _mine = new List<int>();
            private float _lastCollect;
            private float _lastExchange;
            private bool _drag;
            private GameObject _dragObject;
            private bool _grab;
            private GameObject _grabObject;

            private float _lastPress = float.MaxValue;
            private bool _pressed;
            public float opProcess;

            [SyncVar] public int reviveTime;

            [Server]
            public EngineerControllerRecord RecordFrame()
            {
                var record = new EngineerControllerRecord();
                base.RecordFrame(record);
                record.ReviveTime = reviveTime;
                return record;
            }

            protected override void UnFireOperation()
            {
                _pressed = false;
                _lastPress = float.MaxValue;
            }

            protected override bool FireOperation()
            {
                if (GetComponent<Rigidbody>().velocity.magnitude < 0.5f)
                {
                    if (!_pressed)
                    {
                        _lastPress = Time.time;
                        _pressed = true;
                    }
                    else if (Time.time - _lastPress > 5)
                    {
                        _lastPress = float.MaxValue;
                        var ray = new Ray(fpCam.transform.position, fpCam.transform.forward);
                        MineController mc = null;
                        GroundControllerBase gc = null;
                        BlockController bc = null;
                        if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
                        {
                            mc = hit.collider.GetComponent<MineController>();
                            gc = hit.collider.GetComponent<GroundControllerBase>();
                            bc = hit.collider.GetComponent<BlockController>();
                        }

                        // 采矿
                        if (mc != null && !_grab && !_drag)
                        {
                            if ((hit.point - fpCam.transform.position).magnitude > 0.7f) return true;
                            if (Time.time - _lastCollect > 2.0f && _mine.Count < 3)
                            {
                                mc.Collect();
                                switch (mc.type)
                                {
                                    case MineType.Silver:
                                        _mine.Add(75);
                                        break;
                                    case MineType.Gold:
                                        _mine.Add(300);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                _lastCollect = Time.time;
                            }
                        } // 兑换
                        else if (
                            role.Camp == CampT.Red && hit.transform.name == "RE"
                            || role.Camp == CampT.Blue && hit.transform.name == "BE" && !_grab && !_drag)
                        {
                            if ((hit.point - fpCam.transform.position).magnitude > 0.7f) return true;
                            if (Time.time - _lastExchange > 3.0f)
                            {
                                if (_mine.Count > 0)
                                {
                                    FindObjectOfType<GameManager>().Exchange(role.Camp, _mine[0]);
                                    _mine.RemoveAt(0);
                                    _lastExchange = Time.time;
                                }
                            }
                        } // 拖拽
                        else if (gc != null && !_drag)
                        {
                            if ((hit.point - fpCam.transform.position).magnitude > 3.0f) return true;
                            if (gc.role.Camp == role.Camp)
                                // && gc.health == 0)
                            {
                                if (!_drag && !_grab)
                                {
                                    _dragObject = gc.gameObject;
                                    _drag = true;
                                    _dragObject.GetComponent<Rigidbody>().isKinematic = true;
                                }
                            }
                        }
                        else if (_drag)
                        {
                            _drag = false;
                            _dragObject.GetComponent<GroundControllerBase>().EndDrag();
                        }
                        else if (bc != null && !_grab)
                        {
                            if ((hit.point - fpCam.transform.position).magnitude > 3.0f) return true;
                            if (!_grab && !_drag)
                            {
                                _grabObject = bc.gameObject;
                                _grab = true;
                                _grabObject.GetComponent<Rigidbody>().isKinematic = true;
                            }
                        }
                        else if (_grab)
                        {
                            _grab = false;
                            _grabObject.GetComponent<Rigidbody>().isKinematic = false;
                        }
                    }
                }
                else
                {
                    _lastPress = float.MaxValue;
                }

                return true;
            }

            public int MineValue() => _mine.Sum(m => m);

            protected override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isLocalRobot)
                {
                    opProcess = Time.time - _lastPress < 5 ? (Time.time - _lastPress) / 5 : 0;
                    if (health > 0)
                    {
                        if (_drag)
                        {
                            var t = transform;
                            _dragObject
                                .GetComponent<GroundControllerBase>()
                                .Drag(t.position + t.forward + t.up * 0.2f);
                        }

                        if (_grab)
                        {
                            var t = transform;
                            _grabObject
                                .GetComponent<BlockController>()
                                .Drag(t.position + t.forward + t.up * -0.2f, t.rotation);
                        }
                    }
                }

                if (isServer)
                {
                    if (health <= 0)
                    {
                        if (Buffs.Any(b => b.type == BuffT.EngineerRevive))
                        {
                            var er = Buffs.First(b => b.type == BuffT.EngineerRevive);
                            if (Time.time - er.startTime > 20)
                            {
                                health = (int) (RobotPerformanceTable.Table[level][role.Type][chassisType][gunType]
                                    .HealthLimit * 0.2f);
                                Buffs.RemoveAll(b => b.type == BuffT.EngineerRevive);
                                if (Buffs.All(b => b.type != BuffT.ReviveProtect))
                                    Buffs.Add(new ReviveProtectBuff(10));
                            }
                            else reviveTime = Mathf.RoundToInt(20 - (Time.time - er.startTime));
                        }
                    }
                }
            }
        }
    }
}