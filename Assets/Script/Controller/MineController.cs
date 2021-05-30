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
        [SyncVar] public int index;
        [SyncVar] public int dropTime;
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
            if (!isServer) return;
            if (type == MineType.Gold)
            {
                dropTime = _gameManager.mineDropTimes[index - 1];
            }
        }

        private void FixedUpdate()
        {
            if (type == MineType.Gold)
            {
                if (_gameManager.globalStatus.playing)
                {
                    if (_gameManager.globalStatus.countDown <= dropTime)
                    {
                        GetComponent<Rigidbody>().isKinematic = false;
                    }
                }
            }
        }

        public void Collect()
        {
            CmdCollect();
        }

        [Command(requiresAuthority = false)]
        private void CmdCollect()
        {
            NetworkServer.Destroy(transform.gameObject);
        }
    }
}