using System;
using System.Linq;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.AR
{
    public class BaseShield : MonoBehaviour
    {
        public Image shield;
        public CampT camp;
        private GameManager _gm;

        private void Update()
        {
            if (!_gm) _gm = FindObjectOfType<GameManager>();
            else if (_gm.judge && !_gm.observing && _gm.clientFacilityBases.Any(f => f.role.Equals(new RoleT(camp, TypeT.Base))))
            {
                foreach (var c in FindObjectsOfType<Camera>())
                {
                    if (c.enabled)
                    {
                        var facility = _gm.clientFacilityBases.First(f => f.role.Equals(new RoleT(camp, TypeT.Base)));
                        if (facility.GetComponentInChildren<MeshRenderer>().isVisible)
                        {
                            var facilityPos = facility.transform.position;
                            var screenPos = c.WorldToScreenPoint(facilityPos);
                            GetComponent<RectTransform>().anchoredPosition = screenPos;
                            GetComponent<RectTransform>().localScale =
                                8 / (c.transform.position - facilityPos).magnitude * Vector3.one;
                            shield.enabled = _gm.clientRobotBases.Any(r =>
                                r.role.Equals(new RoleT(camp, TypeT.Guard)) && r.health > 0);
                        }
                        else shield.enabled = false;

                        break;
                    }
                }
            }
            else shield.enabled = false;
        }
    }
}