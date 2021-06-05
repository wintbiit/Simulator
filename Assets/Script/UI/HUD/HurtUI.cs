using Script.JudgeSystem.Robot;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    public class HurtUI : HUDBase
    {
        public RawImage hurtHint;

        public void Hurt()
        {
            hurtHint.color = new Color(1, 0, 0, 1);
        }

        public override void Refresh(RobotBase localRobot)
        {
            hurtHint.color = hurtHint.color.a >= 0.04f
                ? new Color(1, 0, 0, hurtHint.color.a - 0.04f)
                : new Color(1, 0, 0, 0);
        }
    }
}