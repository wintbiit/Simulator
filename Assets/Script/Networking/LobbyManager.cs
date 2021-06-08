using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using Script.JudgeSystem.Role;

namespace Script.Networking
{
    namespace Lobby
    {
        // 角色选择UI三件套：选择按钮，准备状态，操作手用户名
        [Serializable]
        public class RoleSelect
        {
            public Button button;
            public RawImage readyStatus;
            public TMP_Text displayName;
        }

        /*
         * 准备大厅管理器脚本
         * + 处理登录用户名冲突
         * + 初始化音频设备
         * + 同步客户端角色选择信息
         * + 同步客户端准备状态信息
         * + R键 根据当前状态选择准备、取消准备、开始游戏
         * + Esc键 断开连接
         */
        public class LobbyManager : NetworkBehaviour
        {
            // 服务器端成员
            private RoomManager _roomManager;

            // 分别存储 ID到角色、ID到准备状态、网络连接ID到ID，ID到用户名数据
            // 此处全部采用同步字典实现，可能有网络性能优化空间
            private readonly SyncDictionary<int, RoleT> _roles = new SyncDictionary<int, RoleT>();
            private readonly SyncDictionary<int, bool> _readyStatus = new SyncDictionary<int, bool>();
            private readonly SyncDictionary<int, int> _connections = new SyncDictionary<int, int>();
            private readonly SyncDictionary<int, string> _displayNames = new SyncDictionary<int, string>();

            // 准备情况标识
            [SyncVar] private bool _allReady;

            /*
             * 游戏类型标识
             * 此设计的作用是方便单人跑图
             * 如果玩家运行本地服务器，而且只有自己一个人，不需要裁判就可以开始游戏
             */
            [SyncVar] private bool _isHost;

            // 客户端成员
            // 音频管理器
            private VivoxManager _vivoxManager;

            // R键提示文本和音频设备初始化错误提示文本
            public TMP_Text readyHint;
            public GameObject audioFailHint;

            // 裁判、红方与蓝方的角色选择UI控件
            private readonly RoleSelect _judgeSelect = new RoleSelect();
            private readonly List<RoleSelect> _redCampSelects = new List<RoleSelect>();
            private readonly List<RoleSelect> _blueCampSelects = new List<RoleSelect>();
            private RoomPlayer _localPlayer;

            #region Server

            [Server]
            public void RoomManagerRegister(RoomManager roomManager)
            {
                _roomManager = roomManager;
            }

            [Server]
            public RoleT GetRole(int id)
            {
                if (_roles.Keys.Contains(id))
                    return _roles[id];
                return new RoleT(CampT.Unknown, TypeT.Unknown);
            }

            [Command(requiresAuthority = false)]
            private void CmdStartGame()
            {
                _roomManager.StartGame(_roles);
            }

            // 登记客户端信息
            [Command(requiresAuthority = false)]
            private void CmdPlayerRegister(int connectionId, int index, string displayName)
            {
                // 如果有重名玩家，先使用 ClientRpc 发送通知，延迟一秒等待通知送达，然后断开连接
                if (_displayNames.Any(existName => existName.Value == displayName))
                {
                    RpcPlayerNameCollides(index);
                    StartCoroutine(DelayedDisconnect(connectionId, 1));
                }
                else
                {
                    // 记录客户端信息并使用 ClientRpc 通知客户端已经登记完成
                    _connections.Add(connectionId, index);
                    _roles.Add(index, new RoleT());
                    _readyStatus.Add(index, false);
                    _displayNames.Add(index, displayName);
                    RpcPlayerRegistered(index);
                }
            }

            private IEnumerator DelayedDisconnect(int connectionId, float waitTime)
            {
                yield return new WaitForSeconds(waitTime);
                _roomManager.Disconnect(connectionId);
            }

            [Command(requiresAuthority = false)]
            private void CmdDisconnect(int connectionId)
            {
                _roomManager.Disconnect(connectionId);
                if (_isHost)
                    _roomManager.StopHost();
            }

            [Server]
            public void PlayerLeave(int connectionId)
            {
                try
                {
                    // 清除客户端登记信息
                    var index = _connections[connectionId];
                    _connections.Remove(connectionId);
                    _roles.Remove(index);
                    _readyStatus.Remove(index);
                    _displayNames.Remove(index);
                }
                catch (KeyNotFoundException)
                {
                    // 可能出现异步重复清除，忽略
                }
            }

            [Command(requiresAuthority = false)]
            private void CmdSelectRole(int index, RoleT target)
            {
                // 如果点击了当前已选角色的按钮，则取消选择
                if (_roles[index].Equals(target))
                {
                    _roles[index] = new RoleT();
                    return;
                }

                // 如果想选择的角色已被占用，忽略
                if (_roles.Any(role => role.Value.Equals(target))) return;
                if (target.Type == TypeT.Ptz)
                    if (_roles[index].Type == TypeT.Drone) return;
                    else if (_roles.All(role => !role.Value.Equals(new RoleT(target.Camp, TypeT.Drone))))
                        return;

                // 选择到角色
                _roles[index] = target;
                // 如果选择的不是裁判，同时游戏模式不是单人跑图，默认准备状态为未准备
                if (target.Camp != CampT.Judge && !_isHost)
                {
                    RpcChangeReadyState(index, false);
                    _readyStatus[index] = false;
                }
                else
                {
                    // 否则自动准备
                    RpcChangeReadyState(index, true);
                    _readyStatus[index] = true;
                }
            }

            [Command(requiresAuthority = false)]
            private void CmdChangeReadyState(int index, bool state)
            {
                _readyStatus[index] = state;
            }

            #endregion

            #region Client

            // 此处应改为 TargetRpc 实现以提升网络性能
            [ClientRpc]
            private void RpcPlayerNameCollides(int index)
            {
                // 对服务端的用户名冲突通知进行响应
                if (_localPlayer.id == index)
                    Debug.Log("用户名冲突！请更换用户名。");
            }

            [Client]
            public void PlayerRegister(RoomPlayer roomPlayer)
            {
                _localPlayer = roomPlayer;
                // 先获取音频管理器实例，调用服务器对本客户端进行登记
                if (!_vivoxManager)
                    _vivoxManager = FindObjectOfType<VivoxManager>();
                CmdPlayerRegister(
                    _localPlayer.connectionId,
                    _localPlayer.id,
                    _localPlayer.displayName);
            }

            // 此处应改为 TargetRpc 实现以提升网络性能
            [ClientRpc]
            private void RpcPlayerRegistered(int index)
            {
                // 登记完成（没有用户名冲突）后，方可初始化音频服务
                if (_localPlayer.id == index)
                    StartCoroutine(
                        _vivoxManager.Login(
                            _localPlayer.displayName));
            }

            private void Start()
            {
                // 隐藏音频设备错误标识
                audioFailHint.SetActive(false);
                // 绑定角色选择UI
                BindSelect(GameObject.Find("JudgeSelect").transform, _judgeSelect, CampT.Judge, TypeT.Unknown);
                BindSelects(GameObject.Find("Red"), _redCampSelects, CampT.Red);
                BindSelects(GameObject.Find("Blue"), _blueCampSelects, CampT.Blue);
            }

            private void BindSelect(Transform role, RoleSelect roleSelect, CampT camp, TypeT type)
            {
                roleSelect.button = role.GetChild(0).GetComponent<Button>();
                roleSelect.readyStatus = role.GetChild(1).GetComponent<RawImage>();
                roleSelect.displayName = role.GetChild(2).GetComponent<TMP_Text>();
                roleSelect.button.onClick.AddListener(() =>
                {
                    /*
                     * 角色选择按钮点击回调函数
                     * + 如果是纯服务器端，无需响应
                     * + 音频设备未就绪，无需响应
                     * + 音频设备已就绪或已出错，尝试选择角色
                     * （音频设备出错后，可以继续以无音频通讯模式游戏）
                     */
                    if (_roomManager && _roomManager.IsServer && !_roomManager.IsHost) return;
                    if (_vivoxManager.Ready || _vivoxManager.Fail)
                        CmdSelectRole(_localPlayer.id, new RoleT(camp, type));
                    else
                        Debug.Log("音频设备未就绪。");
                });
            }

            private void BindSelects(GameObject root, ICollection<RoleSelect> campSelects, CampT camp)
            {
                var started = false;
                var roleIndex = 0;
                foreach (Transform child in root.transform)
                {
                    if (child.name == "Hero") started = true;
                    if (!started) continue;
                    campSelects.Add(new RoleSelect());
                    BindSelect(child.transform, campSelects.Last(), camp, (TypeT) roleIndex);
                    roleIndex++;
                }
            }

            // 此处应重写为 TargetRpc
            [ClientRpc]
            private void RpcChangeReadyState(int index, bool state)
            {
                if (_localPlayer.id != index) return;
                _localPlayer.CmdChangeReadyState(state);
            }

            #endregion

            #region FixedUpdate

            private bool _rPressed;
            private bool _escPressed;

            private void FixedUpdate()
            {
                // 实例存在表示代码运行于服务器端
                if (_roomManager)
                {
                    _allReady = _roomManager.allPlayersReady;
                    // 判断是否为单机跑图
                    _isHost = _roomManager.IsHost && FindObjectsOfType<RoomPlayer>().Length == 1;
                    // 如果是纯服务器模式后面的UI处理就不需要了
                    if (_roomManager.IsServer && !_roomManager.IsHost) return;
                }

                // 音频服务初始化抛了Exception
                if (_vivoxManager.Fail)
                    audioFailHint.SetActive(true);

                if (Input.GetKey(KeyCode.R))
                {
                    if (!_rPressed)
                    {
                        _rPressed = true;
                        // 已经选了角色
                        if (_roles[_localPlayer.id].Camp != CampT.Unknown)
                        {
                            // 单机跑图可以直接开始游戏
                            if (_isHost) CmdStartGame();
                            // 如果不是裁判，则改变准备状态
                            if (_roles[_localPlayer.id].Camp != CampT.Judge)
                            {
                                var currentState = _readyStatus[_localPlayer.id];
                                _localPlayer.CmdChangeReadyState(!currentState);
                                CmdChangeReadyState(_localPlayer.id, !currentState);
                            }
                            // 是裁判而且全部玩家准备完毕，可以开始游戏
                            else if (_allReady)
                                CmdStartGame();
                        }
                    }
                }
                else
                {
                    _rPressed = false;
                }

                if (Input.GetKey(KeyCode.Escape))
                {
                    if (!_escPressed)
                    {
                        _escPressed = true;
                        CmdDisconnect(_localPlayer.connectionId);
                        _vivoxManager.Logout();
                    }
                }
                else
                {
                    _escPressed = false;
                }

                // 根据角色选择、音频设别状态、准备状态更新提示文本
                if (_roles.ContainsKey(_localPlayer.id))
                {
                    readyHint.text =
                        _localPlayer.readyToBegin ? "取消准备" : "准备";
                    if (_roles[_localPlayer.id].Camp == CampT.Judge)
                        readyHint.text = _allReady ? "开始游戏" : "等待准备";
                    if (_roles[_localPlayer.id].Camp == CampT.Unknown)
                        readyHint.text = "选择角色";
                    else if (_isHost)
                        readyHint.text = "开始游戏";
                    if (!_vivoxManager.Ready && !_vivoxManager.Fail)
                        readyHint.text = "等待音频设备就绪";
                }

                // UI更新代码可以降低执行频率以降低CPU占用
                // 还原UI显示
                _judgeSelect.displayName.text = "等待中";
                foreach (var roleSelect in _redCampSelects)
                {
                    roleSelect.readyStatus.color = Color.white;
                    roleSelect.displayName.text = "等待中";
                }

                foreach (var roleSelect in _blueCampSelects)
                {
                    roleSelect.readyStatus.color = Color.white;
                    roleSelect.displayName.text = "等待中";
                }

                // 根据角色选择和准备状态，更新UI显示
                foreach (var role in _roles)
                {
                    switch (role.Value.Camp)
                    {
                        case CampT.Judge:
                            _judgeSelect.displayName.text = _displayNames[role.Key];
                            break;
                        case CampT.Red:
                            _redCampSelects[(int) role.Value.Type].readyStatus.color =
                                _readyStatus[role.Key] ? Color.green : Color.red;
                            _redCampSelects[(int) role.Value.Type].displayName.text = _displayNames[role.Key];
                            break;
                        case CampT.Blue:
                            _blueCampSelects[(int) role.Value.Type].readyStatus.color =
                                _readyStatus[role.Key] ? Color.green : Color.red;
                            _blueCampSelects[(int) role.Value.Type].displayName.text = _displayNames[role.Key];
                            break;
                        case CampT.Unknown:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            #endregion
        }
    }
}