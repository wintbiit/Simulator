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
        [Serializable]
        public class RoleSelect
        {
            public Button button;
            public RawImage readyStatus;
            public TMP_Text displayName;
        }

        public class LobbyManager : NetworkBehaviour
        {
            // Server side
            private readonly SyncDictionary<int, int> _connections = new SyncDictionary<int, int>();
            private readonly SyncDictionary<int, Role> _roles = new SyncDictionary<int, Role>();
            private readonly SyncDictionary<int, string> _displayNames = new SyncDictionary<int, string>();
            private readonly SyncDictionary<int, bool> _readyStatus = new SyncDictionary<int, bool>();

            private RoomManager _roomManager;
            [SyncVar] private bool _allReady;
            [SyncVar] private bool _isHost;

            // Client side
            public TMP_Text readyHint;

            private readonly RoleSelect _judgeSelect = new RoleSelect();
            private readonly List<RoleSelect> _redCampSelects = new List<RoleSelect>();
            private readonly List<RoleSelect> _blueCampSelects = new List<RoleSelect>();
            private RoomPlayer _localPlayer;

            private VivoxManager _vivoxManager;

            #region Server

            [Server]
            public void RoomManagerRegister(RoomManager roomManager)
            {
                _roomManager = roomManager;
            }

            [Command(ignoreAuthority = true)]
            private void CmdStartGame()
            {
                _roomManager.StartGame(_roles);
            }

            [Command(ignoreAuthority = true)]
            private void CmdPlayerRegister(int connectionId, int index, string displayName)
            {
                if (_displayNames.Any(existName => existName.Value == displayName))
                {
                    RpcPlayerNameCollides(index);
                    StartCoroutine(DelayedDisconnect(connectionId, 1));
                }
                else
                {
                    _connections.Add(connectionId, index);
                    _roles.Add(index, new Role());
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

            [Command(ignoreAuthority = true)]
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
                    var index = _connections[connectionId];
                    _connections.Remove(connectionId);
                    _roles.Remove(index);
                    _readyStatus.Remove(index);
                    _displayNames.Remove(index);
                }
                catch (KeyNotFoundException)
                {
                }
            }

            [Command(ignoreAuthority = true)]
            private void CmdSelectRole(int index, Role target)
            {
                if (_roles[index].Equals(target))
                {
                    _roles[index] = new Role();
                    return;
                }

                if (_roles.Any(role => role.Value.Equals(target))) return;
                if (target.Camp != CampT.Judge && !_isHost)
                {
                    RpcChangeReadyState(index, false);
                    _readyStatus[index] = false;
                }
                else
                {
                    RpcChangeReadyState(index, true);
                    _readyStatus[index] = true;
                }

                _roles[index] = target;
            }


            [Command(ignoreAuthority = true)]
            private void CmdChangeReadyState(int index, bool state)
            {
                _readyStatus[index] = state;
            }

            #endregion

            #region Client

            [ClientRpc]
            private void RpcPlayerNameCollides(int index)
            {
                if (_localPlayer.index == index)
                    Debug.Log("用户名冲突！请更换用户名。");
            }

            [Client]
            public void PlayerRegister(RoomPlayer roomPlayer)
            {
                _localPlayer = roomPlayer;
                if (!_vivoxManager)
                    _vivoxManager = FindObjectOfType<VivoxManager>();
                CmdPlayerRegister(
                    _localPlayer.connectionId,
                    _localPlayer.index,
                    _localPlayer.displayName);
            }

            [ClientRpc]
            private void RpcPlayerRegistered(int index)
            {
                if (_localPlayer.index == index)
                    StartCoroutine(
                        _vivoxManager.Login(
                            _localPlayer.displayName));
            }

            private void Start()
            {
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
                    if (_roomManager && _roomManager.IsServer && !_roomManager.IsHost ) return;
                    if (_vivoxManager.Ready)
                        CmdSelectRole(_localPlayer.index, new Role(camp, type));
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


            [ClientRpc]
            private void RpcChangeReadyState(int index, bool state)
            {
                if (_localPlayer.index != index) return;
                _localPlayer.CmdChangeReadyState(state);
            }

            #endregion

            #region FixedUpdate

            private bool _rPressed;
            private bool _escPressed;

            private void FixedUpdate()
            {
                if (_roomManager)
                {
                    _allReady = _roomManager.allPlayersReady;
                    _isHost = _roomManager.IsHost && _roles.Count == 1;
                    if (_roomManager.IsServer && !_roomManager.IsHost) return;
                }

                if (Input.GetKey(KeyCode.R))
                {
                    if (!_rPressed)
                    {
                        _rPressed = true;
                        if (_roles[_localPlayer.index].Camp != CampT.Unknown)
                        {
                            if (_isHost) CmdStartGame();
                            if (_roles[_localPlayer.index].Camp != CampT.Judge)
                            {
                                var currentState = _readyStatus[_localPlayer.index];
                                _localPlayer.CmdChangeReadyState(!currentState);
                                CmdChangeReadyState(_localPlayer.index, !currentState);
                            }
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

                if (_roles.ContainsKey(_localPlayer.index))
                {
                    readyHint.text =
                        _localPlayer.readyToBegin ? "取消准备" : "准备";
                    if (_roles[_localPlayer.index].Camp == CampT.Judge)
                        readyHint.text = _allReady ? "开始游戏" : "等待准备";
                    if (_isHost && _roles[_localPlayer.index].Camp != CampT.Unknown)
                    {
                        readyHint.text = "开始游戏";
                    }

                    if (!_vivoxManager.Ready)
                        readyHint.text = "等待音频设备就绪";
                }

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