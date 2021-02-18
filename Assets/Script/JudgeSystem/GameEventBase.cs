namespace Script.JudgeSystem
{
    namespace Event
    {
        public enum TypeT
        {
            Unknown = -1,
            Hit,
            GameStart,
            GameOver,
            SixMinute,
            FiveMinute,
            FourMinute,
            ThreeMinute,
            TwoMinute,
            OneMinute,
            Reset,
        }
        
        /*
         * 构建一个事件队列来处理赛制
         */
        public abstract class GameEventBase
        {
            public TypeT Type;
        }   
    }
}