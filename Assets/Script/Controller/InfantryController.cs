using System;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    namespace Infantry
    {
        public class InfantryController : GroundControllerBase
        {
            public bool atSupply;

            protected override void OnTriggerEnter(Collider other)
            {
                base.OnTriggerEnter(other);
                atSupply = other.name == (role.Camp == CampT.Red ? "RS" : "BS");
            }

            protected override void OnTriggerExit(Collider other)
            {
                base.OnTriggerExit(other);
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