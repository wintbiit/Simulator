using Mirror;
using Script.Controller.Bullet;
using Script.JudgeSystem.Role;

namespace Script.JudgeSystem
{
    namespace Facility
    {
        public enum TypeT
        {
            SupplyStation = 0,
            DartLauncher = 1,
            Base = 2,
            Outpost = 3,
            EnergyMechanism = 4
        }
        
        public abstract class FacilityBase: NetworkBehaviour
        {
            [SyncVar] public CampT camp; 
            [SyncVar] public TypeT type;
        }
    }
}