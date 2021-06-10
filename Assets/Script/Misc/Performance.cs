using UnityEngine;

namespace Script.Misc
{
    public class Performance : MonoBehaviour
    {
        private float _startTime;

        private void Update()
        {
            if (_startTime == 0) _startTime = Time.time;
            else if (Time.time - _startTime > 5) QualitySettings.DecreaseLevel();
            else if (Time.deltaTime < 1.0f / 30) _startTime = Time.time;
        }
    }
}