using System.Collections.Generic;
using Script.JudgeSystem.Robot;
using UnityEngine;

namespace Script.UI.HUD
{
    public class HUDManager : MonoBehaviour
    {
        public List<HUDBase> hudElements;

        public void SetShow(bool status)
        {
            foreach (var e in hudElements) e.show = status;
        }

        public void Refresh(RobotBase localRobot)
        {
            foreach (var e in hudElements) e.RefreshDisplay(localRobot);
        }
    }
}