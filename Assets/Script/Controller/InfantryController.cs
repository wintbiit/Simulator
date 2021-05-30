using Mirror;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    namespace Infantry
    {
        public class InfantryControllerRecord : GroundControllerBaseRecord
        {
        }

        public class InfantryController : GroundControllerBase
        {
            [SyncVar] public bool atSupply;

            public InfantryControllerRecord RecordFrame()
            {
                var record = new InfantryControllerRecord();
                base.RecordFrame(record);
                return record;
            }

            protected override void OnTriggerEnter(Collider other)
            {
                base.OnTriggerEnter(other);
                if (!isServer) return;
                Debug.Log(other.name);
                if (other.name == "RS" || other.name == "BS")
                    atSupply = other.name == (role.Camp == CampT.Red ? "RS" : "BS");
            }

            protected override void OnTriggerExit(Collider other)
            {
                base.OnTriggerExit(other);
                if (!isServer) return;
                if (role.Camp == CampT.Red && other.name == "RS"
                    || role.Camp == CampT.Blue && other.name == "BS")
                    atSupply = false;
            }

            private bool _oDown;

            private void Update()
            {
                if (isLocalRobot && health > 0)
                {
                    if (Input.GetKeyDown(KeyCode.O) && atSupply)
                    {
                        if (!_oDown)
                        {
                            _oDown = true;
                            FindObjectOfType<GameManager>().Supply(role, smallAmmo);
                        }
                    }

                    if (Input.GetKeyUp(KeyCode.O))
                        _oDown = false;
                }
            }
        }
    }
}