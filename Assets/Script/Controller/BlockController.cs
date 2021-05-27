using Mirror;
using UnityEngine;

namespace Script.Controller
{
    public class BlockControllerRecord
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public class BlockController : NetworkBehaviour
    {
        public BlockControllerRecord RecordFrame()
        {
            var t = transform;
            return new BlockControllerRecord
            {
                Position = t.position,
                Rotation = t.rotation
            };
        }

        public void Snap(Vector3 p, Quaternion r)
        {
            if (isClient)
                Drag(p, r);
        }

        [Client]
        public void Drag(Vector3 p, Quaternion r)
        {
            CmdDrag(p, r);
            var t = transform;
            t.position = p;
            t.rotation = r;
            t.Rotate(Vector3.up * 90);
        }

        [Command(ignoreAuthority = true)]
        private void CmdDrag(Vector3 p, Quaternion r)
        {
            var t = transform;
            t.position = p;
            t.rotation = r;
            t.Rotate(Vector3.up * 90);
            DragRpc(p, r);
        }

        [ClientRpc]
        private void DragRpc(Vector3 p, Quaternion r)
        {
            var t = transform;
            t.position = p;
            t.rotation = r;
            t.Rotate(Vector3.up * 90);
        }
    }
}