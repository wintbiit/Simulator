using System;
using Script.Controller.Bullet;
using UnityEngine;

namespace Script.JudgeSystem
{
    public class Bulletproof : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<BulletController>())
            {
                var rigid = other.GetComponent<Rigidbody>();
                if (rigid)
                    rigid.velocity *= -0.3f;
            }
        }
    }
}