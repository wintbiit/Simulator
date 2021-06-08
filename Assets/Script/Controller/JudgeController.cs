using System.Linq;
using Mirror;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;

namespace Script.Controller
{
    public class JudgeController : NetworkBehaviour
    {
        private bool _isLocal;
        private GroundControllerBase _observing;

        [Client]
        public void ConfirmLocal()
        {
            FindObjectOfType<GameManager>().LocalJudgeRegister();
            CmdConfirmLocal();
            _isLocal = true;
        }

        [Command(requiresAuthority = false)]
        private void CmdConfirmLocal()
        {
            FindObjectOfType<GameManager>().confirmedCount++;
        }

        private void FixedUpdate()
        {
            if (_isLocal)
            {
                if (!_observing)
                {
                    foreach (var c in FindObjectsOfType<Camera>())
                        c.enabled = false;
                    foreach (var a in FindObjectsOfType<AudioListener>())
                        a.enabled = false;
                    GetComponent<Camera>().enabled = true;
                    GetComponent<AudioListener>().enabled = true;
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        var move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                        move *= Input.GetKey(KeyCode.LeftShift) ? 0.2f : 0.03f;
                        var t = transform;
                        transform.Translate(move);
                        var rot = new Vector3(Input.GetAxis("Mouse Y") * -1, Input.GetAxis("Mouse X"), 0);
                        rot *= FindObjectOfType<GameManager>().GetSensitivity() * 4;
                        transform.Rotate(rot);
                        transform.Rotate(Vector3.back * t.rotation.eulerAngles.z);
                        var up = Vector3.zero;
                        if (Input.GetKey(KeyCode.Space)) up += Vector3.up * 0.05f;
                        if (Input.GetKey(KeyCode.LeftControl)) up += Vector3.down * 0.05f;
                        if (Input.GetKey(KeyCode.LeftShift)) up *= 4;
                        transform.position += up;
                    }
                }
                else
                {
                    GetComponent<Camera>().enabled = false;
                    GetComponent<AudioListener>().enabled = false;
                }
            }
            else
            {
                GetComponent<Camera>().enabled = false;
                GetComponent<AudioListener>().enabled = false;
            }
        }

        private void Update()
        {
            if (_isLocal && Cursor.lockState == CursorLockMode.Locked)
            {
                if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.P))
                {
                    FindObjectOfType<GameManager>().CmdPunish(CampT.Red, 3);
                    Debug.Log("红方判罚，白屏3秒。");
                }

                if (Input.GetKeyDown(KeyCode.B) && Input.GetKey(KeyCode.P))
                {
                    FindObjectOfType<GameManager>().CmdPunish(CampT.Blue, 3);
                    Debug.Log("蓝方判罚，白屏3秒。");
                }

                if (!_observing)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchObservation(new RoleT(CampT.Red, TypeT.Hero));
                    if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchObservation(new RoleT(CampT.Red, TypeT.Engineer));
                    if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchObservation(new RoleT(CampT.Red, TypeT.InfantryA));
                    if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchObservation(new RoleT(CampT.Red, TypeT.InfantryB));
                    if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchObservation(new RoleT(CampT.Red, TypeT.InfantryC));
                    if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchObservation(new RoleT(CampT.Blue, TypeT.Hero));
                    if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchObservation(new RoleT(CampT.Blue, TypeT.Engineer));
                    if (Input.GetKeyDown(KeyCode.Alpha8)) SwitchObservation(new RoleT(CampT.Blue, TypeT.InfantryA));
                    if (Input.GetKeyDown(KeyCode.Alpha9)) SwitchObservation(new RoleT(CampT.Blue, TypeT.InfantryB));
                    if (Input.GetKeyDown(KeyCode.Alpha0)) SwitchObservation(new RoleT(CampT.Blue, TypeT.InfantryC));
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Backspace)) SwitchObservation(new RoleT());
                }
            }
        }

        private void SwitchObservation(RoleT role)
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm)
            {
                if (!_observing)
                {
                    if (gm.clientRobotBases.Any(r => r.role.Equals(role)))
                    {
                        var robot = gm.clientRobotBases.First(r => r.role.Equals(role));
                        if (robot is GroundControllerBase)
                        {
                            _observing = (GroundControllerBase) robot;
                            _observing.isLocalRobot = true;
                            FindObjectOfType<GameManager>().observing = _observing;
                        }
                    }
                }
                else
                {
                    _observing.isLocalRobot = false;
                    _observing = null;
                    FindObjectOfType<GameManager>().observing = null;
                }
            }
        }
    }
}