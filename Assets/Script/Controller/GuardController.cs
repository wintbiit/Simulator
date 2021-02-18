using System;
using System.Collections.Generic;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;

namespace Script.Controller
{
    public class GuardController : RobotBase, IVulnerable
    {
        public List<ArmorController> armors = new List<ArmorController>();

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