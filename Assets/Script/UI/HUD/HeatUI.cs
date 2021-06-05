using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    public class HeatUI : HUDBase
    {
        public RawImage overHeat;
        public Image heatProcess;
        public Image heatProcessBack;

        protected override void Clear()
        {
            overHeat.enabled = false;
            heatProcess.enabled = false;
            heatProcessBack.enabled = false;
        }

        public override void Refresh(RobotBase localRobot)
        {
            overHeat.enabled = true;
            heatProcess.enabled = true;
            heatProcessBack.enabled = true;
            if (localRobot.role.Type != TypeT.Engineer)
            {
                var heatLimit =
                    RobotPerformanceTable.Table[localRobot.level][localRobot.role.Type][
                            localRobot.chassisType][localRobot.gunType]
                        .HeatLimit;
                var processColor = localRobot.heat < heatLimit
                    ? (heatLimit - localRobot.heat) / heatLimit
                    : 0;
                heatProcess.color = new Color(1, processColor, processColor);
                heatProcess.fillAmount =
                    localRobot.heat < heatLimit ? localRobot.heat / heatLimit : 1;
                overHeat.color = new Color(1, 1, 1,
                    localRobot.heat > heatLimit
                        ? (localRobot.heat - heatLimit) / (heatLimit / 4.0f)
                        : 0);
            }
            else
            {
                heatProcess.fillAmount = 0;
                overHeat.color = new Color(1, 1, 1, 0);
            }
        }
    }
}