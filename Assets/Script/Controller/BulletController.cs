using System;
using Script.Controller.Armor;
using UnityEngine;

namespace Script.Controller
{
    namespace Bullet
    {
        public enum CaliberT
        {
            Small = 0,
            Large = 1
        }
        public class BulletController : MonoBehaviour
        {
            public bool isActive;
            public int owner;
            public CaliberT caliber = CaliberT.Small;

            private void OnCollisionEnter(Collision other)
            {
                if (!isActive || !other.collider.CompareTag("Armor")) return;
                Debug.Log("Hit");
                other.gameObject.GetComponent<ArmorController>();
            }
        }
    }
}