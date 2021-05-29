using System;
using System.Linq;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.Event;
using Script.JudgeSystem.Facility;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using TypeT = Script.JudgeSystem.Event.TypeT;

namespace Script.Controller
{
    [Serializable]
    public class Branch
    {
        public ArmorController armor;
        public MeshRenderer light;
        public BuffMarker Marker;
    }

    public class BuffMarker : IVulnerable
    {
        public bool Ok;
        public bool Err;

        public void Hit(int hitter, CaliberT caliber, bool isTriangle)
        {
            if (Object.FindObjectsOfType<RobotBase>().First(r => r.id == hitter).Buffs
                .Any(b => b.type != BuffT.Activator))
            {
                if (!Ok)
                    Ok = true;
                else
                    Err = true;
            }
        }
    }

    public class BuffActivateEvent : GameEventBase
    {
        public readonly CampT Camp;
        public readonly bool Large;

        public BuffActivateEvent(CampT c, bool large)
        {
            Type = TypeT.BuffActivate;
            Camp = c;
            Large = large;
        }
    }

    public class SmallEnergyBuff : BuffBase
    {
        public SmallEnergyBuff()
        {
            type = BuffT.SmallEnergy;
            damageRate = 1.5f;
            armorRate = 0;
            coolDownRate = 0;
            reviveRate = 0;
            timeOut = Time.time + 45;
        }
    }

    public class LargeEnergyBuff : BuffBase
    {
        public LargeEnergyBuff()
        {
            type = BuffT.LargeEnergy;
            damageRate = 2.0f;
            armorRate = 0.5f;
            coolDownRate = 0;
            reviveRate = 0;
            timeOut = Time.time + 45;
        }
    }

    public class EnergyMechanismControllerRecord : FacilityBaseRecord
    {
        public bool Enable;
        public bool Large;
        public bool Activated;
        public int Current;
        public float LastCheck;
    }

    public class EnergyMechanismController : FacilityBase
    {
        public Material red;
        public Material blue;
        public Material down;
        public Branch[] branches = new Branch[5];

        [SyncVar] private bool _enable;
        [SyncVar] private bool _large;
        [SyncVar] private int _current;
        [SyncVar] private float _lastCheck;

        public EnergyMechanismControllerRecord RecordFrame()
        {
            var record = new EnergyMechanismControllerRecord
            {
                Enable = _enable,
                Large = _large,
                Current = _current,
                LastCheck = _lastCheck
            };
            base.RecordFrame(record);
            return record;
        }

        private void Start()
        {
            if (!isServer) return;
            ArmorSetup();
        }

        [Server]
        public void Enable(bool large)
        {
            _enable = true;
            _large = large;
            _current = Random.Range(0, 5);
            Select(_current);
        }

        [Server]
        public void Disable()
        {
            _enable = false;
            ArmorSetup();
        }

        [Server]
        private void ArmorSetup()
        {
            foreach (var b in branches)
            {
                b.armor.ChangeColor(ColorT.Down);
                b.light.material = down;
                b.Marker = new BuffMarker();
                b.armor.UnitRegister(b.Marker);
            }

            ArmorSetupRpc();
        }

        [ClientRpc]
        private void ArmorSetupRpc()
        {
            foreach (var b in branches)
            {
                b.armor.ChangeColor(ColorT.Down);
                b.light.material = down;
                b.Marker = new BuffMarker();
                b.armor.UnitRegister(b.Marker);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdSelect(int index)
        {
            Select(index);
        }

        [Server]
        private void Select(int index)
        {
            branches[index].armor.ChangeColor(role.Camp == CampT.Blue ? ColorT.Red : ColorT.Blue);
            branches[index].light.material = down;
            SelectRpc(index);
        }

        [ClientRpc]
        private void SelectRpc(int index)
        {
            branches[index].armor.ChangeColor(role.Camp == CampT.Blue ? ColorT.Red : ColorT.Blue);
            branches[index].light.material = down;
        }

        [Command(requiresAuthority = false)]
        private void CmdActive(int index)
        {
            branches[index].armor.ChangeColor(ColorT.Down);
            branches[index].light.material = role.Camp == CampT.Blue ? blue : red;
            branches[index].Marker.Ok = true;
            _lastCheck = Time.time;
            ActiveRpc(index);
        }

        [ClientRpc]
        private void ActiveRpc(int index)
        {
            branches[index].armor.ChangeColor(ColorT.Down);
            branches[index].light.material = role.Camp == CampT.Blue ? blue : red;
            branches[index].Marker.Ok = true;
        }

        [Command(requiresAuthority = false)]
        private void CmdActivate()
        {
            if (isServer)
                gameManager.Emit(new BuffActivateEvent(role.Camp, _large));
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isClient)
            {
                if (_enable)
                {
                    if (branches[_current].Marker.Ok)
                    {
                        CmdActive(_current);
                        if (branches.All(b => b.Marker.Ok)) CmdActivate();
                        else
                        {
                            while (branches[_current].Marker.Ok)
                            {
                                _current = Random.Range(0, 5);
                            }

                            CmdSelect(_current);
                        }
                    }
                }
            }

            if (isServer)
            {
                if (_enable)
                {
                    var speed = _large ? 0.785f * Mathf.Sin(1.884f * Time.time) + 1.305f : 1;
                    transform.Rotate(Vector3.forward, role.Camp == CampT.Red ? speed : -speed);
                    if (branches.Any(b => b.Marker.Ok))
                        if (Time.time - _lastCheck > 2.5f)
                        {
                            ArmorSetup();
                            Enable(_large);
                        }

                    if (branches.Any(b => b.Marker.Err))
                    {
                        ArmorSetup();
                        Enable(_large);
                    }
                }
            }
        }
    }
}