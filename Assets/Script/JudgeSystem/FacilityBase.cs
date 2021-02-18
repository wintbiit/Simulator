using Mirror;
using Script.JudgeSystem.Role;
using Script.Networking.Game;

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
            [SyncVar] public float armorRate;
            
            public GameManager gameManager;
        }
    }
}