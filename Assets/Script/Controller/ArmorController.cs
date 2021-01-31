using System;
using TMPro;
using UnityEngine;

namespace Script.Controller
{
    namespace Armor
    {
        public enum ColorT
        {
            Down = 0,
            Red = 1,
            Blue = 2
        }

        public class ArmorController : MonoBehaviour
        {
            public MeshRenderer[] lights;
            public Material redLight;
            public Material blueLight;

            public TMP_Text label;

            public void ChangeColor(ColorT color)
            {
                foreach (var armorLight in lights)
                {
                    switch (color)
                    {
                        case ColorT.Down:
                            armorLight.material = null;
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

            public void ChangeLabel(int labelNumber) => label.text = (labelNumber % 10).ToString();
        }
    }
}