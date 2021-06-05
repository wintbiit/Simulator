using Script.JudgeSystem.Robot;
using Script.Networking.Game;
using UnityEngine;

namespace Script.UI.HUD
{
    public class HUDBase : MonoBehaviour
    {
        protected GameManager Gm;

        public void RefreshDisplay(RobotBase localRobot)
        {
            if (!Gm) Gm = FindObjectOfType<GameManager>();
            else if (localRobot) Refresh(localRobot);
            else Clear();
        }

        protected virtual void Refresh(RobotBase localRobot)
        {
        }

        protected virtual void Clear()
        {
        }
    }
}