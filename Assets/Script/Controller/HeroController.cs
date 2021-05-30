using System.Linq;
using Mirror;
using Script.JudgeSystem;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    namespace Hero
    {
        public class HeroSnipeBuff : BuffBase
        {
            public HeroSnipeBuff()
            {
                type = BuffT.HeroSnipe;
                damageRate = 2.5f;
                timeOut = float.MaxValue;
            }
        }

        public class HeroControllerRecord : GroundControllerBaseRecord
        {
        }

        public class HeroController : GroundControllerBase
        {
            [SyncVar] public bool atSupply;
            private int _fired;

            public HeroControllerRecord RecordFrame()
            {
                var record = new HeroControllerRecord();
                base.RecordFrame(record);
                return record;
            }

            protected override void OnTriggerEnter(Collider other)
            {
                base.OnTriggerEnter(other);
                if (!isServer) return;
                if (other.name == "RSZ" || other.name == "BSZ")
                    atSupply = other.name == (role.Camp == CampT.Red ? "RSZ" : "BSZ");
                switch (other.name)
                {
                    case "BSP":
                        if (role.Camp == CampT.Blue)
                            if (Buffs.All(b => b.type != BuffT.HeroSnipe))
                                Buffs.Add(new HeroSnipeBuff());
                        break;
                    case "RSP":
                        if (role.Camp == CampT.Red)
                            if (Buffs.All(b => b.type != BuffT.HeroSnipe))
                                Buffs.Add(new HeroSnipeBuff());
                        break;
                }
            }

            protected override void OnTriggerExit(Collider other)
            {
                base.OnTriggerExit(other);
                if (!isServer) return;
                if (role.Camp == CampT.Red && other.name == "RSZ"
                    || role.Camp == CampT.Blue && other.name == "BSZ")
                    atSupply = false;
                switch (other.name)
                {
                    case "BSP":
                        if (role.Camp == CampT.Blue)
                            Buffs.RemoveAll(b => b.type == BuffT.HeroSnipe);
                        break;
                    case "RSP":
                        if (role.Camp == CampT.Red)
                            Buffs.RemoveAll(b => b.type == BuffT.HeroSnipe);
                        break;
                }
            }

            private bool _oDown;

            private void Update()
            {
                if (isLocalRobot && health > 0)
                {
                    if (Input.GetKeyDown(KeyCode.I) && atSupply)
                    {
                        if (!_oDown)
                        {
                            _oDown = true;
                            FindObjectOfType<GameManager>().Supply(role, largeAmmo);
                        }
                    }

                    if (Input.GetKeyUp(KeyCode.I))
                        _oDown = false;
                }
            }
        }
    }
}