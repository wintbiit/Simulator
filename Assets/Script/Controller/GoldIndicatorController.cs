using System.Linq;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    public class GoldIndicatorController : MonoBehaviour
    {
        public int index;
        public Material off;
        public Material on;

        private float _startTime;
        private GameManager _gm;
        private bool _drop;

        private void FixedUpdate()
        {
            if (_gm)
            {
                if (_gm.globalStatus.playing && !_drop)
                {
                    var mines = FindObjectsOfType<MineController>();
                    if (mines.Any(m => m.type == MineType.Gold && m.index == index))
                    {
                        var gold = mines
                            .First(m => m.type == MineType.Gold && m.index == index);
                        var remain = _gm.globalStatus.countDown - gold.dropTime;
                        if (remain == 3) _startTime = Time.time;
                        if (remain > 0 && remain <= 3)
                            for (var i = 0; i < transform.childCount; i++)
                                transform.GetChild(i).GetComponent<MeshRenderer>().material =
                                    Mathf.Sin((Time.time - _startTime) * 18) > 0 ? on : off;
                        if (remain <= 0)
                            _drop = true;
                    }
                }
            }
            else _gm = FindObjectOfType<GameManager>();
        }
    }
}