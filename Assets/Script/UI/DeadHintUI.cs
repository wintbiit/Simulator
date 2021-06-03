using System.Collections;
using Script.JudgeSystem.Role;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    public class DeadHintUI : MonoBehaviour
    {
        public GameObject deadHint;
        public TMP_Text killer;
        public TMP_Text victim;
        public TMP_Text method;

        private int _amount;

        private void Start()
        {
            deadHint.SetActive(false);
        }

        public void Hint(RoleT k, RoleT v, string m)
        {
            killer.text = k.Type.ToString();
            killer.color = k.Camp == CampT.Red ? Color.white : Color.blue;
            victim.text = v.Type.ToString();
            victim.color = v.Camp == CampT.Red ? Color.white : Color.blue;
            method.text = m;
            deadHint.SetActive(true);
            StartCoroutine(HideHint());
            if (_amount == 0)
                GameObject.Find("fbSound").GetComponent<AudioSource>().Play();
            GameObject.Find("killSound").GetComponent<AudioSource>().Play();
            _amount++;
        }

        private IEnumerator HideHint()
        {
            yield return new WaitForSeconds(2);
            deadHint.SetActive(false);
        }
    }
}