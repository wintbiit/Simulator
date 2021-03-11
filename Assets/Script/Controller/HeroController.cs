using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    namespace Hero
    {
        public class HeroController : GroundControllerBase
        {
            public bool atSupply;
            private int _fired;

            protected override void OnTriggerEnter(Collider other)
            {
                base.OnTriggerEnter(other);
                atSupply = other.name == (role.Camp == CampT.Red ? "RSZ" : "BSZ");
            }

            private void OnTriggerExit(Collider other)
            {
                if (role.Camp == CampT.Red && other.name == "RSZ"
                    || role.Camp == CampT.Blue && other.name == "BSZ")
                    atSupply = false;
            }

            private bool _oDown;
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isLocalRobot && health > 0)
                {
                    if (Input.GetKeyDown(KeyCode.O))
                    {
                        if (!_oDown)
                        {
                            _oDown = true;
                            FindObjectOfType<GameManager>().Supply(role);
                        }
                    }

                    if (Input.GetKeyUp(KeyCode.O))
                        _oDown = false;
                }
            }
        }
    }
}