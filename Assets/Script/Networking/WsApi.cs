using System;
using System.Linq;
using Script.Networking.Game;
using Script.Networking.Lobby;
using UnityEngine;
using UnityWebSocket;

namespace Script.Networking
{
    [Serializable]
    public class PlayerStatus
    {
        public string name;
        public string role;
    }

    [Serializable]
    public enum ServerStatus
    {
        Lobby = 0,
        Playing = 1
    }

    [Serializable]
    public class StatusReport
    {
        public ServerStatus status;
        public PlayerStatus[] players;
    }

    public class WsApi
    {
        private readonly WebSocket _socket;
        private GameManager _gameManager;
        private RoomManager _roomManager;

        public WsApi(RoomManager rm)
        {
            _roomManager = rm;
            _socket = new WebSocket("ws://127.0.0.1:8765");
            _socket.OnOpen += OnOpen;
            _socket.OnClose += OnClose;
            _socket.OnError += OnError;
            _socket.OnMessage += OnMessage;
            _socket.ConnectAsync();
            _socket.SendAsync("Hello WsFwd.");
        }

        public void Stop()
        {
            _gameManager = null;
        }

        public void SetGameManager(GameManager gm) => _gameManager = gm;

        private static void OnOpen(object sender, OpenEventArgs e) => Debug.Log("Connected");
        private static void OnClose(object sender, CloseEventArgs e) => Debug.Log("Closed");
        private static void OnError(object sender, ErrorEventArgs e) => Debug.Log("Error:" + e.Message);

        private void OnMessage(object sender, MessageEventArgs e) => Debug.Log("Message:" + e.Data);

        private float _lastReportTime;

        public void OnFixedUpdate()
        {
            if (Time.time - _lastReportTime > 1)
            {
                _lastReportTime = Time.time;
                if (_gameManager != null)
                {
                    _socket.SendAsync(JsonUtility.ToJson(new StatusReport
                    {
                        status = ServerStatus.Playing,
                        players = _gameManager.GetPlayers().Select(player => new PlayerStatus
                            {name = player.displayName, role = player.role.Camp + " " + player.role.Type}).ToArray()
                    }));
                }
                else
                {
                    _socket.SendAsync(JsonUtility.ToJson(new StatusReport
                    {
                        status = ServerStatus.Lobby,
                        players = _roomManager.roomSlots.Select(player => new PlayerStatus
                        {
                            name = ((RoomPlayer) player).displayName,
                            role = _roomManager.GetRole(((RoomPlayer) player).id).Camp + " " +
                                   _roomManager.GetRole(((RoomPlayer) player).id).Type
                        }).ToArray()
                    }));
                }
            }
        }
    }
}