using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using VivoxUnity;

namespace Script.Networking
{
    /*
     * 音频服务管理器脚本
     * 使用 VIVOX 提供语音、文本（暂无计划）通讯服务
     * 这里很多调用直接参考了 VIVOX 官方例程，没有太多可配置的参数
     * VIVOX 官方文档需要注册并完成认证才能查看
     * + 用户登录
     * + 切换频道
     * + 用户登出
     */
    public class VivoxManager : MonoBehaviour
    {
        // Secrets
        // 硬编码了一些认证数据，目前后台配置为 Sandbox 模式，可以随意测试
        // VIVOX 为独立项目提供（目前体量下用不完的）免费配额
        private const string TokenIssuer = "yanghu8187-ro49-dev";
        private const string TokenDomain = "mt1s.vivox.com";
        private const string TokenKey = "back250";
        private readonly TimeSpan _tokenExpiration = new TimeSpan(0, 1, 30);
        private readonly Uri _serverUri = new Uri("https://mt1s.www.vivox.com/api2");

        // 不同频道的名称
        private const string LobbyChannel = "lobbychannel";
        private const string RedChannel = "redchannel";
        private const string BlueChannel = "bluechannel";

        // 客户端侧成员
        private readonly Client _client = new Client();
        private ILoginSession _loginSession;
        private IChannelSession _channelSession;

        public bool Ready { private set; get; }
        public bool Fail { private set; get; }

        private void Awake()
        {
            var instances = FindObjectsOfType<VivoxManager>();
            if (instances.Length > 1)
                Destroy(gameObject);
            else
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _client.Initialize();
        }

        private void OnApplicationQuit()
        {
            _client.Uninitialize();
        }

        /*
         * 此类接口调用会阻塞较长时间
         * 以下方法都实现为 Coroutine，方便异步调用
         */
        public IEnumerator Login(string username)
        {
            yield return null;
            var accountId = new AccountId(
                TokenIssuer,
                username,
                TokenDomain);
            _loginSession = _client.GetLoginSession(accountId);
            _loginSession.PropertyChanged += OnLoginSessionPropertyChanged;
            _loginSession.BeginLogin(
                _serverUri,
                _loginSession.GetLoginToken(
                    TokenKey,
                    _tokenExpiration),
                ar =>
                {
                    try
                    {
                        _loginSession.EndLogin(ar);
                    }
                    catch (Exception)
                    {
                        Fail = true;
                    }
                }
            );
        }

        public IEnumerator SwitchRed()
        {
            yield return null;
            _channelSession.Disconnect();
            JoinChannel(RedChannel);
        }

        public IEnumerator SwitchBlue()
        {
            yield return null;
            _channelSession.Disconnect();
            JoinChannel(BlueChannel);
        }

        public void Logout()
        {
            Ready = false;
            try
            {
                _channelSession.Disconnect();
                _loginSession.Logout();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void JoinChannel(string channel)
        {
            var channelId = new ChannelId(
                TokenIssuer,
                channel,
                TokenDomain,
                ChannelType.Echo);
            _channelSession = _loginSession.GetChannelSession(channelId);
            _channelSession.PropertyChanged += SourceOnChannelPropertyChanged;
            _channelSession.BeginConnect(
                true, false, true,
                _channelSession.GetConnectToken(
                    TokenKey,
                    _tokenExpiration),
                ar =>
                {
                    try
                    {
                        _channelSession.EndConnect(ar);
                    }
                    catch (Exception)
                    {
                        Fail = true;
                    }
                });
        }

        private void OnLoginSessionPropertyChanged(
            object sender,
            PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != "State") return;
            switch (((ILoginSession) sender).State)
            {
                case LoginState.LoggedOut:
                    break;
                case LoginState.LoggedIn:
                    JoinChannel(LobbyChannel);
                    break;
                case LoginState.LoggingIn:
                    break;
                case LoginState.LoggingOut:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SourceOnChannelPropertyChanged(
            object sender,
            PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var channelSession = (IChannelSession) sender;
            if (propertyChangedEventArgs.PropertyName != "AudioState") return;
            switch (channelSession.AudioState)
            {
                case ConnectionState.Disconnected:
                    break;
                case ConnectionState.Connecting:
                    break;
                case ConnectionState.Connected:
                    if (channelSession.Channel.Name == LobbyChannel)
                        Ready = true;
                    break;
                case ConnectionState.Disconnecting:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}