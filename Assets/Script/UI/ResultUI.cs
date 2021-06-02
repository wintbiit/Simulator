using System;
using System.Collections.Generic;
using System.Linq;
using Script.Controller;
using Script.JudgeSystem.Facility;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    [Serializable]
    public class CampResult
    {
        public CampT camp;
        public TMP_Text baseHp;
        public TMP_Text outpostHp;
        public TMP_Text guardHp;
        public TMP_Text damage;
    }

    public class ResultUI : MonoBehaviour
    {
        public List<CampResult> displays = new List<CampResult>();

        private void Update()
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm && FindObjectsOfType<FacilityBase>().Length > 0)
            {
                foreach (var cd in displays)
                {
                    var camp = cd.camp;
                    var display = displays.First(d => d.camp == camp);
                    display.baseHp.text = FindObjectsOfType<BaseController>().First(f => f.role.Camp == camp).health
                        .ToString();
                    display.outpostHp.text = FindObjectsOfType<OutpostController>().First(f => f.role.Camp == camp)
                        .health.ToString();
                    display.guardHp.text = FindObjectsOfType<GuardController>().First(r => r.role.Camp == camp)
                        .health.ToString();
                    display.damage.text = gm.CampStatusMap[camp].damage.ToString();
                }
            }
        }
    }
}