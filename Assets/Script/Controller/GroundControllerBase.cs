using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.Controller.Armor;
using Script.Controller.Bullet;
using Script.JudgeSystem;
using Script.JudgeSystem.Facility;
using Script.JudgeSystem.GameEvent;
using Script.JudgeSystem.Robot;
using Script.JudgeSystem.Role;
using Script.Networking.Game;
using UnityEngine;
using Random = UnityEngine.Random;
using TypeT = Script.JudgeSystem.Role.TypeT;

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

    public class BaseBuff : BuffBase
    {
        public BaseBuff()
        {
            type = BuffT.Base;
            armorRate = 0.5f;
            coolDownRate = 3;
            timeOut = float.MaxValue;
        }
    }

    public class VcdBuff : BuffBase
    {
        public VcdBuff()
        {
            type = BuffT.Vcd;
            coolDownRate = 5;
            timeOut = float.MaxValue;
        }
    }

    public class ActivatorBuff : BuffBase
    {
        public ActivatorBuff()
        {
            type = BuffT.Activator;
        }
    }

    public class ReviveProtectBuff : BuffBase
    {
        public ReviveProtectBuff(float time)
        {
            type = BuffT.ReviveProtect;
            armorRate = 1;
            timeOut = Time.time + time;
        }
    }

    public class ReviveBuff : BuffBase
    {
        public ReviveBuff()
        {
            type = BuffT.Revive;
            reviveRate = 0.05f;
            timeOut = float.MaxValue;
        }
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
        public bool safe = true;

        [Header("Armor")] public ArmorController[] armors;

        // 特殊角度导航
        private float _targetRot;
        private bool _isNav;
        private bool _clockwise = true;

        // 多摄像机切换
        [Header("Camera")] public GameObject tpCam;
        public GameObject fpCam;
        private bool _fpActive = true;

        // 防翻车回溯
        private Vector3 _lastAngle;

        // 辅助瞄准
        private ArmorController _lastTarget;
        private Vector3 _lastPosition;
        private Vector3 _prediction;
        private float _lastPredictTime;
        private float _predictInterval;
        private float _flightTime;
        private LineRenderer _visual;

        // 辅助变量
        private float _steeringSpeed;
        private float _pitchingSpeed;
        private int _braking;
        private int _antiCarCrash;
        private int _fireCd;
        private int _angleSpin;
        [SyncVar] private bool _isSpin;
        [SyncVar] private Quaternion _pitchRot;
        private bool _climbing;

        protected virtual void OnTriggerStay(Collider other)
        {
            if (!isLocalRobot) return;
            if (other.name == "Waves")
            {
                if (_isSpin && health > 0)
                {
                    if (Random.Range(0, 5) > 3)
                        transform.Translate(Vector3.up * Random.Range(0, 0.1f));
                }
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;
            switch (other.name)
            {
                case "RSZ":
                    if (role.Camp == CampT.Red)
                        if (Buffs.All(b => b.type != BuffT.Revive))
                            Buffs.Add(new ReviveBuff());
                    break;
                case "BSZ":
                    if (role.Camp == CampT.Blue)
                        if (Buffs.All(b => b.type != BuffT.Revive))
                            Buffs.Add(new ReviveBuff());
                    break;
                case "BBase":
                    if (role.Camp == CampT.Blue)
                        if (Buffs.All(b => b.type != BuffT.Base))
                            Buffs.Add(new BaseBuff());
                    break;
                case "RBase":
                    if (role.Camp == CampT.Red)
                        if (Buffs.All(b => b.type != BuffT.Base))
                            Buffs.Add(new BaseBuff());
                    break;
                case "BBunker":
                    if (role.Camp == CampT.Blue)
                        if (Buffs.All(b => b.type != BuffT.Vcd))
                            Buffs.Add(new VcdBuff());
                    break;
                case "RBunker":
                    if (role.Camp == CampT.Red)
                        if (Buffs.All(b => b.type != BuffT.Vcd))
                            Buffs.Add(new VcdBuff());
                    break;
                case "BOP":
                    if (role.Camp == CampT.Blue)
                        if (Buffs.All(b => b.type != BuffT.Vcd) && FindObjectsOfType<FacilityBase>()
                            .First(f => f.role.Equals(new RoleT(CampT.Blue, TypeT.Outpost))).health > 0)
                            Buffs.Add(new VcdBuff());
                    break;
                case "ROP":
                    if (role.Camp == CampT.Red)
                        if (Buffs.All(b => b.type != BuffT.Vcd) && FindObjectsOfType<FacilityBase>()
                            .First(f => f.role.Equals(new RoleT(CampT.Red, TypeT.Outpost))).health > 0)
                            Buffs.Add(new VcdBuff());
                    break;
                case "BSP":
                    if (role.Camp == CampT.Blue)
                    {
                        if (Buffs.All(b => b.type != BuffT.Vcd))
                            Buffs.Add(new VcdBuff());
                        if (Buffs.All(b => b.type != BuffT.Activator))
                            Buffs.Add(new ActivatorBuff());
                    }

                    break;
                case "RSP":
                    if (role.Camp == CampT.Red)
                    {
                        if (Buffs.All(b => b.type != BuffT.Vcd))
                            Buffs.Add(new VcdBuff());
                        if (Buffs.All(b => b.type != BuffT.Activator))
                            Buffs.Add(new ActivatorBuff());
                    }

                    break;
                case "High":
                    if (Buffs.All(b => b.type != BuffT.Vcd))
                        Buffs.Add(new VcdBuff());
                    break;
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (!isServer) return;
            switch (other.name)
            {
                case "RSZ":
                    if (role.Camp == CampT.Red)
                        Buffs.RemoveAll(b => b.type == BuffT.Revive);
                    break;
                case "BSZ":
                    if (role.Camp == CampT.Blue)
                        Buffs.RemoveAll(b => b.type == BuffT.Revive);
                    break;
                case "BBase":
                    if (role.Camp == CampT.Blue)
                        Buffs.RemoveAll(b => b.type == BuffT.Base);
                    break;
                case "RBase":
                    if (role.Camp == CampT.Red)
                        Buffs.RemoveAll(b => b.type == BuffT.Base);
                    break;
                case "BBunker":
                    if (role.Camp == CampT.Blue)
                        Buffs.RemoveAll(b => b.type == BuffT.Vcd);
                    break;
                case "RBunker":
                    if (role.Camp == CampT.Red)
                        Buffs.RemoveAll(b => b.type == BuffT.Vcd);
                    break;
                case "BOP":
                    if (role.Camp == CampT.Blue)
                        Buffs.RemoveAll(b => b.type == BuffT.Vcd);
                    break;
                case "ROP":
                    if (role.Camp == CampT.Red)
                        Buffs.RemoveAll(b => b.type == BuffT.Vcd);
                    break;
                case "BSP":
                    if (role.Camp == CampT.Blue)
                    {
                        Buffs.RemoveAll(b => b.type == BuffT.Vcd);
                        Buffs.RemoveAll(b => b.type == BuffT.Activator);
                    }

                    break;
                case "RSP":
                    if (role.Camp == CampT.Red)
                    {
                        Buffs.RemoveAll(b => b.type == BuffT.Vcd);
                        Buffs.RemoveAll(b => b.type == BuffT.Activator);
                    }

                    break;
                case "High":
                    Buffs.RemoveAll(b => b.type == BuffT.Vcd);
                    break;
            }
        }

        public void Hit(int hitter, CaliberT caliber)
        {
            if (isClient)
                CmdHit(hitter, caliber);
            else
            {
                gameManager.Emit(new HitEvent(hitter, id, caliber));
                HitRpc();
            }
        }

        [Command(ignoreAuthority = true)]
        private void CmdHit(int hitter, CaliberT caliber)
        {
            gameManager.Emit(new HitEvent(hitter, id, caliber));
            HitRpc();
        }

        [ClientRpc]
        private void HitRpc()
        {
            if (isLocalRobot && Math.Abs(GetAttr().ArmorRate - 1) > 1e-2) FindObjectOfType<GameManager>().Hurt();
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

        private bool IsGameObjectInCameraView(GameObject targetObj)
        {
            var fpCamera = fpCam.GetComponent<Camera>();
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

        private void Start()
        {
            tpCam.SetActive(false);
            fpCam.SetActive(false);
            ToggleMeshRenderer(chassis, true);
            ToggleMeshRenderer(spinner, false);
            _maxMotorTorque = oriMaxMotorTorque;
            _angleSpin = 0;
        }

        [Client]
        public override void ConfirmLocalRobot()
        {
            base.ConfirmLocalRobot();
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
            FindObjectOfType<GameManager>().LocalRobotRegister(this);
            _visual = GameObject.Find("Prediction").GetComponent<LineRenderer>();
            _fpActive = true;
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
            // Destroy(b, 4);
        }

        protected virtual bool FireOperation()
        {
            return false;
        }

        protected virtual void UnFireOperation()
        {
        }

        private void Fire()
        {
            var b = Instantiate(bullet, gun.position, gun.rotation);
            b.GetComponent<Rigidbody>().velocity = gun.forward * speed;
            var bulletController = b.GetComponent<BulletController>();
            bulletController.owner = id;
            bulletController.isActive = true;
            // Destroy(b, 4);
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

        private float _reviveUpdate;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            ArmorSetup();
            if (isServer)
            {
                if (Time.time - _reviveUpdate > 1)
                {
                    health += (int) (GetAttr().ReviveRate * RobotPerformanceTable.table[level][role.Type].HealthLimit);
                    if (health > RobotPerformanceTable.table[level][role.Type].HealthLimit)
                        health = RobotPerformanceTable.table[level][role.Type].HealthLimit;
                    _reviveUpdate = Time.time;
                }
            }

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

                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var sensitivity = FindObjectOfType<GameManager>().GetSensitivity();
                    // 车辆旋转速度计算
                    if (Input.GetAxis("Mouse X") != 0)
                    {
                        _steeringSpeed = ((Input.GetAxis("Mouse X") * 1.8f * sensitivity * 2) + _steeringSpeed) / 2;
                    }

                    if (Mathf.Abs(_steeringSpeed) > maxSteeringSpeed)
                    {
                        _steeringSpeed = _steeringSpeed > 0 ? maxSteeringSpeed : -maxSteeringSpeed;
                    }

                    // 俯仰速度计算
                    if (Input.GetAxis("Mouse Y") != 0)
                    {
                        _pitchingSpeed = -((Input.GetAxis("Mouse Y") * 1.2f * sensitivity * 2) + _pitchingSpeed) / 2;
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

                if (Input.GetKey(KeyCode.V) && role.Type != TypeT.Engineer)
                {
                    var targets = FindObjectsOfType<ArmorController>()
                        .Where(a => a.GetColor() != this.armors[0].GetColor() && a.GetColor() != ColorT.Down)
                        .Where(a => IsGameObjectInCameraView(a.gameObject));
                    var minDistance = float.MaxValue;
                    ArmorController target = null;
                    var fpCamera = fpCam.GetComponent<Camera>();
                    foreach (var t in targets)
                    {
                        var sp = fpCamera.WorldToScreenPoint(t.transform.position);
                        sp -= new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
                        var distance = sp.sqrMagnitude;
                        if (!(distance < minDistance)) continue;
                        if (Vector3.Angle(fpCam.transform.position - t.transform.position, t.transform.right) > 90)
                            continue;
                        minDistance = distance;
                        target = t;
                    }

                    if (target != null)
                    {
                        var position = fpCam.transform.position;
                        var targetPosition = target.transform.position;
                        var noise = Random.Range(-0.1f, 0.1f);
                        targetPosition += new Vector3(noise, noise, noise);
                        if (target == _lastTarget)
                        {
                            if (_prediction != Vector3.zero)
                            {
                                targetPosition += _prediction * _flightTime / _predictInterval;
                                targetPosition += Vector3.up * (role.Type == TypeT.Hero ? 0.1f : 0.07f);
                            }
                        }

                        // 辅助瞄准算法
                        var distance = (targetPosition - position).magnitude;
                        var alpha = Mathf.Asin((targetPosition.y - position.y) / distance);
                        var theta = alpha;
                        const float g = -9.8f;
                        for (var i = 0; i < 100; i++)
                        {
                            var vX0 = Mathf.Cos(theta) * speed;
                            var vY0 = Mathf.Sin(theta) * speed;
                            var t = Mathf.Cos(alpha) * distance / vX0;
                            _flightTime = t;
                            var sY = vY0 * t + 0.5f * g * Mathf.Pow(t, 2);
                            var err = Mathf.Sin(alpha) * distance - sY;
                            if (err < 1e-3) break;
                            var adjust = 1.0f / (1 + Mathf.Pow((float) Math.E, -err)) - 0.5f;
                            theta += 0.025f * (float) Math.PI * adjust;
                        }

                        var tY = Mathf.Tan(theta) * Mathf.Cos(alpha) * distance + position.y;

                        var correction = Vector3.zero;
                        if (target.transform.localRotation.eulerAngles.z > 45)
                            correction = target.transform.right * 0.08f;

                        var vTargetPos = new Vector3(targetPosition.x, tY, targetPosition.z) + correction;

                        if (Time.time - _lastPredictTime > 0.05f)
                        {
                            _predictInterval = Time.time - _lastPredictTime;
                            _lastPredictTime = Time.time;
                            if (target == _lastTarget) _prediction = target.transform.position - _lastPosition;
                            _lastTarget = target;
                            _lastPosition = target.transform.position;
                        }

                        var delta = fpCamera.WorldToScreenPoint(vTargetPos);
                        delta -= new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
                        var screenErr = delta;
                        delta *= 10;
                        delta.y /= Screen.height;
                        delta.x /= Screen.width;
                        _pitchingSpeed -= (1.0f / (1 + Mathf.Pow((float) Math.E, -delta.y)) - 0.5f) * 1.5f;
                        _steeringSpeed += (1.0f / (1 + Mathf.Pow((float) Math.E, -delta.x)) - 0.5f) * 1.5f;

                        {
                            var vY0 = Mathf.Sin(theta) * speed;
                            var xDir = new Vector3(targetPosition.x, 0, targetPosition.z) -
                                       new Vector3(position.x, 0, position.z);
                            var points = new List<Vector3>();
                            for (float t = 0; t < _flightTime; t += 0.02f)
                            {
                                var sY = vY0 * t + 0.5f * g * Mathf.Pow(t, 2);
                                var point = position + xDir * (t / _flightTime) + Vector3.up * sY +
                                            Vector3.up * 0.05f;
                                points.Add(point);
                            }

                            _visual.positionCount = points.Count;
                            _visual.SetPositions(points.ToArray());
                            var gradient = _visual.colorGradient;
                            var colorKeys = gradient.colorKeys;
                            Debug.Log(screenErr.magnitude);
                            colorKeys[0].color = screenErr.magnitude > 21.5f ? Color.red : Color.green;
                            gradient.colorKeys = colorKeys;
                            _visual.colorGradient = gradient;
                        }
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
                            Vector3.right * (Input.GetAxis("Horizontal") * (_climbing
                                ? oriMaxMotorTorque
                                : _maxMotorTorque) * 2)
                            / 10000);
                    }
                    else
                    {
                        transform.Translate(
                            Vector3.right * (Input.GetAxis("Horizontal") * (_climbing
                                ? oriMaxMotorTorque
                                : _maxMotorTorque) * 2)
                            / 20000);
                    }
                }

                if (Cursor.lockState == CursorLockMode.Locked && isLocalRobot)
                {
                    if (Input.GetMouseButton(1))
                    {
                        SetSpin(true);
                        _maxMotorTorque = oriMaxMotorTorque / 2;
                    }
                    else
                    {
                        SetSpin(false);
                        _maxMotorTorque = oriMaxMotorTorque;
                    }
                }

                var climb = transform.localRotation.eulerAngles.x;
                if (climb > 180) climb -= 360;
                if (climb < -2)
                {
                    _climbing = true;
                    _maxMotorTorque = oriMaxMotorTorque * Mathf.Abs(climb);
                }
                else
                {
                    _climbing = false;
                    _maxMotorTorque = oriMaxMotorTorque;
                }

                // Boost效果
                if (Input.GetKey(KeyCode.LeftShift) && !_isSpin || role.Type == TypeT.Engineer)
                {
                    _maxMotorTorque = oriMaxMotorTorque * 4;
                }

                // 射击
                if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButton(0))
                {
                    if (Input.GetMouseButton(0))
                    {
                        if (!FireOperation())
                        {
                            var caliber = bullet.GetComponent<BulletController>().caliber;
                            if (_fireCd == 0 && (caliber == CaliberT.Large ? largeAmmo > 0 : smallAmmo > 0))
                            {
                                if (caliber == CaliberT.Large)
                                {
                                    if (safe)
                                    {
                                        largeAmmo--;
                                        heat += 100;
                                        safe = false;
                                        Fire();
                                    }
                                }
                                else
                                {
                                    smallAmmo--;
                                    heat += 10;
                                    Fire();
                                    _fireCd = Random.Range(3, 8);
                                    if (highFreq) _fireCd /= 2;
                                }
                            }
                            else
                            {
                                if (_fireCd > 0) _fireCd--;
                            }
                        }
                    }
                }
                else
                {
                    UnFireOperation();
                }

                if (Cursor.lockState == CursorLockMode.Locked && Input.GetMouseButtonUp(0))
                {
                    if (role.Type == TypeT.Hero) safe = true;
                }


                var heatLimit = RobotPerformanceTable.table[level][role.Type].HeatLimit;
                var healthLimit = RobotPerformanceTable.table[level][role.Type].HealthLimit;
                if (heat > heatLimit && heat < heatLimit * 2)
                {
                    health -= (int) ((heat - heatLimit) / 250 * healthLimit * (Time.fixedDeltaTime / 1.0f));
                    heat -= RobotPerformanceTable.table[level][role.Type].CoolDownRate * GetAttr().ColdDownRate *
                            (Time.fixedDeltaTime / 1.0f);
                }
                else if (heat > heatLimit * 2)
                {
                    health -= (int) ((heat - heatLimit * 2) / 250 * healthLimit);
                    heat = heatLimit * 2;
                }
                else if (heat > 0)
                    heat -= RobotPerformanceTable.table[level][role.Type].CoolDownRate * GetAttr().ColdDownRate *
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
            }

            // Spin效果
            if (health > 0 && role.Type != TypeT.Engineer)
            {
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
            }
            else
            {
                ToggleMeshRenderer(chassis, true);
                ToggleMeshRenderer(spinner, false);
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