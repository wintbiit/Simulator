using System.Collections;
using UnityEngine;

namespace Script.UI
{
    public class PunishUI : MonoBehaviour
    {
        public GameObject panel;

        private void Start()
        {
            panel.SetActive(false);
        }

        public void Punish(int time)
        {
            panel.SetActive(true);
            StartCoroutine(PunishFor(time));
        }

        private IEnumerator PunishFor(int time)
        {
            yield return new WaitForSeconds(time);
            panel.SetActive(false);
        }
    }
}