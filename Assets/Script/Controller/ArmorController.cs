﻿using System;
using System.Collections;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.Controller
{
    namespace Armor
    {
        /*
         * 装甲板颜色种类
         * 红色与蓝色
         * Down暂时没有使用
         */
        public enum ColorT
        {
            Down = 0,
            Red = 1,
            Blue = 2
        }

        /*
         * 实例化于每一个装甲板预制件上
         * + 设置灯光颜色
         * + 设置数字标记
         */
        public class ArmorController : MonoBehaviour
        {
            public MeshRenderer[] lights;
            public Material redLight;
            public Material blueLight;
            public Material noLight;

            public TMP_Text label;
            public bool isTriangle;

            private IVulnerable _unit;
            private ColorT _color;

            public void UnitRegister(IVulnerable unit)
            {
                _unit = unit;
            }

            public void Hit(int hitter, CaliberT caliber)
            {
                if (caliber == CaliberT.Large && GetComponentInParent<GroundControllerBase>())
                {
                    if (Random.Range(0, 2) == 0)
                    {
                        StartCoroutine(Blink(0.7f));
                        _unit?.Hit(hitter, caliber, isTriangle);
                    }
                }
                else
                {
                    StartCoroutine(Blink(caliber == CaliberT.Large ? 0.7f : 0.4f));
                    _unit?.Hit(hitter, caliber, isTriangle);
                }
            }

            public void ChangeColor(ColorT color)
            {
                _color = color;
                foreach (var armorLight in lights)
                {
                    switch (color)
                    {
                        case ColorT.Down:
                            armorLight.material = noLight;
                            break;
                        case ColorT.Red:
                            armorLight.material = redLight;
                            break;
                        case ColorT.Blue:
                            armorLight.material = blueLight;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(color), color, null);
                    }
                }

                foreach (var flare in GetComponentsInChildren<LensFlare>())
                {
                    switch (color)
                    {
                        case ColorT.Down:
                            flare.enabled = false;
                            break;
                        case ColorT.Red:
                            flare.enabled = true;
                            flare.color = Color.red;
                            break;
                        case ColorT.Blue:
                            flare.enabled = true;
                            flare.color = Color.blue;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(color), color, null);
                    }
                }
            }

            public ColorT GetColor() => _color;

            public void ChangeLabel(int labelNumber) =>
                label.text = labelNumber != 0 ? (labelNumber % 10).ToString() : "";

            public IEnumerator Blink(float t)
            {
                ChangeColor(ColorT.Down);
                yield return new WaitForSeconds(t);
                ChangeColor(_color);
            }
        }
    }
}