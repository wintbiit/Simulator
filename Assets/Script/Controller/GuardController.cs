using System;
using System.Collections.Generic;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using UnityEngine;

namespace Script.Controller
{
    public class GuardController : RobotBase, IVulnerable
    {
        public List<ArmorController> armors = new List<ArmorController>();

        private const float XMax = 2.0f;
        private const float XMin = -2.0f;
        private bool _left = true;

        private void ArmorSetup()
        {
            foreach (var armor in armors)
            {
                armor.UnitRegister(this);
                armor.ChangeLabel(0);
                switch (role.Camp)
                {
                    case CampT.Unknown:
                        armor.ChangeColor(ColorT.Down);
                        break;
                    case CampT.Red:
                        armor.ChangeColor(ColorT.Red);
                        break;
                    case CampT.Blue:
                        armor.ChangeColor(ColorT.Blue);
                        break;
                    case CampT.Judge:
                        armor.ChangeColor(ColorT.Down);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void FixedUpdate()
        {
            ArmorSetup();
            if (!isServer) return;
            if (health <= 0) return;
            if (transform.position.x > XMax)
                _left = true;
            if (transform.position.x < XMin)
                _left = false;
            transform.localPosition += Vector3.right * ((_left ? -1 : 1) * 0.05f);
        }

        public void Hit(int hitter, CaliberT caliber)
        {
            CmdHit(hitter, caliber);
        }

        [Command(ignoreAuthority = true)]
        private void CmdHit(int hitter, CaliberT caliber)
        {
            gameManager.Emit(new HitEvent(hitter, id, caliber));
        }
    }
}