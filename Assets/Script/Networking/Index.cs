using TMPro;
using Mirror;
using UnityEngine;
using Script.Networking.Lobby;

namespace Script.Networking
{
    namespace Index
    {
        public class Index : MonoBehaviour
        {
            public TMP_InputField displayNameInputField;
            public TMP_InputField serverAddressInputField;
            public TMP_Text enterHintText;

            private RoomManager _manager;

            private string _displayName = "";
            private string _serverAddress = "";

            private void Start()
            {
                _manager = GameObject.Find("RoomManager").GetComponent<RoomManager>();
                displayNameInputField.onValueChanged.AddListener(OnDisplayNameChanged);
                serverAddressInputField.onValueChanged.AddListener(OnServerAddressChanged);
                CheckInfoComplete();
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


            private bool _tabPressed;
            private bool _returnPressed;

            private void KeyEvents()
            {
                if (Input.GetKey(KeyCode.Tab))
                {
                    if (!_tabPressed)
                    {
                        _tabPressed = true;
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
                        _manager.SelfDisplayName = _displayName;
                        switch (CheckInfoComplete())
                        {
                            case 0:
                                _manager.networkAddress = _serverAddress;
                                _manager.StartClient();
                                break;
                            case 1:
                                if (Application.platform != RuntimePlatform.WebGLPlayer)
                                    _manager.StartHost();
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
                        _manager.StartServer();
                }

                if (Input.GetKey(KeyCode.Escape))
                    Application.Quit();
            }

            #endregion

            private void FixedUpdate()
            {
                if (!NetworkServer.active && !NetworkClient.isConnected)
                    KeyEvents();
            }
        }
    }
}