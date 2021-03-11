using System;
using System.Collections.Generic;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Script.Controller
{
    /*
     * 车辆轮组结构（仿照 WheelCollider 例程）
     */
    [Serializable]
    public class AxleInfo
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool motor;
    }

    /*
     * 地面车辆统一运动模型
     * + 前后驱动
     * + 水平驱动
     * + 刹车
     * + 方向旋转
     * + 定向旋转
     * + 小陀螺
     * + 防翻车
     * + 部分补给逻辑（需要替换）
     * + 射击（需要替换）
     * + 鼠标锁定（需要替换）
     * + 摄像机切换（需要替换）
     */
    public class GroundControllerBase : RobotBase, IVulnerable
    {
        // 车辆最大驱动扭矩、最大转向速度
        [Header("Motor")] public List<AxleInfo> axleInfos;
        public float oriMaxMotorTorque;
        private float _maxMotorTorque;
        public float maxSteeringSpeed;

        // 视角俯仰
        [Header("Pitch")] public GameObject pitch;
        public float maxPitchAngle;

        // 底盘旋转
        [Header("Spin")] public Transform chassis;
        public Transform spinner;

        // 射击
        [Header("Fire")] public Transform gun;
        public GameObject bullet;
        public float speed;
        public bool highFreq;

        [Header("Armor")] public ArmorController[] armors;

        // 特殊角度导航
        private float _targetRot;
        private bool _isNav;
        private bool _clockwise = true;

        // 多摄像机切换
        [Header("Camera")] public GameObject tpCam;
        public GameObject fpCam;
        private bool _fpActive = true;

        private const float Sensitivity = 1.0f;

        // 防翻车回溯
        private Vector3 _lastAngle;

        // 辅助变量
        private float _steeringSpeed;
        private float _pitchingSpeed;
        private int _braking;
        private int _antiCarCrash;
        private int _fireCd;
        private int _angleSpin;
        [SyncVar] private bool _isSpin;
        [SyncVar] private Quaternion _pitchRot;

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!isLocalRobot) return;
            if (other.name != (role.Camp == CampT.Red ? "RSZ" : "BSZ")) return;
            if (health==0)
                CmdRevive();
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

        private void ArmorSetup()
        {
            foreach (var armor in armors)
            {
                armor.UnitRegister(this);

                if (role.IsInfantry())
                {
                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (role.Type)
                    {
                        case TypeT.InfantryA:
                            armor.ChangeLabel(3);
                            break;
                        case TypeT.InfantryB:
                            armor.ChangeLabel(4);
                            break;
                        case TypeT.InfantryC:
                            armor.ChangeLabel(5);
                            break;
                    }
                }
                else
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (role.Type)
                    {
                        case TypeT.Hero:
                            armor.ChangeLabel(1);
                            break;
                        case TypeT.Engineer:
                            armor.ChangeLabel(2);
                            break;
                        default:
                            armor.ChangeLabel(0);
                            break;
                    }

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

        private void Start()
        {
            if (isLocalRobot)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _fpActive = true;
            }
            else
            {
                tpCam.SetActive(false);
                fpCam.SetActive(false);
            }

            ToggleMeshRenderer(chassis, true);
            ToggleMeshRenderer(spinner, false);
            _maxMotorTorque = oriMaxMotorTorque;
            _angleSpin = 0;
        }

        [Client]
        public override void ConfirmLocalRobot()
        {
            base.ConfirmLocalRobot();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            FindObjectOfType<GameManager>().LocalRobotRegister(this);
        }

        // 显示、隐藏模型
        private static void ToggleMeshRenderer(Transform obj, bool show)
        {
            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = show;
            }

            if (obj.childCount == 0) return;
            for (var i = 0; i < obj.childCount; i++)
            {
                var c = obj.GetChild(i);
                mr = c.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = show;
                }

                ToggleMeshRenderer(c, show);
            }
        }

        // 车轮旋转效果
        private static void ApplyLocalPositionToVisuals(WheelCollider wheelCollider)
        {
            if (wheelCollider.transform.childCount == 0)
            {
                return;
            }

            var visualWheel = wheelCollider.transform.GetChild(0);

            wheelCollider.GetWorldPose(out var position, out var rotation);

            visualWheel.position = position;
            visualWheel.rotation = rotation;
        }

        [Command(ignoreAuthority = true)]
        private void SetSpin(bool spin)
        {
            _isSpin = spin;
        }

        [Command(ignoreAuthority = true)]
        private void SyncPitch(Quaternion rot)
        {
            SyncPitchRpc(rot);
        }

        [Command(ignoreAuthority = true)]
        private void CmdFire()
        {
            FireRpc();
        }

        [Command(ignoreAuthority = true)]
        private void CmdDrag(Vector3 position)
        {
            RpcDrag(position);
        }

        [Command(ignoreAuthority = true)]
        private void CmdRevive()
        {
            if (health == 0)
            {
                health = RobotPerformanceTable.table[level][role.Type].HealthLimit;
            }
        }

        [ClientRpc]
        private void RpcDrag(Vector3 position)
        {
            if (isLocalRobot)
                transform.position = position;
        }

        [ClientRpc]
        private void FireRpc()
        {
            if (isLocalRobot) return;
            var b = Instantiate(bullet, gun.position, gun.rotation);
            b.GetComponent<Rigidbody>().velocity = gun.forward * speed;
            Destroy(b, 4);
        }

        protected virtual bool FireOperation()
        {
            return false;
        }

        private void Fire()
        {
            var b = Instantiate(bullet, gun.position, gun.rotation);
            b.GetComponent<Rigidbody>().velocity = gun.forward * velocityLimit;
            var bulletController = b.GetComponent<BulletController>();
            bulletController.owner = id;
            bulletController.isActive = true;
            Destroy(b, 4);
            CmdFire();
        }

        public void Drag(Vector3 position)
        {
            CmdDrag(position);
        }

        [ClientRpc]
        private void SyncPitchRpc(Quaternion rot)
        {
            if (!isLocalRobot)
            {
                pitch.transform.rotation = rot;
            }
        }

        public override void OnStopClient()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public virtual void FixedUpdate()
        {
            ArmorSetup();

            if (isLocalRobot && health > 0)
            {
                // 车辆前后驱动
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var motor = _maxMotorTorque * Input.GetAxis("Vertical");

                    foreach (var axleInfo in axleInfos)
                    {
                        if (axleInfo.motor)
                        {
                            axleInfo.leftWheel.motorTorque = motor;
                            axleInfo.rightWheel.motorTorque = motor;
                            // 刹车阻尼效果
                            if (Input.GetKey(KeyCode.Space))
                            {
                                _braking++;
                                GetComponent<Rigidbody>().velocity /= 1.15f;
                                if (_braking > 30)
                                {
                                    GetComponent<Rigidbody>().velocity = Vector3.zero;
                                }
                            }
                            else
                            {
                                _braking = 0;
                            }
                        }

                        ApplyLocalPositionToVisuals(axleInfo.leftWheel);
                        ApplyLocalPositionToVisuals(axleInfo.rightWheel);
                    }
                }

                // 车辆旋转速度计算
                if (Input.GetAxis("Mouse X") != 0)
                {
                    _steeringSpeed = ((Input.GetAxis("Mouse X") * 1.8f * Sensitivity * 2) + _steeringSpeed) / 2;
                }

                if (Mathf.Abs(_steeringSpeed) > maxSteeringSpeed)
                {
                    _steeringSpeed = _steeringSpeed > 0 ? maxSteeringSpeed : -maxSteeringSpeed;
                }

                // 俯仰速度计算
                if (Input.GetAxis("Mouse Y") != 0)
                {
                    _pitchingSpeed = -((Input.GetAxis("Mouse Y") * 1.2f * Sensitivity * 2) + _pitchingSpeed) / 2;
                }

                var pitchA = pitch.transform.localEulerAngles.x;
                if (pitchA > 180) pitchA -= 360;
                if (pitchA > maxPitchAngle / 2 || pitchA < -maxPitchAngle)
                {
                    _pitchingSpeed = 0;
                    if (pitchA > 0) pitch.transform.Rotate(Vector3.right, -1);
                    if (pitchA < 0) pitch.transform.Rotate(Vector3.right, 1);
                }

                SyncPitch(pitch.transform.rotation);

                // UI切换、车辆旋转
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    transform.Rotate(Vector3.up, _steeringSpeed);
                    pitch.transform.Rotate(Vector3.right, _pitchingSpeed);
                }

                // 旋转阻尼效果
                _steeringSpeed *= 0.9f;
                _pitchingSpeed *= 0.9f;

                // 车辆左右平移
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    if (Math.Abs(_maxMotorTorque - oriMaxMotorTorque) < 1e-2)
                    {
                        transform.Translate(
                            Vector3.right * (Input.GetAxis("Horizontal") * _maxMotorTorque * 2)
                            / 10000);
                    }
                    else
                    {
                        transform.Translate(
                            Vector3.right * (Input.GetAxis("Horizontal") * _maxMotorTorque * 2)
                            / 20000);
                    }
                }

                // Boost效果
                if (Input.GetKey(KeyCode.LeftShift) && !_isSpin)
                {
                    _maxMotorTorque = oriMaxMotorTorque * 4;
                }
                else
                {
                    _maxMotorTorque = oriMaxMotorTorque;
                }

                // 射击
                if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(0))
                {
                    if (!FireOperation())
                    {
                        var caliber = bullet.GetComponent<BulletController>().caliber;
                        if (_fireCd == 0 && (caliber == CaliberT.Large ? largeAmmo > 0 : smallAmmo > 0))
                        {
                            if (caliber == CaliberT.Large)
                                largeAmmo--;
                            else
                                smallAmmo--;
                            Fire();
                            _fireCd = Random.Range(5, 15);
                            if (highFreq) _fireCd /= 2;
                        }
                        else
                        {
                            if (_fireCd > 0) _fireCd--;
                        }
                    }
                }

                // 射速切换
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.C))
                {
                    highFreq = false;
                }

                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.V))
                {
                    highFreq = true;
                }

                // 特殊角度导航
                if (Input.GetKey(KeyCode.Q) && !_isNav)
                {
                    _targetRot = 20;
                    _clockwise = false;
                    _isNav = true;
                }

                if (Input.GetKey(KeyCode.E) && !_isNav)
                {
                    _targetRot = 20;
                    _clockwise = true;
                    _isNav = true;
                }

                if (Input.GetKey(KeyCode.X) && !_isNav)
                {
                    _targetRot = 180;
                    _clockwise = true;
                    _isNav = true;
                }

                if (_isNav && _targetRot > 0)
                {
                    if (_clockwise) transform.Rotate(Vector3.up, 5);
                    else transform.Rotate(Vector3.up, -5);
                    _targetRot -= 5;
                }
                else
                {
                    _isNav = false;
                }

                // 切换摄像机
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    _fpActive = !_fpActive;
                }

                tpCam.SetActive(!_fpActive);
                fpCam.SetActive(_fpActive);
                // hud.SetActive(_fpActive);

                // 重置车辆位置、旋转
                if (Input.GetKeyDown(KeyCode.R))
                {
                    var selfTransform = transform;
                    selfTransform.position += Vector3.up;
                    selfTransform.rotation = new Quaternion();
                }

                // 解锁鼠标
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (Cursor.lockState == CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
            }

            // Spin效果
            if (_angleSpin == 0 && _isSpin) _angleSpin = 1;
            if (_angleSpin > 0 && _angleSpin < 37)
            {
                ToggleMeshRenderer(chassis, false);
                ToggleMeshRenderer(spinner, true);
                spinner.Rotate(Vector3.up, 10);
                _angleSpin++;
                if (_angleSpin == 37)
                {
                    if (!_isSpin)
                    {
                        ToggleMeshRenderer(chassis, true);
                        ToggleMeshRenderer(spinner, false);
                        _angleSpin = 0;
                    }
                    else
                    {
                        _angleSpin = 1;
                    }
                }
            }

            if (Cursor.lockState == CursorLockMode.Locked && isLocalRobot)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    SetSpin(true);
                    _maxMotorTorque = oriMaxMotorTorque / 2;
                }

                if (Input.GetMouseButtonUp(1))
                {
                    SetSpin(false);
                    _maxMotorTorque = oriMaxMotorTorque;
                }
            }

            // 防翻车
            if (Mathf.Abs(this.transform.rotation.eulerAngles.x - 180) <= 140
                || Mathf.Abs(this.transform.rotation.eulerAngles.z - 180) <= 140)
            {
                if (_antiCarCrash == 0)
                {
                    this.transform.rotation = Quaternion.Euler(_lastAngle);
                    this.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                }

                _antiCarCrash++;
                if (_antiCarCrash == 30)
                {
                    _antiCarCrash = 0;
                }
            }
            else
            {
                _lastAngle = this.transform.rotation.eulerAngles;
            }
        }
    }
}