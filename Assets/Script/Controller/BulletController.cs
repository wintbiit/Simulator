using System.Collections;
using Script.Controller.Armor;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    namespace Bullet
    {
        /*
         * 口径大小类型
         * 小弹丸（17mm）
         * 大弹丸（42mm）
         */
        public enum CaliberT
        {
            Small = 0,
            Large = 1,
            Dart = 2
        }

        /*
         * 子弹控制器脚本
         * + 每一个子弹预制件实例都带有
         * + 由发射者进行碰撞检测和伤害判定
         */
        public class BulletController : MonoBehaviour
        {
            // 若非 isActive 证明该实例不存在于发射者端，不予判定
            public bool isActive;

            // 发射者ID
            public int owner;
            public CaliberT caliber = CaliberT.Small;

            public AudioClip hit;
            public AudioClip drop;

            private void OnCollisionEnter(Collision other)
            {
                if (other.gameObject.name == "Arena21")
                {
                    if (FindObjectOfType<GameManager>().judge)
                    {
                        var a = GetComponent<AudioSource>();
                        a.clip = drop;
                        a.Play();
                    }

                    StartCoroutine(RemoveComponents());
                }
                else
                {
                    if (other.collider.CompareTag("Armor"))
                    {
                        if (FindObjectOfType<GameManager>().judge)
                        {
                            var a = GetComponent<AudioSource>();
                            a.clip = hit;
                            a.Play();
                        }

                        if (isActive) other.gameObject.GetComponent<ArmorController>().Hit(owner, caliber);
                        StartCoroutine(RemoveComponents());
                    }
                }
            }

            private IEnumerator RemoveComponents()
            {
                yield return new WaitForSeconds(4);
                var r = GetComponent<Rigidbody>();
                var a = GetComponent<AudioSource>();
                if (r)
                {
                    r.velocity = Vector3.zero;
                    Destroy(r);
                }

                if (a) Destroy(a);
            }
        }
    }
}