using System;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using TMPro;
using UnityEngine;

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

            private IVulnerable _unit;
            private ColorT _color;

            public void UnitRegister(IVulnerable unit)
            {
                _unit = unit;
            }

            public void Hit(int hitter, CaliberT caliber)
            {
                _unit?.Hit(hitter, caliber);
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
            }

            public ColorT GetColor() => _color;

            public void ChangeLabel(int labelNumber) => label.text = labelNumber != 0 ? (labelNumber % 10).ToString() : "";
        }
    }
}