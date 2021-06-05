using System;
using System.Linq;
using Script.JudgeSystem;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    public class IndicatorUI : HUDBase
    {
        public TMP_Text label;
        public RawImage outpostIndicator;
        public RawImage guardIndicator;
        public RawImage emIndicator;
        public RawImage flyIndicator;

        protected override void Refresh(RobotBase localRobot)
        {
            // Indicator
            label.enabled = true;
            outpostIndicator.enabled = true;
            guardIndicator.enabled = true;
            emIndicator.enabled = true;
            flyIndicator.enabled = true;
            var wave = Mathf.Sin(Time.time * 6);
            var op = Gm.clientFacilityBases.First(r =>
                r.role.Equals(new RoleT(localRobot.role.Camp, TypeT.Outpost)));
            outpostIndicator.color = op.health < op.healthLimit * 0.75f
                ? new Color32(255, 70, 59, (byte) (200 * wave))
                : new Color32(230, 255, 174, 200);
            guardIndicator.color =
                Gm.clientRobotBases.First(r =>
                        r.role.Equals(new RoleT(localRobot.role.Camp, TypeT.Guard)))
                    .health <
                RobotPerformanceTable.Table[1][TypeT.Guard][ChassisT.Default][GunT.Default]
                    .HealthLimit * 0.75f
                    ? new Color32(255, 70, 59, (byte) (200 * wave))
                    : new Color32(230, 255, 174, 200);
            emIndicator.color =
                Gm.clientRobotBases.Any(rb =>
                    rb.Buffs.Any(b => b.type == BuffT.LargeEnergy || b.type == BuffT.SmallEnergy) &&
                    rb.role.Camp != localRobot.role.Camp)
                    ? new Color32(255, 70, 59, (byte) (200 * wave))
                    : new Color32(230, 255, 174, 200);
            flyIndicator.color =
                Gm.clientRobotBases.Any(rb =>
                    rb.Buffs.Any(b => b.type == BuffT.Jump) &&
                    rb.role.Camp != localRobot.role.Camp)
                    ? new Color32(255, 70, 59, (byte) (200 * wave))
                    : new Color32(230, 255, 174, 200);
        }

        protected override void Clear()
        {
            label.enabled = false;
            outpostIndicator.enabled = false;
            guardIndicator.enabled = false;
            emIndicator.enabled = false;
            flyIndicator.enabled = false;
        }
    }
}