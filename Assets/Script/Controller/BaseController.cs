using System;
using System.Collections.Generic;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.Facility;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Role;

namespace Script.Controller
{
    public class BaseControllerRecord : FacilityBaseRecord
    {
        
    }
    public class BaseController : FacilityBase, IVulnerable
    {
        public List<ArmorController> armors = new List<ArmorController>();

        public BaseControllerRecord RecordFrame()
        {
            var record = new BaseControllerRecord();
            base.RecordFrame(record);
            return record;
        }
        
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

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            ArmorSetup();
        }

        public void Hit(int hitter, CaliberT caliber, bool isTriangle)
        {
            CmdHit(hitter, caliber, isTriangle);
        }

        [Command(requiresAuthority = false)]
        private void CmdHit(int hitter, CaliberT caliber, bool isTriangle)
        {
            var newEvent = new HitEvent(hitter, id, caliber) {IsTriangle = isTriangle};
            gameManager.Emit(newEvent);
        }
    }
}