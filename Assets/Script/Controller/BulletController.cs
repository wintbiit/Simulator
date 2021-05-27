using System.Collections;
using Script.Controller.Armor;
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

            private void Start()
            {
                StartCoroutine(RemoveRigid());
            }

            private void OnCollisionEnter(Collision other)
            {
                if (!isActive || !other.collider.CompareTag("Armor")) return;
                other.gameObject.GetComponent<ArmorController>().Hit(owner, caliber);
                Destroy(this);
            }

            private IEnumerator RemoveRigid()
            {
                yield return new WaitForSeconds(4);
                Destroy(GetComponent<Rigidbody>());
            }
        }
    }
}