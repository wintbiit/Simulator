using System.Linq;
using Script.Controller;
using Script.JudgeSystem;
using Script.JudgeSystem.Robot;
using Script.UI.HUD;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    public class InfoScreenUI : HUDBase
    {
        public GameObject pending;
        public TMP_Text smallAmmo;
        public TMP_Text largeAmmo;
        public TMP_Text money;
        public TMP_Text decision;

        [HideInInspector] public bool quit;

        private void TogglePending(bool status)
        {
            pending.SetActive(status);
        }

        public override void Refresh(RobotBase localRobot)
        {
            TogglePending(quit || Gm.globalStatus.finished);

            // decision.text = FindObjectOfType<StrategyUI>()?.strategyDisplay.text;
            decision.text = "";
            if (Gm.globalStatus.countDown <= 420)
            {
                var strategy = FindObjectOfType<StrategyUI>();
                if (strategy)
                {
                    var decider = strategy.Decider;
                    if (decider.Code != -1)
                    {
                        var table = StrategyTable.Table[decider.Code].Messages;
                        foreach (var k in table.Keys)
                        {
                            decision.text += k + ": " + table[k] + "\n";
                        }
                    }
                }
            }

            var campRobots =
                Gm.clientRobotBases.Where(r => r.role.Camp == localRobot.role.Camp && r is GroundControllerBase);
            int sA = 0, lA = 0;
            foreach (var r in campRobots)
            {
                sA += r.smallAmmo;
                lA += r.largeAmmo;
            }

            smallAmmo.text = sA.ToString();
            largeAmmo.text = lA.ToString();
            money.text = Gm.CampStatusMap[localRobot.role.Camp].money.ToString();
        }

        protected override void Clear()
        {
            TogglePending(true);
        }
    }
}