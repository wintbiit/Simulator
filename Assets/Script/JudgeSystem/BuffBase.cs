using System;
using UnityEngine.Serialization;

namespace Script.JudgeSystem
{
    [Serializable]
    public enum BuffT
    {
        HeroSnipe,
        SmallEnergy,
        LargeEnergy,
        OutpostBaseSnipe,
        Base,
        Vcd,
        Activator,
        EngineerRevive,
        ReviveProtect,
        Revive,
        Jump
    }
    
    [Serializable]
    public class BuffBase
    {
        public BuffT type;
        public float damageRate;
        public float armorRate;
        public float coolDownRate;
        public float reviveRate;
        public float timeOut;
        public float startTime;
    }
}