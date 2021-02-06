namespace Script.JudgeSystem
{
    namespace Event
    {
        public enum TypeT
        {
            Unknown = -1,
            Hit = 0
        }
        
        /*
         * 构建一个事件队列来处理赛制
         */
        public class EventT
        {
            public readonly TypeT Type;

            public EventT()
            {
                Type = TypeT.Unknown;
            }

            public EventT(TypeT type)
            {
                Type = type;
            }
        }   
    }
}