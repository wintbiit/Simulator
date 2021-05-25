using System;
using Mirror;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    public class JudgeController : NetworkBehaviour
    {
        private bool _isLocal;

        [Client]
        public void ConfirmLocal()
        {
            FindObjectOfType<GameManager>().LocalJudgeRegister();
            CmdConfirmLocal();
            _isLocal = true;
        }

        [Command(ignoreAuthority = true)]
        private void CmdConfirmLocal()
        {
            FindObjectOfType<GameManager>().confirmedCount++;
        }

        private void FixedUpdate()
        {
            if (_isLocal && Cursor.lockState == CursorLockMode.Locked)
            {
                foreach (var c in FindObjectsOfType<Camera>())
                    c.enabled = false;
                GetComponent<Camera>().enabled = true;
                var move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                move *= Input.GetKey(KeyCode.LeftShift) ? 0.2f : 0.03f;
                transform.Translate(move);
                var rot = new Vector3(Input.GetAxis("Mouse Y") * -1, Input.GetAxis("Mouse X"), 0);
                rot *= FindObjectOfType<GameManager>().GetSensitivity() * 5;
                transform.Rotate(rot);
                var x = transform.localEulerAngles.x;
                if (90 - Math.Abs(x) > 10)
                    transform.Rotate(Vector3.back * transform.rotation.eulerAngles.z);
            }
        }
    }
}