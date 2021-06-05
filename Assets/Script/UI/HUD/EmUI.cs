using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI.HUD
{
    public class EmUI : HUDBase
    {
        public RawImage emHint;
        private bool _show;

        public void Activate()
        {
            if (!show) return;
            _show = true;
            GameObject.Find("emSound").GetComponent<AudioSource>().Play();
            StartCoroutine(Hide());
        }

        private IEnumerator Hide()
        {
            yield return new WaitForSeconds(3);
            _show = false;
        }

        private void Update() =>
            emHint.color = new Color(255, 255, 255, _show ? 255 : 0);
    }
}