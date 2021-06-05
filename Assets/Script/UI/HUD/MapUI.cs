using System;
using System.Collections.Generic;
using System.Linq;
using Script.Controller;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    [Serializable]
    public class MapRobot
    {
        public TypeT type;
        public RawImage image;

        public void InitWithColor(Color col)
        {
            image.color = col;
            image.gameObject.SetActive(true);
        }
    }

    public class MapUI : HUDBase
    {
        public List<MapRobot> mapRobots = new List<MapRobot>();

        private void Start()
        {
            foreach (var mr in mapRobots) mr.image.gameObject.SetActive(false);
        }

        protected override void Refresh(RobotBase localRobot)
        {
            GetComponent<RawImage>().enabled = true;
            foreach (var r in Gm.clientRobotBases)
            {
                if (!localRobot || r.role.Camp != localRobot.role.Camp) continue;
                if (!(r is GroundControllerBase)) continue;
                var mr = mapRobots.First(m => m.type == r.role.Type);
                mr.InitWithColor(localRobot.role.Camp == CampT.Red ? Color.red : Color.blue);
                if (r.health == 0) mr.InitWithColor(Color.gray);
                var p = r.transform.position;
                mr.image.rectTransform.anchoredPosition = new Vector2(
                    p.z * -1 * (83 / 13.6f), p.x * (43 / 7.1f));
            }
        }

        protected override void Clear()
        {
            GetComponent<RawImage>().enabled = false;
            foreach (var mr in mapRobots) mr.image.gameObject.SetActive(false);
        }
    }
}