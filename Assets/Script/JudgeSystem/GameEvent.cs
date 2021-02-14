using Script.Controller.Bullet;
using Script.JudgeSystem.Event;

namespace Script.JudgeSystem
{
    namespace GameEvent
    {
        public class TimeEvent : GameEventBase
        {
            public TimeEvent(TypeT type)
            {
                Type = type;
            }
        }

        public class HitEvent : GameEventBase
        {
            public readonly int Hitter;
            public readonly int Target;
            public readonly CaliberT Caliber;

            public HitEvent(int hitter, int target, CaliberT caliber)
            {
                Type = TypeT.Hit;
                Hitter = hitter;
                Target = target;
                Caliber = caliber;
            }
        }
    }
}