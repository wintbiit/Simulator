using Mirror;
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