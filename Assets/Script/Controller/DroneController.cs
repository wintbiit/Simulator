using System;
using System.Linq;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem.Event;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using TypeT = Script.JudgeSystem.Event.TypeT;

namespace Script.Controller
{
    public class AirRaidEvent : GameEventBase
    {
        public readonly CampT Camp;

        public AirRaidEvent(CampT c)
        {
            Type = TypeT.AirRaid;
            Camp = c;
        }
    }

    public class DartEvent : GameEventBase
    {
        public readonly CampT Camp;

        public DartEvent(CampT c)
        {
            Type = TypeT.Dart;
            Camp = c;
        }
    }

    public class DroneControllerRecord : RobotBaseRecord
    {
        public float RaidTill;
        public bool Raiding;
        public float DartTill;
        public int DartCount;
    }

    public class DroneController : RobotBase
    {
        public GameObject cam;
        public GameObject yaw;
        public GameObject pitch;

        public float maxSteeringSpeed;
        public float maxPitchAngle;

        public float moveSpeed;
        [SyncVar] public float raidTill;
        [SyncVar] public bool raiding;
        [FormerlySerializedAs("_raidStart")] public float raidStart = -1;

        [Header("Fire")] public GameObject bullet;
        public Transform gun;
        public bool highFreq;

        private float _steeringSpeed;
        private float _pitchingSpeed;

        private int _fireCd;

        // 辅助瞄准
        private ArmorController _lastTarget;
        private Vector3 _lastPosition;
        private Vector3 _prediction;
        private float _lastPredictTime;
        private float _predictInterval;
        private float _flightTime;
        private LineRenderer _visual;
        private GameObject _target;

        [SyncVar] public float dartTill;
        [SyncVar] public int dartCount;

        public bool isPtz;

        private float _xOffset;
        private float _yOffset;
        private float _zOffset;

        [Server]
        public DroneControllerRecord RecordFrame()
        {
            var record = new DroneControllerRecord
            {
                RaidTill = raidTill,
                Raiding = raiding,
                DartTill = dartTill,
                DartCount = dartCount
            };
            base.RecordFrame(record);
            return record;
        }

        [Command(requiresAuthority = false)]
        private void CmdFire(float realSpeed)
        {
            FireRpc(realSpeed);
        }

        [ClientRpc]
        private void FireRpc(float realSpeed)
        {
            if (isLocalRobot && isPtz) return;
            var b = Instantiate(bullet, gun.position, gun.rotation);
            b.GetComponent<Rigidbody>().velocity = gun.forward * realSpeed;
        }

        private void Fire()
        {
            var b = Instantiate(bullet, gun.position, gun.rotation);
            var realSpeed = RobotPerformanceTable.Table[level][role.Type][chassisType][gunType]
                .VelocityLimit * Random.Range(0.9f, 1.1f);
            b.GetComponent<Rigidbody>().velocity = gun.forward * realSpeed;
            var bulletController = b.GetComponent<BulletController>();
            bulletController.owner = id;
            bulletController.isActive = true;
            CmdFire(realSpeed);
        }

        private bool IsGameObjectInCameraView(GameObject targetObj)
        {
            var fpCamera = cam.GetComponent<Camera>();
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

        public override void ConfirmLocalRobot()
        {
            base.ConfirmLocalRobot();
            FindObjectOfType<GameManager>().LocalRobotRegister(this);
            _visual = GameObject.Find("Prediction").GetComponent<LineRenderer>();
            dartTill = Time.time + 60;
        }

        [Command(requiresAuthority = false)]
        private void CmdSyncPtz(Quaternion y, Quaternion p)
        {
            yaw.transform.rotation = y;
            pitch.transform.rotation = p;
            SyncPtzRpc(y, p);
        }

        [ClientRpc]
        private void SyncPtzRpc(Quaternion y, Quaternion p)
        {
            if (isLocalRobot && isPtz) return;
            yaw.transform.rotation = y;
            pitch.transform.rotation = p;
        }

        [Command(requiresAuthority = false)]
        private void CmdAirRaid()
        {
            if (Time.time > raidTill)
                gameManager.Emit(new AirRaidEvent(role.Camp));
        }

        [Command(requiresAuthority = false)]
        private void CmdDart()
        {
            gameManager.Emit(new DartEvent(role.Camp));
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isServer)
            {
                raiding = Time.time < raidTill;
                if (!raiding) smallAmmo = 0;
            }


            cam.SetActive(isLocalRobot);
            if (isLocalRobot)
            {
                foreach (var c in FindObjectsOfType<Camera>())
                    c.enabled = false;
                foreach (var a in FindObjectsOfType<AudioListener>())
                    a.enabled = false;
                cam.GetComponent<Camera>().enabled = true;
                cam.GetComponent<AudioListener>().enabled = true;
                cam.SetActive(isPtz);
                var dCam = (role.Camp == CampT.Red
                    ? FindObjectOfType<GameManager>().redDCam
                    : FindObjectOfType<GameManager>().blueDCam);
                dCam.SetActive(!isPtz);
                dCam.GetComponent<Camera>().enabled = !isPtz;
                dCam.GetComponent<AudioListener>().enabled = !isPtz;
                if (!isPtz)
                {
                    if (raiding)
                    {
                        if (raidStart < 0)
                            raidStart = Time.time;
                    }
                    else
                    {
                        raidStart = -1;
                    }
                }

                if (health > 0)
                {
                    if (!isPtz)
                    {
                        var gm = FindObjectOfType<GameManager>();
                        if (gm)
                        {
                            var canMove = gm.globalStatus.countDown <= 420;
                            if (canMove)
                            {
                                if (Input.GetKey(KeyCode.Space))
                                {
                                    if (_yOffset < 2)
                                    {
                                        transform.Translate(Vector3.up * moveSpeed);
                                        _yOffset += moveSpeed;
                                    }
                                }

                                if (Input.GetKey(KeyCode.LeftShift))
                                {
                                    if (_yOffset > 0)
                                    {
                                        transform.Translate(Vector3.down * moveSpeed);
                                        _yOffset -= moveSpeed;
                                    }
                                }

                                if (Input.GetKey(KeyCode.W))
                                {
                                    if (_zOffset < 9)
                                    {
                                        transform.Translate(Vector3.forward * moveSpeed);
                                        _zOffset += moveSpeed;
                                    }
                                }

                                if (Input.GetKey(KeyCode.A))
                                {
                                    if (_xOffset > -0.5)
                                    {
                                        transform.Translate(Vector3.left * moveSpeed);
                                        _xOffset -= moveSpeed;
                                    }
                                }

                                if (Input.GetKey(KeyCode.S))
                                {
                                    if (_zOffset > -0.5)
                                    {
                                        transform.Translate(Vector3.back * moveSpeed);
                                        _zOffset -= moveSpeed;
                                    }
                                }

                                if (Input.GetKey(KeyCode.D))
                                {
                                    if (_xOffset < 3)
                                    {
                                        transform.Translate(Vector3.right * moveSpeed);
                                        _xOffset += moveSpeed;
                                    }
                                }
                            }
                        }
                    }

                    if (isPtz)
                    {
                        if (Input.GetKeyDown(KeyCode.H))
                        {
                            CmdAirRaid();
                        }

                        if (Input.GetKeyDown(KeyCode.Y) && Time.time > dartTill)
                        {
                            CmdDart();
                            dartTill = Time.time + 60;
                            dartCount++;
                        }

                        if (Cursor.lockState == CursorLockMode.Locked)
                        {
                            var sensitivity = FindObjectOfType<GameManager>().GetSensitivity();
                            // 车辆旋转速度计算
                            if (Input.GetAxis("Mouse X") != 0)
                            {
                                _steeringSpeed =
                                    ((Input.GetAxis("Mouse X") * 1.8f * sensitivity * 2) + _steeringSpeed) / 2;
                            }

                            if (Mathf.Abs(_steeringSpeed) > maxSteeringSpeed)
                            {
                                _steeringSpeed = _steeringSpeed > 0 ? maxSteeringSpeed : -maxSteeringSpeed;
                            }

                            // 俯仰速度计算
                            if (Input.GetAxis("Mouse Y") != 0)
                            {
                                _pitchingSpeed =
                                    -((Input.GetAxis("Mouse Y") * 1.2f * sensitivity * 2) + _pitchingSpeed) /
                                    2;
                            }
                        }

                        var pitchA = pitch.transform.localEulerAngles.x;
                        if (pitchA > 180) pitchA -= 360;
                        if (pitchA > maxPitchAngle / 2 || pitchA < -maxPitchAngle)
                        {
                            _pitchingSpeed = 0;
                            if (pitchA > 0) pitch.transform.Rotate(Vector3.right, -1);
                            if (pitchA < 0) pitch.transform.Rotate(Vector3.right, 1);
                        }

                        yaw.transform.Rotate(Vector3.up, _steeringSpeed);
                        pitch.transform.Rotate(Vector3.left, _pitchingSpeed);

                        CmdSyncPtz(yaw.transform.rotation, pitch.transform.rotation);

                        // 射击
                        if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(0))
                        {
                            var caliber = bullet.GetComponent<BulletController>().caliber;
                            if (_fireCd == 0 && (caliber == CaliberT.Large ? largeAmmo > 0 : smallAmmo > 0) && raiding)
                            {
                                if (caliber == CaliberT.Large)
                                {
                                    largeAmmo--;
                                    heat += 100;
                                }
                                else
                                {
                                    smallAmmo--;
                                    heat += 10;
                                }

                                Fire();
                                _fireCd = Random.Range(2, 5);
                                if (highFreq) _fireCd /= 2;
                            }
                            else
                            {
                                if (_fireCd > 0) _fireCd--;
                            }
                        }

                        if (heat > 0)
                            heat -= RobotPerformanceTable.Table[level][role.Type][chassisType][gunType].CoolDownRate *
                                    GetAttr().ColdDownRate *
                                    (Time.fixedDeltaTime / 1.0f);

                        // 射速切换
                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.C))
                        {
                            highFreq = false;
                        }

                        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.V))
                        {
                            highFreq = true;
                        }

                        if (Input.GetMouseButton(1))
                        {
                            ArmorController target = null;
                            var fpCamera = cam.GetComponent<Camera>();
                            if (!_target)
                            {
                                var targets = FindObjectsOfType<ArmorController>()
                                    .Where(a => a.GetColor() != (role.Camp == CampT.Red ? ColorT.Red : ColorT.Blue) &&
                                                a.GetColor() != ColorT.Down)
                                    .Where(a => IsGameObjectInCameraView(a.gameObject));
                                var minDistance = float.MaxValue;
                                foreach (var t in targets)
                                {
                                    var sp = fpCamera.WorldToScreenPoint(t.transform.position);
                                    sp -= new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
                                    var distance = sp.sqrMagnitude;
                                    if (!(distance < minDistance)) continue;
                                    minDistance = distance;
                                    target = t;
                                    _target = t.gameObject;
                                }
                            }
                            else
                            {
                                target = _target.GetComponent<ArmorController>();
                                if (target.GetColor() == ColorT.Down)
                                {
                                    _target = null;
                                    target = null;
                                }
                            }

                            if (target != null)
                            {
                                var position = cam.transform.position;
                                var targetPosition = target.transform.position;
                                if (target == _lastTarget)
                                {
                                    if (_prediction != Vector3.zero)
                                    {
                                        targetPosition += _prediction * _flightTime / _predictInterval;
                                        targetPosition += Vector3.up * 0.1f;
                                    }
                                }

                                // 辅助瞄准算法
                                var distance = (targetPosition - position).magnitude;
                                var alpha = Mathf.Asin((targetPosition.y - position.y) / distance);
                                var theta = alpha;
                                const float g = -9.8f;
                                for (var i = 0; i < 100; i++)
                                {
                                    var vX0 = Mathf.Cos(theta) *
                                              RobotPerformanceTable.Table[level][role.Type][chassisType][gunType]
                                                  .VelocityLimit;
                                    var vY0 = Mathf.Sin(theta) *
                                              RobotPerformanceTable.Table[level][role.Type][chassisType][gunType]
                                                  .VelocityLimit;
                                    var t = Mathf.Cos(alpha) * distance / vX0;
                                    _flightTime = t;
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
                                    if (target == _lastTarget) _prediction = target.transform.position - _lastPosition;
                                    _lastTarget = target;
                                    _lastPosition = target.transform.position;
                                }

                                var delta = fpCamera.WorldToScreenPoint(vTargetPos);
                                delta -= new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
                                // var screenErr = delta;
                                delta *= 10;
                                delta.y /= Screen.height;
                                delta.x /= Screen.width;
                                // var noise = Random.Range(-0.16f, 0.16f);
                                // delta += new Vector3(noise, noise, 0);
                                _pitchingSpeed -= 1.0f / (1 + Mathf.Pow((float) Math.E, -delta.y)) - 0.5f;
                                _steeringSpeed += 1.0f / (1 + Mathf.Pow((float) Math.E, -delta.x)) - 0.5f;

                                // {
                                //     var vY0 = Mathf.Sin(theta) *
                                //               RobotPerformanceTable.Table[level][role.Type][chassisType][gunType]
                                //                   .VelocityLimit;
                                //     var xDir = new Vector3(targetPosition.x, 0, targetPosition.z) -
                                //                new Vector3(position.x, 0, position.z);
                                //     var points = new List<Vector3>();
                                //     for (float t = 0; t < _flightTime; t += 0.02f)
                                //     {
                                //         var sY = vY0 * t + 0.5f * g * Mathf.Pow(t, 2);
                                //         var point = position + xDir * (t / _flightTime) + Vector3.up * sY +
                                //                     Vector3.up * 0.05f;
                                //         points.Add(point);
                                //     }
                                //
                                //     _visual.positionCount = points.Count;
                                //     _visual.SetPositions(points.ToArray());
                                //     var gradient = _visual.colorGradient;
                                //     var colorKeys = gradient.colorKeys;
                                //     colorKeys[0].color = screenErr.magnitude > 25 ? Color.red : Color.green;
                                //     gradient.colorKeys = colorKeys;
                                //     _visual.colorGradient = gradient;
                                // }
                                _visual.positionCount = 0;
                                Debug.DrawRay(position, vTargetPos + _prediction - position, Color.magenta);
                                Debug.DrawRay(position, targetPosition - position, Color.red);
                                Debug.DrawRay(position, vTargetPos - position, Color.yellow);
                                Debug.DrawRay(vTargetPos, targetPosition - vTargetPos, Color.yellow);
                                Debug.DrawRay(position, targetPosition - position, Color.yellow);
                                Debug.DrawRay(vTargetPos, _prediction, Color.magenta);
                                Debug.DrawRay(vTargetPos + _prediction, targetPosition - vTargetPos - _prediction,
                                    Color.magenta);
                            }
                            else
                            {
                                _lastTarget = null;
                                _prediction = Vector3.zero;
                            }
                        }
                        else
                        {
                            _visual.positionCount = 0;
                            _target = null;
                        }
                    }

                    _pitchingSpeed *= 0.9f;
                    _steeringSpeed *= 0.9f;
                }
            }
        }
    }
}