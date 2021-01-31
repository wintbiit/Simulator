using System;

namespace Script.JudgeSystem
{
    namespace Role
    {
        public enum CampT
        {
            Unknown = -1,
            Red = 0,
            Blue = 1,
            Judge = 2
        }

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

        public class RoleTag
        {
            public readonly CampT Camp;
            public readonly TypeT Type;

            public RoleTag()
            {
                Camp = CampT.Unknown;
                Type = TypeT.Unknown;
            }

            public RoleTag(CampT camp, TypeT type)
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

            public bool Equals(RoleTag obj)
            {
                return obj.Camp == Camp && obj.Type == Type;
            }
        }
    }
}