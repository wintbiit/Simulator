namespace Script.JudgeSystem
{
    namespace Role
    {
        /*
         * 阵营类型
         * 未选择阵营为 Unknown
         * 红方与蓝方阵营
         * 裁判阵营
         */
        public enum CampT
        {
            Unknown = -1,
            Red = 0,
            Blue = 1,
            Judge = 2
        }

        /*
         * 职业类型
         * 未选择职业或裁判阵营为 Unknown
         * 英雄、工程、至多三个步兵、云台手、飞手
         */
        public enum TypeT
        {
            Unknown = -1,
            Hero = 0,
            Engineer = 1,
            InfantryA = 2,
            InfantryB = 3,
            InfantryC = 4,
            Ptz = 5,
            Drone = 6
        }

        // 角色类型包含了阵营与职业信息
        public class RoleT
        {
            public readonly CampT Camp;
            public readonly TypeT Type;

            public RoleT()
            {
                Camp = CampT.Unknown;
                Type = TypeT.Unknown;
            }

            public RoleT(CampT camp, TypeT type)
            {
                Camp = camp;
                Type = type;
            }

            public bool IsInfantry()
            {
                return Type == TypeT.InfantryA
                       || Type == TypeT.InfantryB
                       || Type == TypeT.InfantryC;
            }

            public bool IsRed()
            {
                return Camp == CampT.Red;
            }

            public bool Equals(RoleT obj)
            {
                return obj.Camp == Camp && obj.Type == Type;
            }
        }
    }
}