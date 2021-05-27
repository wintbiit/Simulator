using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.JudgeSystem
{
    namespace Facility
    {
        public class FacilityBaseRecord
        {
            public int Id;
            public RoleT Role;
            public int Health;
            public int HealthLimit;
            public List<BuffBase> Buffs = new List<BuffBase>();
        }
        
        public abstract class FacilityBase : NetworkBehaviour
        {
            [SyncVar] public int id;
            [SyncVar] public RoleT role;

            [SyncVar] public int health;
            [SyncVar] public int healthLimit;

            public readonly SyncList<BuffBase> Buffs = new SyncList<BuffBase>();

            public GameManager gameManager;

            protected void RecordFrame(FacilityBaseRecord record)
            {
                record.Id = id;
                record.Role = role;
                record.Health = health;
                record.HealthLimit = healthLimit;
            }

            public float GetArmorRate()
            {
                return Buffs.Select(b => b.armorRate).Prepend(0.0f).Max();
            }

            protected virtual void FixedUpdate()
            {
                if (!isServer) return;
                foreach (var b in Buffs.Where(b => Time.time > b.timeOut))
                {
                    Buffs.Remove(b);
                }
            }
        }
    }
}