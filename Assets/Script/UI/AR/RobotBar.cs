using System;
using System.Linq;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.AR
{
    public class RobotBar : MonoBehaviour
    {
        public GameObject panel;
        public Image bar;
        public Image level;
        public CampT camp;
        public TypeT type;
        public Sprite level1;
        public Sprite level2;
        public Sprite level3;
        public TMP_Text extra;

        private GameManager _gm;

        private void Update()
        {
            if (!_gm) _gm = FindObjectOfType<GameManager>();
            else if (_gm.judge && !_gm.observing &&
                     _gm.clientRobotBases.Any(r => r.role.Equals(new RoleT(camp, type)) && r.health > 0))
            {
                foreach (var c in FindObjectsOfType<Camera>())
                {
                    if (c.enabled)
                    {
                        panel.SetActive(true);
                        var robot = _gm.clientRobotBases.First(r => r.role.Equals(new RoleT(camp, type)));
                        if (robot.GetComponentInChildren<MeshRenderer>().isVisible)
                        {
                            var robotPos = robot.transform.position;
                            var screenPos = c.WorldToScreenPoint(robotPos + Vector3.up * 0.7f);
                            GetComponent<RectTransform>().anchoredPosition = screenPos;
                            GetComponent<RectTransform>().localScale =
                                10 / (c.transform.position - robotPos).magnitude * Vector3.one;
                            bar.fillAmount = (float) robot.health /
                                             RobotPerformanceTable.Table[robot.level][robot.role.Type][
                                                 robot.chassisType][robot.gunType].HealthLimit;
                            switch (robot.level)
                            {
                                case 1:
                                    level.sprite = level1;
                                    level.GetComponent<RectTransform>().localScale =
                                        new Vector3(1, 0.5f, 1) * 0.1247058f;
                                    break;
                                case 2:
                                    level.sprite = level2;
                                    level.GetComponent<RectTransform>().localScale =
                                        new Vector3(1, 0.7f, 1) * 0.1247058f;
                                    break;
                                case 3:
                                    level.sprite = level3;
                                    level.GetComponent<RectTransform>().localScale = Vector3.one * 0.1247058f;
                                    break;
                            }

                            extra.text = "";
                            if (robot.smallAmmo > 0) extra.text += "S:" + robot.smallAmmo + " ";
                            if (robot.largeAmmo > 0) extra.text += "L:" + robot.largeAmmo + " ";
                            if (type != TypeT.Guard)
                                extra.text += "H:" +
                                              Math.Round(
                                                  robot.heat /
                                                  RobotPerformanceTable.Table[robot.level][robot.role.Type][
                                                      robot.chassisType][robot.gunType].HeatLimit * 100) + "% ";
                        }
                        else panel.SetActive(false);

                        break;
                    }
                }
            }
            else panel.SetActive(false);
        }
    }
}