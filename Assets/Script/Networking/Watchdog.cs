using Script.JudgeSystem.Facility;
using Script.Networking.Lobby;
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
            if (Time.time - _startTime > 0.3 && FindObjectsOfType<FacilityBase>().Length == 0)
            {
                var r = FindObjectOfType<RoomManager>();
                if (!r || r.roomSlots.Count == 0)
                    SceneManager.LoadScene("Index");
            }
#endif
        }
    }
}