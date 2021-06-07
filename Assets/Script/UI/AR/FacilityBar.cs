using System.Linq;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.AR
{
    public class FacilityBar : MonoBehaviour
    {
        public Image bg;
        public Image bar;
        public CampT camp;
        public TypeT type;

        private GameManager _gm;

        // Update is called once per frame
        private void Update()
        {
            if (!_gm) _gm = FindObjectOfType<GameManager>();
            else if (_gm.judge && !_gm.observing && _gm.clientFacilityBases.Any(f => f.role.Equals(new RoleT(camp, type))))
            {
                foreach (var c in FindObjectsOfType<Camera>())
                {
                    if (c.enabled)
                    {
                        var facility = _gm.clientFacilityBases.First(f => f.role.Equals(new RoleT(camp, type)));
                        if (facility.GetComponentInChildren<MeshRenderer>().isVisible)
                        {
                            var facilityPos = facility.transform.position;
                            var screenPos =
                                c.WorldToScreenPoint(facilityPos + Vector3.up * (type == TypeT.Outpost ? 1.8f : 1.5f));
                            GetComponent<RectTransform>().anchoredPosition = screenPos / GetComponentInParent<CanvasScaler>().scaleFactor;
                            GetComponent<RectTransform>().localScale =
                                10 / (c.transform.position - facilityPos).magnitude * Vector3.one;
                            bg.enabled = facility.health > 0;
                            bar.fillAmount = (float) facility.health / (type == TypeT.Outpost ? 1000 : 5500);
                        }
                        else
                        {
                            bg.enabled = false;
                            bar.fillAmount = 0;
                        }

                        break;
                    }
                }
            }
            else
            {
                bg.enabled = false;
                bar.fillAmount = 0;
            }
        }
    }
}