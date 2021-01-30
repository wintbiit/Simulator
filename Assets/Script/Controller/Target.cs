using UnityEngine;

namespace Script.Controller
{
    public class Target : MonoBehaviour
    {
        // Start is called before the first frame update
        private void Start()
        {
            Refresh();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Armor")) return;
            Draw(transform, Color.red);
            Invoke(nameof(Refresh), 2.5f);
        }

        private void Refresh()
        {
            Draw(transform, Color.white);
        }

        private static void Draw(Component obj, Color col)
        {
            if (obj.GetComponent<MeshRenderer>() != null)
            {
                obj.GetComponent<MeshRenderer>().material.color = col;
            }

            if (obj.transform.childCount == 0) return;
            for (var i = 0; i < obj.transform.childCount; i++)
            {
                Draw(obj.transform.GetChild(i), col);
            }
        }
    }
}