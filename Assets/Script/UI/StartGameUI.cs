using Script.Networking.Game;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    public class StartGameUI : MonoBehaviour
    {
        public TMP_Text countDown;
        private GameManager _gm;

        void Update()
        {
            if (!_gm) _gm = FindObjectOfType<GameManager>();
            else
            {
                if (_gm.globalStatus.countDown > 425 || _gm.globalStatus.countDown < 420) countDown.text = "";
                else
                {
                    var timeLeft = _gm.globalStatus.countDown - 420;
                    countDown.text = timeLeft == 0 ? "Start" : timeLeft.ToString();
                }
            }
        }
    }
}