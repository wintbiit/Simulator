using Script.Controller.Engineer;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    public class AimUI : HUDBase
    {
        public TMP_Text speedDisplay;
        public TMP_Text ammoDisplay;
        public Image operationProcess;
        public GameObject staticUI;

        protected override void Clear()
        {
            speedDisplay.enabled = false;
            ammoDisplay.enabled = false;
            operationProcess.enabled = false;
            staticUI.SetActive(false);
        }

        public override void Refresh(RobotBase localRobot)
        {
            speedDisplay.enabled = true;
            ammoDisplay.enabled = true;
            operationProcess.enabled = true;
            operationProcess.fillAmount = 0;
            staticUI.SetActive(true);
            ammoDisplay.text = "0";
            if (localRobot.smallAmmo != 0) ammoDisplay.text = localRobot.smallAmmo.ToString();
            if (localRobot.largeAmmo != 0) ammoDisplay.text = localRobot.largeAmmo.ToString();
            speedDisplay.text =
                RobotPerformanceTable.Table[localRobot.level][localRobot.role.Type][
                    localRobot.chassisType][localRobot.gunType].VelocityLimit + "m/s";
            if (localRobot.role.Type == TypeT.Engineer)
                operationProcess.fillAmount = ((EngineerController) localRobot).opProcess;
        }
    }
}