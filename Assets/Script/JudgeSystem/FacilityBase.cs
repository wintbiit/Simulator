using System.Linq;
using Mirror;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.JudgeSystem
{
    namespace Facility
    {
        public abstract class FacilityBase : NetworkBehaviour
        {
            [SyncVar] public int id;
            [SyncVar] public RoleT role;

            [SyncVar] public int health;
            [SyncVar] public int healthLimit;

            public readonly SyncList<BuffBase> Buffs = new SyncList<BuffBase>();

            public GameManager gameManager;

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