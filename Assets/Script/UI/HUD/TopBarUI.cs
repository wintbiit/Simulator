using System;
using System.Collections.Generic;
using System.Linq;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using TMPro;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    [Serializable]
    public class HealthDisplay
    {
        public TypeT type;
        public Image bar;
    }

    public class TopBarUI : HUDBase
    {
        public List<HealthDisplay> redHealthDisplays = new List<HealthDisplay>();
        public List<HealthDisplay> blueHealthDisplays = new List<HealthDisplay>();
        public Image redBaseHealthBar;
        public Image blueBaseHealthBar;
        public TMP_Text redBaseHealthDisplay;
        public TMP_Text blueBaseHealthDisplay;
        public TMP_Text redGuardHealthDisplay;
        public TMP_Text blueGuardHealthDisplay;
        public TMP_Text redOutpostHealthDisplay;
        public TMP_Text blueOutpostHealthDisplay;
        public TMP_Text redMoneyDisplay;
        public TMP_Text blueMoneyDisplay;

        protected override void Refresh(RobotBase localRobot)
        {
            foreach (var r in Gm.clientRobotBases)
            {
                switch (r.role.Type)
                {
                    case TypeT.Drone:
                        continue;
                    case TypeT.Guard:
                    {
                        var display = r.role.Camp == CampT.Red
                            ? redGuardHealthDisplay
                            : blueGuardHealthDisplay;
                        display.text = r.health.ToString();
                        break;
                    }
                    default:
                    {
                        var display = r.role.Camp == CampT.Red
                            ? redHealthDisplays.First(hd => hd.type == r.role.Type)
                            : blueHealthDisplays.First(hd => hd.type == r.role.Type);
                        var healthRate = (float) r.health /
                                         RobotPerformanceTable.Table[r.level][r.role.Type][r.chassisType][
                                                 r.gunType]
                                             .HealthLimit;
                        display.bar.fillAmount = healthRate;
                        break;
                    }
                }
            }

            foreach (var f in Gm.clientFacilityBases.Where(f => f.role.Type != TypeT.EnergyMechanism))
            {
                switch (f.role.Type)
                {
                    case TypeT.Outpost:
                    {
                        var hd = f.role.Camp == CampT.Red ? redOutpostHealthDisplay : blueOutpostHealthDisplay;
                        hd.text = f.health.ToString();
                        break;
                    }
                    case TypeT.Base:
                    {
                        var hd = f.role.Camp == CampT.Red ? redBaseHealthDisplay : blueBaseHealthDisplay;
                        hd.text = f.health.ToString();
                        var display = f.role.Camp == CampT.Red ? redBaseHealthBar : blueBaseHealthBar;
                        var healthRate = (float) f.health / f.healthLimit;
                        display.fillAmount = healthRate;
                        break;
                    }
                }
            }
            
            redMoneyDisplay.text = Gm.CampStatusMap[CampT.Red].money.ToString();
            blueMoneyDisplay.text = Gm.CampStatusMap[CampT.Blue].money.ToString();
        }
    }
}