using System;
using System.Collections.Generic;
using System.Linq;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    namespace Engineer
    {
        public class EngineerController : GroundControllerBase
        {
            private readonly List<int> _mine = new List<int>();
            private float _lastCollect;
            private float _lastExchange;
            private bool _drag;
            private GameObject _dragObject;

            protected override bool FireOperation()
            {
                var ray = new Ray(fpCam.transform.position, fpCam.transform.forward);
                if (!Physics.Raycast(ray, out var hit, Mathf.Infinity)) return true;
                var mc = hit.collider.GetComponent<MineController>();
                var gc = hit.collider.GetComponent<GroundControllerBase>();

                // 采矿
                if (mc != null)
                {
                    if ((hit.point - fpCam.transform.position).magnitude > 0.7f) return true;
                    if (Time.time - _lastCollect > 2.0f)
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
                }
                else if (
                    role.Camp == CampT.Red && hit.transform.name == "RE"
                    || role.Camp == CampT.Blue && hit.transform.name == "BE")
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
                }
                else if (gc != null)
                {
                    if ((hit.point - fpCam.transform.position).magnitude > 2.0f) return true;
                    if (gc.role.Camp == role.Camp)
                        // && gc.health == 0)
                    {
                        if (!_drag)
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
                    _dragObject.GetComponent<Rigidbody>().isKinematic = false;
                }

                return true;
            }

            public int MineValue() => _mine.Sum(m => m);

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isLocalRobot && health > 0)
                {
                    if (_drag)
                    {
                        var t = transform;
                        _dragObject
                            .GetComponent<GroundControllerBase>()
                            .Drag(t.position + t.forward + t.right);
                    }
                }
            }
        }
    }
}