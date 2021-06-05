using System.Collections;
using UnityEngine;

namespace Script.UI.HUD
{
    public class PunishUI : HUDBase
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