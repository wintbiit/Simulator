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

        private void FixedUpdate()
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm)
            {
                if (gm.playing)
                {
                    if (gm.countDown <= 408 && gm.countDown >= 406)
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

                    if (gm.countDown <= 243 && gm.countDown >= 241)
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
            }
        }
    }
}