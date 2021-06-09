using System;
using TMPro;
using Mirror;
using UnityEngine;
using Script.Networking.Lobby;
using UnityEditor;

namespace Script.Networking
{
    namespace Index
    {
        /*
         * 登陆页面管理器脚本
         * + 文本框输入
         * + 提示文本更新
         * + Tab键 切换文本框
         * + Return键 根据输入情况选择本地游戏、连接服务器
         * + F1键 以纯服务器模式启动
         * + Esc键 退出程序
         */
        public class IndexManager : MonoBehaviour
        {
            private RoomManager _roomManager;

            // 用户名、服务器地址文本框
            public TMP_InputField displayNameInputField;

            public TMP_InputField serverAddressInputField;

            // 输入内容
            private string _displayName = "";

            private string _serverAddress = "";

            // Return动作提示文字
            public TMP_Text enterHintText;

            private void Awake()
            {
                Screen.SetResolution(Screen.width, Screen.height, true);
            }

            private void Start()
            {
                _roomManager = GameObject.Find("RoomManager").GetComponent<RoomManager>();
                displayNameInputField.onValueChanged.AddListener(OnDisplayNameChanged);
                serverAddressInputField.onValueChanged.AddListener(OnServerAddressChanged);
                // 先更新一次提示文字
                CheckInfoComplete();
#if UNITY_SERVER
                if (Application.platform != RuntimePlatform.WebGLPlayer)
                    _roomManager.StartServer();
#endif
            }

            #region UI

            private void OnDisplayNameChanged(string value)
            {
                _displayName = value;
                CheckInfoComplete();
            }

            private void OnServerAddressChanged(string value)
            {
                _serverAddress = value;
                CheckInfoComplete();
            }

            private int CheckInfoComplete()
            {
                if (_displayName.Length > 0 && _serverAddress.Length > 0)
                {
                    enterHintText.text = "连接服务器";
                    return 0;
                }

                if (_displayName.Length > 0)
                {
                    enterHintText.text = "本地游戏";
                    return 1;
                }

                enterHintText.text = "填写信息";
                return 2;
            }


            // 防止触发连按的标志变量
            private bool _tabPressed;
            private bool _returnPressed;

            private void KeyEvents()
            {
                if (Input.GetKey(KeyCode.Tab))
                {
                    if (!_tabPressed)
                    {
                        _tabPressed = true;
                        // Tab键切换输入框
                        if (!displayNameInputField.isFocused)
                            displayNameInputField.Select();
                        else
                            serverAddressInputField.Select();
                    }
                }
                else
                {
                    _tabPressed = false;
                }

                if (Input.GetKey(KeyCode.Return))
                {
                    if (!_returnPressed)
                    {
                        _returnPressed = true;
                        _roomManager.LocalDisplayName = _displayName;
                        switch (CheckInfoComplete())
                        {
                            case 0:
                                // 填写了用户名和服务器地址
                                _roomManager.networkAddress = _serverAddress;
                                _roomManager.StartClient();
                                break;
                            case 1:
                                // 只填写用户名未填写服务器地址
                                // WebPlayer平台无法作为服务器
                                if (Application.platform != RuntimePlatform.WebGLPlayer)
                                    _roomManager.StartHost();
                                break;
                        }
                    }
                }
                else
                {
                    _returnPressed = false;
                }

                if (Input.GetKey(KeyCode.F1))
                {
                    if (Application.platform != RuntimePlatform.WebGLPlayer)
                        _roomManager.StartServer();
                }

                if (Input.GetKey(KeyCode.Escape))
                {
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }

            #endregion

            private void FixedUpdate()
            {
                // 未进行本地游戏或连接服务器
                if (!NetworkServer.active && !NetworkClient.isConnected)
                    KeyEvents();
            }
        }
    }
}