using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    public class GoldIndicatorController : MonoBehaviour
    {
        public int index;
        public Material off;
        public Material on;

        private bool _activated;
        private float _startTime;
        private GameManager _gm;

        private void FixedUpdate()
        {
            if (_gm)
            {
                if (_gm.globalStatus.playing)
                {
                    if (_gm.globalStatus.countDown <= 408 && _gm.globalStatus.countDown >= 406)
                    {
                        if (index == 2 || index == 4)
                        {
                            if (!_activated)
                            {
                                _activated = true;
                                _startTime = Time.time;
                            }

                            for (var i = 0; i < transform.childCount; i++)
                                transform.GetChild(i).GetComponent<MeshRenderer>().material =
                                    Mathf.Sin((Time.time - _startTime) * 18) > 0 ? on : off;
                        }
                    }

                    if (_gm.globalStatus.countDown <= 243 && _gm.globalStatus.countDown >= 241)
                    {
                        if (index == 1 || index == 3 || index == 5)
                        {
                            if (!_activated)
                            {
                                _activated = true;
                                _startTime = Time.time;
                            }

                            for (var i = 0; i < transform.childCount; i++)
                                transform.GetChild(i).GetComponent<MeshRenderer>().material =
                                    Mathf.Sin((Time.time - _startTime) * 18) > 0 ? on : off;
                        }
                    }
                }
            } else _gm = FindObjectOfType<GameManager>();
        }
    }
}