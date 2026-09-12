using Assets.CryptoKartz.Scripts.managers;
using Assets.CryptoKartz.Scripts.Utils;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
//using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using uPLibrary.Networking.M2Mqtt.Messages;
using Application = UnityEngine.Application;

namespace Assets.CryptoKartz.Scripts.Managers
{
    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class ServerManager : ServerManagerBaseNetwork, INetworkRunnerCallbacks
    {
        private List<string> eventMessages = new List<string>();

        // OMNIBUS UPGRADE: Track all active races (NetworkRunners) by their Session Name
        private Dictionary<string, NetworkRunner> _activeRaces = new Dictionary<string, NetworkRunner>();

        // Your Flask API URL (Update this to your actual server IP/Domain when deployed)
        private readonly string API_BASE_URL = "http://localhost:8001/api";

        #region MQTT Client

        public void SetClientId(string clientId)
        {
            this.clientId = clientId;
        }

        public void SetEncrypted(bool isEncrypted)
        {
            this.isEncrypted = isEncrypted;
        }

        protected override void OnConnecting()
        {
            base.OnConnecting();
            Debug.Log("Connecting to broker on " + brokerAddress + ":" + brokerPort.ToString() + "...\n");
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            SubscribeTopics();
            Debug.Log("Connected to broker on " + brokerAddress + "\n");
        }

        protected override void OnConnectionFailed(string errorMessage)
        {
            Debug.Log("CONNECTION FAILED! " + errorMessage);
        }

        protected override void OnDisconnected()
        {
            Debug.Log("Disconnected.");
        }

        protected override void OnConnectionLost()
        {
            Debug.Log("Server Manager CONNECTION LOST!");
        }

        protected override void SubscribeTopics()
        {
            client.Subscribe(new string[] { "server/manager/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        protected override void UnsubscribeTopics()
        {
            client.Unsubscribe(new string[] { "server/manager/#" });
        }

        #endregion

        protected override async void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msg = System.Text.Encoding.UTF8.GetString(message);
                Debug.Log($"[ServerManager] Master Agent Command Received on Topic: {topic} | Msg: {msg}");

                if (topic.Contains("server/manager/create_race"))
                {
                    var result = await CreateNewRaceRoom(msg);
                    Debug.Log("StartServer Result: " + result);
                }
                else if (topic.Contains("server/manager/close_race"))
                {
                    CloseRaceRoom(msg);
                }

                StoreMessage(msg);
            }
            catch (Exception e)
            {
                Debug.LogError("DecodeMessage Exception: " + e.StackTrace);
            }
        }

        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        public override void FixedUpdateNetwork()
        {
            base.Update();

            if (eventMessages.Count > 0)
            {
                eventMessages.Clear();
            }
        }

        public async Task<StartGameResult> CreateNewRaceRoom(string msg)
        {
            await Task.Yield();
            Application.targetFrameRate = 30;

            // Parse Master Agent Command
            CreateRaceTrackConfig config = new CreateRaceTrackConfig(msg);

            string newSessionName = config.SessionName;
            string targetLobby = config.LobbyName;

            // FIX: Ensure valid strings before starting
            if (string.IsNullOrEmpty(newSessionName) || string.IsNullOrEmpty(targetLobby))
            {
                Debug.LogError("[Omnibus] Cannot start race: SessionName or LobbyName is null/empty.");
                return default; // Safely returns default struct without throwing an error
            }

            if (_activeRaces.ContainsKey(newSessionName))
            {
                Debug.LogWarning($"[Omnibus] Race {newSessionName} already exists!");
                return default;
            }

            // Spawn a dedicated Runner for this specific Race
            NetworkRunner newRaceRunner = Instantiate(_runnerServerPrefab);
            newRaceRunner.name = $"ServerRunner_{newSessionName}";
            newRaceRunner.ProvideInput = true;

            // Start the Fusion Session (Room) inside the specified Lobby
            var result = await StartSession(
                newRaceRunner,
                GameMode.Server,
                newSessionName,
                targetLobby,
                SceneRef.FromIndex((int)SceneDefs.ERLGame)
            );

            // Validate and Register
            if (result.Ok)
            {
                Debug.Log($"[Omnibus] Successfully started race room: {newSessionName} in lobby: {targetLobby}");

                _activeRaces.Add(newSessionName, newRaceRunner);

                int trackLevelId = int.TryParse(config.RacePlatformLevel, out int parsedLevel) ? parsedLevel : 4;
                StartCoroutine(RegisterRaceInDatabaseCoroutine(newSessionName, targetLobby, trackLevelId));
            }
            else
            {
                Debug.LogError($"[Omnibus] Error starting room {newSessionName}: {result.ShutdownReason}");
                //newRaceRunner.Disconnect();
            }

            return result;
        }

        public void CloseRaceRoom(string msg)
        {
            // Simple parsing to find which room to close
            CreateRaceTrackConfig config = new CreateRaceTrackConfig(msg);
            string sessionToClose = config.SessionName;

            if (!string.IsNullOrEmpty(sessionToClose) && _activeRaces.TryGetValue(sessionToClose, out NetworkRunner runner))
            {
                Debug.Log($"[Omnibus] Shutting down race: {sessionToClose}");
                runner.Shutdown();
                _activeRaces.Remove(sessionToClose);
            }
        }

        public Task<StartGameResult> StartSession(NetworkRunner runner, GameMode gameMode, string sessionName, string lobbyName, SceneRef scene)
        {
            return runner.StartGame(new StartGameArgs()
            {
                CustomLobbyName = lobbyName,
                SessionName = sessionName,
                GameMode = gameMode,
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                Scene = scene,
            });
        }

        #region Database API Calls

        private System.Collections.IEnumerator RegisterRaceInDatabaseCoroutine(string roomName, string lobbyName, int trackLevelId)
        {
            string url = $"{API_BASE_URL}/races/create";

            string jsonPayload = $"{{\"lobby_name\":\"{lobbyName}\", \"room_name\":\"{roomName}\", \"track_level_id\":{trackLevelId}}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[Database] Failed to register race {roomName}: {request.error}");
                }
                else
                {
                    Debug.Log($"[Database] Successfully registered race {roomName}. Response: {request.downloadHandler.text}");
                }
            }
        }

        #endregion

        #region Network Callbacks
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}