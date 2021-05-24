using Script.Controller;
using UnityEngine;

namespace Script.JudgeSystem
{
    public class BlockSnap : MonoBehaviour
    {
        public GameObject target;

        private void Start()
        {
            target.transform.Rotate(Vector3.down * 90);
            target.GetComponent<MeshRenderer>().enabled = false;
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.GetComponent<BlockController>())
                if (!other.GetComponent<Rigidbody>().isKinematic)
                    other.GetComponent<BlockController>().Snap(target.transform.position, target.transform.rotation);
        }
    }
}