using System;
using System.Collections.Generic;
using System.Linq;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI
{
    [Serializable]
    public class HealthDisplay
    {
        public TypeT type;
        public Image bar;
    }

    public class TopBarUI : MonoBehaviour
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

        private GameManager _gm;

        private void Update()
        {
            if (!_gm) _gm = FindObjectOfType<GameManager>();
            else
            {
                foreach (var r in _gm.clientRobotBases)
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

                foreach (var f in _gm.clientFacilityBases.Where(f => f.role.Type != TypeT.EnergyMechanism))
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

                if (_gm.CampStatusMap.ContainsKey(CampT.Red))
                    redMoneyDisplay.text = _gm.CampStatusMap[CampT.Red].money.ToString();
                if (_gm.CampStatusMap.ContainsKey(CampT.Blue))
                    blueMoneyDisplay.text = _gm.CampStatusMap[CampT.Blue].money.ToString();
            }
        }
    }
}