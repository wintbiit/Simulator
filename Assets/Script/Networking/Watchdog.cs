using UnityEngine;
using UnityEngine.SceneManagement;

namespace Script.Networking
{
    public class Watchdog : MonoBehaviour
    {
        private float _startTime;

        private void Start()
        {
            _startTime = Time.time;
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (Time.time - _startTime > 0.3 && FindObjectsOfType<Camera>().Length == 0)
                SceneManager.LoadScene("Index");
#endif
        }
    }
}