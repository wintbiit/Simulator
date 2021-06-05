using System;
using System.Globalization;
using System.Linq;
using Mirror;
using Script.Controller;
using Script.Controller.Engineer;
using Script.Controller.Hero;
using Script.Controller.Infantry;
using Script.JudgeSystem;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    public class InfoUI : HUDBase
    {
        public TMP_Text extraDisplay;
        public GameObject infantrySupplyHint;
        public GameObject heroSupplyHint;
        public TMP_Text pitchDisplay;
        public TMP_Text levelDisplay;
        public TMP_Text capacityDisplay;
        public TMP_Text latencyDisplay;
        public TMP_Text speedDisplay;
        public TMP_Text ammoDisplay;
        public TMP_Text experienceDisplay;
        public TMP_Text healthTextDisplay;
        public Image operationProcess;
        public Image healthDisplay;
        public GameObject setupHint;
        public GameObject deadHint;

        private void Start()
        {
            infantrySupplyHint.SetActive(false);
            heroSupplyHint.SetActive(false);
        }

        protected override void Refresh(RobotBase localRobot)
        {
            setupHint.SetActive(localRobot.chassisType == ChassisT.Default && localRobot is GroundControllerBase &&
                                localRobot.role.Type != TypeT.Engineer);
            deadHint.SetActive(localRobot.health == 0);
            healthTextDisplay.text = localRobot.health + "/" +
                                     RobotPerformanceTable.Table[localRobot.level][localRobot.role.Type][
                                         localRobot.chassisType][localRobot.gunType].HealthLimit;
            if (localRobot.role.Type == TypeT.Drone || localRobot.role.Type == TypeT.Ptz)
                healthTextDisplay.text = "";

            extraDisplay.text = "";
            latencyDisplay.text = $"{Math.Round(NetworkTime.rtt * 1000)}ms";
            ammoDisplay.text = "0";
            if (localRobot.smallAmmo != 0) ammoDisplay.text = localRobot.smallAmmo.ToString();
            if (localRobot.largeAmmo != 0) ammoDisplay.text = localRobot.largeAmmo.ToString();
            speedDisplay.text =
                RobotPerformanceTable.Table[localRobot.level][localRobot.role.Type][
                    localRobot.chassisType][localRobot.gunType].VelocityLimit + "m/s";
            experienceDisplay.text = Math.Round(localRobot.experience, 1)
                .ToString(CultureInfo.InvariantCulture);
            operationProcess.fillAmount = 0;
            if (localRobot is GroundControllerBase)
            {
                var ground = localRobot.GetComponent<GroundControllerBase>();
                healthDisplay.fillAmount = (float) ground.health /
                                           RobotPerformanceTable.Table[ground.level][ground.role.Type][
                                               ground.chassisType][
                                               ground.gunType].HealthLimit;
                capacityDisplay.text = ground.role.Type != TypeT.Engineer
                    ? Math.Round(ground.capability, 4) * 100 + "%"
                    : "";
                capacityDisplay.color =
                    ground.con ? new Color32(255, 116, 84, 220) : new Color32(83, 200, 255, 220);
            }

            if (localRobot.role.Type == TypeT.Engineer)
            {
                var engineer = localRobot.GetComponent<EngineerController>();
                if (engineer.Buffs.Any(b => b.type == BuffT.EngineerRevive))
                {
                    extraDisplay.text +=
                        "revive in " + ((EngineerController) localRobot).reviveTime + "\n";
                }

                operationProcess.fillAmount = engineer.opProcess;
            }

            if (localRobot.role.IsInfantry())
            {
                var infantry = localRobot.GetComponent<InfantryController>();
                infantrySupplyHint.SetActive(infantry.atSupply);
            }

            if (localRobot.role.Type == TypeT.Hero)
            {
                var hero = localRobot.GetComponent<HeroController>();
                heroSupplyHint.SetActive(hero.atSupply);
            }

            if (localRobot.role.Type == TypeT.Drone)
            {
                if (((DroneController) localRobot).isPtz)
                {
                    var drone = localRobot.GetComponent<DroneController>();
                    if (drone.raidStart > 0)
                        extraDisplay.text +=
                            "air raid: " + Mathf.RoundToInt(30 - (Time.time - drone.raidStart)) +
                            " remain\n";
                    else if (drone.role.Camp == CampT.Red && Gm.CampStatusMap[CampT.Red].money >= 400 ||
                             drone.role.Camp == CampT.Blue && Gm.CampStatusMap[CampT.Blue].money >= 400)
                        extraDisplay.text += "press H for an air raid\n";
                    extraDisplay.text += "missile " + (4 - drone.dartCount) + "times remain\n";
                    if (drone.dartCount < 4)
                    {
                        if (drone.dartTill > Time.time)
                            extraDisplay.text += "missile ready in: " +
                                                 Mathf.RoundToInt(drone.dartTill - Time.time) + "\n";
                        else
                            extraDisplay.text += "push Y to launch missile\n";
                    }
                }
            }

            var a = localRobot.GetAttr();
            if (Math.Abs(a.DamageRate - 1) > 1e-2)
                extraDisplay.text += "attack buff: " + a.DamageRate * 100 + "%\n";
            if (Math.Abs(a.ArmorRate - 0) > 1e-2)
                extraDisplay.text += "protect buff: " + a.ArmorRate * 100 + "%\n";
            if (Math.Abs(a.ColdDownRate - 1) > 1e-2)
                extraDisplay.text += "cooldown buff: " + a.ColdDownRate * 100 + "%\n";
            if (Math.Abs(a.ReviveRate - 0) > 1e-2)
                extraDisplay.text += "revive buff: " + a.ReviveRate * 100 + "%\n";

            if (localRobot.Buffs.Any(b => b.type == BuffT.SmallEnergy))
                extraDisplay.text += "small em" + '\n';
            if (localRobot.Buffs.Any(b => b.type == BuffT.LargeEnergy))
                extraDisplay.text += "large em" + '\n';
            if (localRobot.Buffs.Any(b => b.type == BuffT.Jump))
                extraDisplay.text += "fly buff" + "\n";

            levelDisplay.text = localRobot.level.ToString();

            if (localRobot.role.IsInfantry() || localRobot.role.Type == TypeT.Hero)
            {
                var pitch = localRobot.GetComponent<GroundControllerBase>().pitch
                    .transform.localEulerAngles.x * -1;
                if (pitch < -180) pitch += 360;
                pitchDisplay.text = Math.Round(pitch, 2) + " deg";
            }

            if (localRobot.role.Type == TypeT.Ptz)
            {
                extraDisplay.text += "\n";
                foreach (var r in Gm.clientRobotBases.Where(r => r.role.Camp == localRobot.role.Camp))
                {
                    extraDisplay.text += r.role.Type + " ";
                    if (r.largeAmmo != 0)
                        extraDisplay.text += r.largeAmmo;
                    if (r.smallAmmo != 0)
                        extraDisplay.text += r.smallAmmo;
                    extraDisplay.text += "\n";
                }
            }
        }

        protected override void Clear()
        {
            setupHint.SetActive(false);
            deadHint.SetActive(false);
        }
    }
}