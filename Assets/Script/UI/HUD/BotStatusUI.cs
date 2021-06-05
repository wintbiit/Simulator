using System;
using System.Collections.Generic;
using System.Linq;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    [Serializable]
    public class StatusDisplay
    {
        public TypeT type;
        public RawImage status;
    }

    public class BotStatusUI : HUDBase
    {
        public List<StatusDisplay> redStatusDisplay = new List<StatusDisplay>();
        public List<StatusDisplay> blueStatusDisplay = new List<StatusDisplay>();
        public Texture protectStatus;
        public Texture attackStatus;

        protected override void Refresh(RobotBase localRobot)
        {
            // Bot Status
            foreach (var sd in redStatusDisplay) sd.status.color = new Color(0, 0, 0, 0);
            foreach (var sd in blueStatusDisplay) sd.status.color = new Color(0, 0, 0, 0);
            foreach (var sd in from sd in redStatusDisplay
                where Gm.clientRobotBases.Any(r => r.role.Equals(new RoleT(CampT.Red, sd.type)))
                let robot = Gm.clientRobotBases.First(r =>
                    r.role.Equals(new RoleT(CampT.Red, sd.type)))
                where robot.health <
                      RobotPerformanceTable.Table[robot.level][robot.role.Type][robot.chassisType][
                          robot.gunType].HealthLimit * 0.75f
                select sd)
            {
                sd.status.texture = localRobot.role.Camp == CampT.Red
                    ? protectStatus
                    : attackStatus;
                sd.status.color = new Color32(230, 255, 177, 200);
            }

            foreach (var sd in from sd in blueStatusDisplay
                where Gm.clientRobotBases.Any(r => r.role.Equals(new RoleT(CampT.Blue, sd.type)))
                let robot = Gm.clientRobotBases.First(r =>
                    r.role.Equals(new RoleT(CampT.Blue, sd.type)))
                where robot.health <
                      RobotPerformanceTable.Table[robot.level][robot.role.Type][robot.chassisType][
                          robot.gunType].HealthLimit * 0.75f
                select sd)
            {
                sd.status.texture = localRobot.role.Camp == CampT.Blue
                    ? protectStatus
                    : attackStatus;
                sd.status.color = new Color32(230, 255, 177, 200);
            }
        }
    }
}