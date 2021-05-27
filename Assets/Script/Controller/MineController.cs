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

    public class MineControllerRecord
    {
        public MineType Type;
        public int Index;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public class MineController : NetworkBehaviour
    {
        public MineType type;
        public int index;
        private GameManager _gameManager;

        public MineControllerRecord RecordFrame()
        {
            var t = transform;
            return new MineControllerRecord
            {
                Type = type,
                Index = index,
                Position = t.position,
                Rotation = t.rotation
            };
        }

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
        }

        private void FixedUpdate()
        {
            if (type == MineType.Gold)
            {
                if (_gameManager.globalStatus.playing)
                {
                    if (_gameManager.globalStatus.countDown <= 405)
                    {
                        if (index == 2 || index == 4)
                            GetComponent<Rigidbody>().isKinematic = false;
                    }

                    if (_gameManager.globalStatus.countDown <= 240)
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