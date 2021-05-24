using System;
using Mirror;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    public enum MineType
    {
        Silver = 0,
        Gold = 1
    }

    public class MineController : NetworkBehaviour
    {
        public MineType type;
        public int index;
        private GameManager _gameManager;

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        private void FixedUpdate()
        {
            if (type == MineType.Gold)
            {
                if (_gameManager.playing)
                {
                    if (_gameManager.countDown <= 405)
                    {
                        if (index == 2 || index == 4)
                            GetComponent<Rigidbody>().isKinematic = false;
                    }

                    if (_gameManager.countDown <= 240)
                    {
                        if (index == 1 || index == 3 || index == 5)
                            GetComponent<Rigidbody>().isKinematic = false;
                    }
                }
            }
        }

        public void Collect()
        {
            CmdCollect();
        }

        [Command(ignoreAuthority = true)]
        private void CmdCollect()
        {
            NetworkServer.Destroy(transform.gameObject);
        }
    }
}