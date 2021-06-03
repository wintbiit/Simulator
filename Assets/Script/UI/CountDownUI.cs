using System.Collections;
using System.Collections.Generic;
using Script.Networking.Game;
using TMPro;
using UnityEngine;

public class CountDownUI : MonoBehaviour
{
    public TMP_Text countDown;
    private GameManager _gm;

    void Update()
    {
        if (!_gm) _gm = FindObjectOfType<GameManager>();
        else
        {
            if (_gm.gameTime - 5 - _gm.globalStatus.countDown > 6) countDown.text = "";
            else
            {
                var timeLeft = (_gm.globalStatus.countDown - _gm.gameTime + 5) + 6;
                if (timeLeft < 6) countDown.text = timeLeft == 0 ? "Start" : timeLeft.ToString();
                else countDown.text = "";
            }
        }
    }
}