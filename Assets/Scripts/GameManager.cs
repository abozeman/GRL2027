using Assets.GRL.Scripts.Managers;
using cryptokartz.Scripts.Player;
using Fusion;
using Fusion.Sockets;
using M2MqttUnity;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // Added for REST API
using System.Text;            // Added for payload encoding
using uPLibrary.Networking.M2Mqtt.Messages;
using cryptokartz.Scripts.Car;

namespace cryptokartz.Scripts.GameControllers
{
    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class GameManager : SimulationBehaviour, INetworkRunnerCallbacks, ISpawned, IDespawned
    {
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private NetworkObject _liveCarPrefab;
        [SerializeField] private NetworkObject _ghostCarPrefab;

        private readonly Dictionary<PlayerRef, NetworkObject> _playerMap = new Dictionary<PlayerRef, NetworkObject>();
        private struct QueuedMessage
        {
            public string Topic;
            public string Payload;
        }
        private List<QueuedMessage> eventMessages = new List<QueuedMessage>();

        private int _playerId;
        private int _playerCount;
        private PlayerRef _player;
        private int TrackLevelId { get; set; }
        private string Vid { get; set; }

        // Flask API URL (Update if not running locally on the same machine)
        private readonly string API_BASE_URL = "http://192.168.2.49:8001/api";
        public string SessionName;

        public void Spawned()
        {
            SessionName = Runner.SessionInfo.Name.ToLower();
            Debug.Log($"[GameManager] has Spawned in room: {SessionName}");

            if (M2MqttUnityClientNetwork.Instance != null && Runner != null)
            {
                // Subscribe to the global event
                M2MqttUnityClientNetwork.Instance.OnMessageReceived += DecodeMessage;

                string scopedTopic = $"game/manager/{SessionName}/#";
                M2MqttUnityClientNetwork.Instance.SubscribeToTopic(scopedTopic);
                Debug.Log($"[GameManager] Hooked into Singleton and subscribed to {scopedTopic}");
            }
        }

        public void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Runner.IsServer && M2MqttUnityClientNetwork.Instance != null)
            {
                string scopedTopic = $"game/manager/{SessionName}/#";
                M2MqttUnityClientNetwork.Instance.OnMessageReceived -= DecodeMessage;
                M2MqttUnityClientNetwork.Instance.UnsubscribeFromTopic(scopedTopic);
            }
        }

        #region Message Decoding

        protected void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msg = System.Text.Encoding.UTF8.GetString(message);
                string myRoomName = Runner.SessionInfo.Name.ToLower();

                if (!topic.Contains($"game/manager/{myRoomName}")) return;

                // FIX 2: Do NOT spawn here! Just queue it for FixedUpdateNetwork
                eventMessages.Add(new QueuedMessage { Topic = topic, Payload = msg });
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] DecodeMessage EXCEPTION: {e.Message}");
            }
        }


        public override void FixedUpdateNetwork()
        {
            if (eventMessages.Count > 0)
            {
                foreach (var msg in eventMessages)
                {
                    ProcessMessage(msg.Topic, msg.Payload);
                }
                eventMessages.Clear();
            }
        }

        private void ProcessMessage(string topic, string msg)
        {
            try
            {
                string myRoomName = Runner.SessionInfo.Name.ToLower();

                if (topic.Contains("livecar") || topic.Contains("ghostcar"))
                {
                    // FIX 2: Safely parse the JSON using Newtonsoft (JObject) 
                    // to completely ignore case-sensitivity mismatches in the Python payloads
                    var jsonData = Newtonsoft.Json.Linq.JObject.Parse(msg);

                    string rawLevel = (string)jsonData.GetValue("raceplatformlevel", StringComparison.OrdinalIgnoreCase)
                                   ?? (string)jsonData.GetValue("RacePlatformLevel", StringComparison.OrdinalIgnoreCase)
                                   ?? "4";

                    TrackLevelId = int.Parse(rawLevel);
                    Vid = (string)jsonData.GetValue("vid", StringComparison.OrdinalIgnoreCase);

                    if (topic.Contains("livecar"))
                    {
                        NetworkObject car = grlLiveCarSpawn(_liveCarPrefab);
                    }
                    else if (topic.Contains("ghostcar"))
                    {
                        NetworkObject car = grlGhostCarSpawn(_ghostCarPrefab);
                    }
                }
                else if (topic.Contains("updatetrack"))
                {
                    UpdateRaceTrackConfig raceTrackConfig = new UpdateRaceTrackConfig(msg);
                    int rtLevel = int.Parse(raceTrackConfig.RacePlatformLevel);
                    GetTrackDefinitionManager(rtLevel).TrackId = raceTrackConfig.trackId;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] ProcessMessage EXCEPTION: {e.Message}");
            }
        }

        #endregion

        #region Player & Database Logic
        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[GameManager] Entered OnPlayerJoined for PlayerRef: {player.PlayerId}");

            if (runner.IsServer && _playerPrefab != null)
            {
                _playerId = player.PlayerId;
                _playerCount = Runner.SessionInfo.PlayerCount - 1;
                _player = player;

                if (_playerCount == 0) return;

                // 1. Spawn the Avatar
                NetworkObject character = grlAvatarSpawn(_playerPrefab, player);
                _playerMap[player] = character;
                runner.SetPlayerObject(player, character);

                Log.Info($"[GameManager] Spawned Avatar for Player: {player}");

                // 2. OMNIBUS UPGRADE: Register the contestant in PostgreSQL
                // TODO: You will need to extract the actual User ID and Wallet from your auth system here.
                // For now, we are generating mock/placeholder data to ensure the pipeline works.
                string mockUserId = System.Guid.NewGuid().ToString();
                string mockWallet = "0xMockWalletAddress123456789";
                string emptyCarConfig = "{}";

                StartCoroutine(RegisterContestantInDatabaseCoroutine(
                    runner.SessionInfo.Name, // Use the Session Name as the Room ID
                    mockUserId,
                    mockWallet,
                    emptyCarConfig
                ));
            }
        }

        private IEnumerator RegisterContestantInDatabaseCoroutine(string roomName, string userId, string walletAddress, string carConfigJson)
        {
            string url = $"{API_BASE_URL}/contestants/join";

            // Build the payload mapping to your Flask API fields
            string jsonPayload = $@"{{
                ""race_id"": ""{roomName}"", 
                ""user_id"": ""{userId}"", 
                ""wallet_address"": ""{walletAddress}"", 
                ""car_config"": {carConfigJson}
            }}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[Database] Failed to register contestant in room {roomName}: {request.error} | Response: {request.downloadHandler.text}");
                }
                else
                {
                    Debug.Log($"[Database] Successfully registered contestant in room {roomName}. Response: {request.downloadHandler.text}");
                }
            }
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.TryGetPlayerObject(player, out NetworkObject character))
            {
                runner.Despawn(character);
                _playerMap.Remove(player);
                Log.Info($"[GameManager] Despawn for Player: {player}");

                // TODO: Optionally trigger an API call here to update contestant status to 'DNF' or 'LEFT'
            }

            if (_playerMap.Count == 0)
            {
                Log.Info("[GameManager] Last player left, notifying Omnibus to shutdown room...");
                // Note: The actual shutdown should likely be coordinated by the Master Agent via MQTT, 
                // but you can call runner.Shutdown() here if you want it to auto-close.
            }
        }
        #endregion

        #region Helper & Spawning Methods
        private TrackDefinitionManager GetTrackDefinitionManager(int level)
        {
            var tpc = GameObject.Find("TrackPlatformContainer");
            if (tpc == null) return null;

            TrackDefinitionManager[] trackDefinitionManagers = tpc.GetComponentsInChildren<TrackDefinitionManager>();
            foreach (TrackDefinitionManager trackDefManager in trackDefinitionManagers)
            {
                if (trackDefManager.LevelId == level) return trackDefManager;
            }
            return null;
        }

        private NetworkObject grlAvatarSpawn(NetworkObject _objPrefab, PlayerRef player)
        {
            return Runner.Spawn(_objPrefab, GetRacePlatformLevelVector(4), Quaternion.identity, inputAuthority: player);
        }

        private NetworkObject grlGhostCarSpawn(NetworkObject _objPrefab)
        {
            return Runner.Spawn(_objPrefab, Vector3.zero, Quaternion.identity, inputAuthority: PlayerRef.None, InitializeCarBeforeSpawn);
        }

        private NetworkObject grlLiveCarSpawn(NetworkObject _objPrefab)
        {
            return Runner.Spawn(_objPrefab, Vector3.zero, Quaternion.identity, inputAuthority: PlayerRef.None, InitializeCarBeforeSpawn);
        }

        private void InitializeCarBeforeSpawn(NetworkRunner runner, NetworkObject obj)
        {
            var objCarDataNetwork = obj.GetComponent<CarDataNetwork>();
            if (objCarDataNetwork != null)
            {
                objCarDataNetwork.LevelId = TrackLevelId;
                objCarDataNetwork.ColorId = 1;
                objCarDataNetwork.Vid = Vid;
            }

        }

        private Vector3 GetRacePlatformLevelVector(int level)
        {
            switch (level)
            {
                case 1: return new Vector3(0, 0.016f, 0);
                case 2: return new Vector3(0, 0.366f, 0);
                case 3: return new Vector3(0, 0.716f, 0);
                case 4:
                default: return new Vector3(0, 1.07f, 0);
            }
        }
        #endregion

        #region Unused Callbacks
        // (Keep the rest of your unused INetworkRunnerCallbacks here as they were before)
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {

        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {

        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {

        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {

        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {

        }

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {

        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {

        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {

        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {

        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {

        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }

        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {

        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {

        }

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}