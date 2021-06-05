using System;
using Script.Networking.Game;
using TMPro;
using UnityEngine;

namespace Script.UI
{
    public class CountDownUI : MonoBehaviour
    {
        public TMP_Text countDown;
        private GameManager _gm;

        private void Update()
        {
            if (!_gm) _gm = FindObjectOfType<GameManager>();
            else
            {
                // 更新倒计时
                var minute = (int) Math.Floor(_gm.globalStatus.countDown / 60.0f);
                var second = _gm.globalStatus.countDown % 60;
                if (minute == 0 && second <= 10)
                    countDown.color = Color.red;
                // 结算页面倒计时
                if (_gm.globalStatus.finished)
                {
                    countDown.color = Color.red;
                    minute = 0;
                    second = 17 + (_gm.globalStatus.countDown -
                                   (_gm.gameTime - (_gm.globalStatus.finishTime - _gm.globalStatus.startTime)));
                    if (second == 0)
                        _gm.CmdReset();
                }

                countDown.text = minute + ":" + (second < 10 ? "0" : "") + second;
            }
        }
    }
}
