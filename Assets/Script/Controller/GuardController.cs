using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.Controller
{
    [Serializable]
    public class GuardPtz
    {
        public GameObject yaw;
        public GameObject pitch;
        public GameObject gun;
        public Camera aim;
    }

    public class GuardController : RobotBase, IVulnerable
    {
        [Header("Fire")] public float speed;
        private int _fireCd;

        public List<ArmorController> armors = new List<ArmorController>();
        public List<GuardPtz> ptz = new List<GuardPtz>();
        public GameObject bullet;

        private const float XMax = 1.642f;
        private const float XMin = -1.642f;
        private bool _left = true;
        private float _pitchingSpeed;
        private float _steeringSpeed;

        // 辅助瞄准
        private ArmorController _lastTarget;
        private Vector3 _lastPosition;
        private Vector3 _prediction;
        private float _lastPredictTime;
        private float _predictInterval;

        private void ArmorSetup()
        {
            foreach (var armor in armors)
            {
                armor.UnitRegister(this);
                armor.ChangeLabel(0);
                if (health <= 0)
                {
                    armor.ChangeColor(ColorT.Down);
                }
                else
                    switch (role.Camp)
                    {
                        case CampT.Unknown:
                            armor.ChangeColor(ColorT.Down);
                            break;
                        case CampT.Red:
                            armor.ChangeColor(ColorT.Red);
                            break;
                        case CampT.Blue:
                            armor.ChangeColor(ColorT.Blue);
                            break;
                        case CampT.Judge:
                            armor.ChangeColor(ColorT.Down);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
            }
        }

        [Server]
        private void SyncPtz(int index, Quaternion yaw, Quaternion pitch)
        {
            SyncPtzRpc(index, yaw, pitch);
        }

        [ClientRpc]
        private void SyncPtzRpc(int index, Quaternion yaw, Quaternion pitch)
        {
            ptz[index].yaw.transform.rotation = yaw;
            ptz[index].pitch.transform.rotation = pitch;
        }

        private void Start()
        {
            ptz[0].yaw.transform.Rotate(Vector3.up, 180);
        }

        protected override void FixedUpdate()
        {
            ArmorSetup();
            if (!isServer) return;
            if (health <= 0) return;

            foreach (var head in ptz)
            {
                if (head.gun)
                {
                    var targets = FindObjectsOfType<ArmorController>()
                        .Where(a => a.GetColor() != this.armors[0].GetColor() && a.GetColor() != ColorT.Down)
                        .Where(a => IsGameObjectInCameraView(head.aim, a.gameObject));
                    var minDistance = float.MaxValue;
                    var fpCam = head.aim;
                    ArmorController target = null;
                    var fpCamera = fpCam.GetComponent<Camera>();
                    foreach (var t in targets)
                    {
                        var sp = fpCamera.WorldToScreenPoint(t.transform.position);
                        sp -= new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
                        var distance = sp.sqrMagnitude;
                        if (!(distance < minDistance)) continue;
                        if ((t.transform.position - fpCam.transform.position).magnitude > 6.5f) continue;
                        minDistance = distance;
                        target = t;
                    }

                    if (target != null)
                    {
                        var position = fpCam.transform.position;
                        var targetPosition = target.transform.position;
                        // 辅助瞄准算法
                        Debug.DrawRay(position, targetPosition - position, Color.red);
                        var distance = (targetPosition - position).magnitude;
                        var alpha = Mathf.Asin((targetPosition.y - position.y) / distance);
                        var theta = alpha;
                        var flightTime = 0.0f;
                        const float g = -9.8f;
                        for (var i = 0; i < 100; i++)
                        {
                            var vX0 = Mathf.Cos(theta) * speed;
                            var vY0 = Mathf.Sin(theta) * speed;
                            var t = Mathf.Cos(alpha) * distance / vX0;
                            flightTime = t;
                            var sY = vY0 * t + 0.5f * g * Mathf.Pow(t, 2);
                            var err = Mathf.Sin(alpha) * distance - sY;
                            if (err < 1e-3) break;
                            var adjust = 1.0f / (1 + Mathf.Pow((float) Math.E, -err)) - 0.5f;
                            theta += 0.025f * (float) Math.PI * adjust;
                        }

                        var tY = Mathf.Tan(theta) * Mathf.Cos(alpha) * distance + position.y;
                        var vTargetPos = new Vector3(targetPosition.x, tY, targetPosition.z);
                        if (Time.time - _lastPredictTime > 0.2)
                        {
                            _predictInterval = Time.time - _lastPredictTime;
                            _lastPredictTime = Time.time;
                            if (target == _lastTarget)
                            {
                                _prediction = targetPosition - _lastPosition;
                                _prediction += Vector3.right * (flightTime * (0.02f * (_left ? 1 : -1)));
                            }

                            _lastTarget = target;
                            _lastPosition = targetPosition;
                        }

                        Debug.DrawRay(position, vTargetPos - position, Color.yellow);
                        Debug.DrawRay(vTargetPos, targetPosition - vTargetPos, Color.yellow);
                        Debug.DrawRay(position, targetPosition - position, Color.yellow);
                        Debug.DrawRay(vTargetPos, _prediction, Color.magenta);
                        Debug.DrawRay(vTargetPos + _prediction, targetPosition - vTargetPos - _prediction,
                            Color.magenta);
                        Debug.DrawRay(position, vTargetPos + _prediction - position, Color.magenta);

                        var delta = fpCamera.WorldToScreenPoint(
                            vTargetPos + _prediction * flightTime / _predictInterval);
                        delta -= new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
                        delta *= 10;
                        delta.y /= Screen.height;
                        delta.x /= Screen.width;
                        var noise = Random.Range(-0.3f, 0.3f);
                        delta += new Vector3(noise, noise, 0);
                        _pitchingSpeed -= 1.0f / (1 + Mathf.Pow((float) Math.E, -delta.y)) - 0.5f;
                        _steeringSpeed += 1.0f / (1 + Mathf.Pow((float) Math.E, -delta.x)) - 0.5f;

                        if (_fireCd <= 0 && smallAmmo > 0)
                        {
                            smallAmmo--;
                            Fire(0);
                            _fireCd = Random.Range(8, 16);
                        }
                        else
                        {
                            _fireCd--;
                        }
                    }
                    else
                    {
                        var pitchAngle = head.pitch.transform.localRotation.z;
                        var yawAngle = head.yaw.transform.localRotation.y;
                        _pitchingSpeed = pitchAngle > 0 ? 5 : -5;
                        _steeringSpeed = yawAngle > -180 ? -5 : 5;
                    }

                    // 旋转阻尼效果
                    _steeringSpeed *= 0.9f;
                    _pitchingSpeed *= 0.9f;

                    head.yaw.transform.Rotate(Vector3.up, _steeringSpeed);
                    head.pitch.transform.Rotate(Vector3.forward, _pitchingSpeed);
                    SyncPtz(0, head.yaw.transform.rotation, head.pitch.transform.rotation);
                }
            }

            if (transform.position.x > XMax)
                _left = true;
            if (transform.position.x < XMin)
                _left = false;
            transform.localPosition += Vector3.right * ((_left ? -1 : 1) * 0.02f);
        }

        public void Hit(int hitter, CaliberT caliber)
        {
            CmdHit(hitter, caliber);
        }

        [Command(ignoreAuthority = true)]
        private void CmdHit(int hitter, CaliberT caliber)
        {
            gameManager.Emit(new HitEvent(hitter, id, caliber));
        }

        private bool IsGameObjectInCameraView(Camera aim, GameObject targetObj)
        {
            var fpCamera = aim;
            var targetObjViewportCoord = fpCamera.WorldToViewportPoint(targetObj.transform.position);
            if (!(targetObjViewportCoord.x > 0) || !(targetObjViewportCoord.x < 1) ||
                !(targetObjViewportCoord.y > 0) || !(targetObjViewportCoord.y < 1) ||
                !(targetObjViewportCoord.z > fpCamera.nearClipPlane) ||
                !(targetObjViewportCoord.z < fpCamera.farClipPlane)) return false;
            var position = fpCamera.transform.position;
            var ray = new Ray(position, targetObj.transform.position - position);
            if (!Physics.Raycast(ray, out var hit, float.MaxValue, 1 << LayerMask.NameToLayer("Default"),
                QueryTriggerInteraction.Ignore)) return false;
            // Debug.DrawRay(position, targetObj.transform.position - position, Color.blue);
            var o = hit.transform.gameObject;
            return o == targetObj || o.GetComponent<BulletController>() != null;
        }

        private void Fire(int index)
        {
            var gun = ptz[index].gun.transform;
            var b = Instantiate(bullet, gun.position, gun.rotation);
            b.GetComponent<Rigidbody>().velocity = gun.forward * speed;
            var bulletController = b.GetComponent<BulletController>();
            bulletController.owner = id;
            bulletController.isActive = true;
            // Destroy(b, 4);
            FireRpc(index);
        }

        [ClientRpc]
        private void FireRpc(int index)
        {
            var gun = ptz[index].gun.transform;
            var b = Instantiate(bullet, gun.position, gun.rotation);
            b.GetComponent<Rigidbody>().velocity = gun.forward * speed;
            // Destroy(b, 4);
        }
    }
}