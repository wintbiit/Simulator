using System;
using Script.Networking.Game;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                    second = _gm.globalStatus.countDown;
                    // TODO: Fix
                    if (second <= 0 && Time.time - _gm.globalStatus.finishTime > 10)
                    {
                        try
                        {
                            _gm.CmdReset();
                        }
                        catch
                        {
                            // ignored
                        }

                        SceneManager.LoadScene("Index");
                    }
                }

                if (_gm.globalStatus.countDown >= 0)
                {
                    if (_gm.globalStatus.countDown > 430)
                        countDown.text = "调整站位";
                    else
                        countDown.text = minute + ":" + (second < 10 ? "0" : "") + second;
                }
            }
        }
    }
}