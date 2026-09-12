using Assets.CryptoKartz.Scripts.Managers;
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
    public class GameManager : M2MqttUnityClientNetwork, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private NetworkObject _liveCarPrefab;
        [SerializeField] private NetworkObject _ghostCarPrefab;
        [SerializeField] private List<NetworkObject> _carPrefabs = new List<NetworkObject>();

        private readonly Dictionary<PlayerRef, NetworkObject> _playerMap = new Dictionary<PlayerRef, NetworkObject>();
        private Dictionary<PlayerRef, PlayerDataNetwork> _playerDataMap = new Dictionary<PlayerRef, PlayerDataNetwork>();
        private List<string> eventMessages = new List<string>();

        private int _playerId;
        private int _playerCount;
        private PlayerRef _player;
        private int TrackLevelId { get; set; }

        // Flask API URL (Update if not running locally on the same machine)
        private readonly string API_BASE_URL = "http://localhost:8001/api";

        #region Session Info Publishing
        public IEnumerator SessionInfoPublish(SessionInfo sessionInfo)
        {
            var jsonSessionInfo = JsonConvert.SerializeObject(sessionInfo);
            client.Publish($"ckgame.sessioninfo.{sessionInfo.Name}", System.Text.Encoding.UTF8.GetBytes(jsonSessionInfo));
            yield return new WaitForSecondsRealtime(.033f);
        }

        public IEnumerator SessionInfoRemove(SessionInfo sessionInfo)
        {
            var jsonSessionInfo = JsonConvert.SerializeObject(sessionInfo);
            client.Publish($"ckgame.sessioninfo.remove.{sessionInfo.Name}", System.Text.Encoding.UTF8.GetBytes(jsonSessionInfo));
            yield return new WaitForSecondsRealtime(.033f);
        }

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (runner.IsServer)
            {
                Log.Info($"sessionList updated: {sessionList.Count} sessions");
            }
        }
        #endregion

        #region MQTT Client & Scoped Subscriptions

        public void SetClientId(string clientId) { this.clientId = clientId; }
        public void SetEncrypted(bool isEncrypted) { this.isEncrypted = isEncrypted; }

        protected override void OnConnecting()
        {
            base.OnConnecting();
            Debug.Log($"[GameManager] Connecting to broker on {brokerAddress}:{brokerPort}...");
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            Debug.Log($"[GameManager] Connected to broker on {brokerAddress}");
            SubscribeTopics();
        }

        protected override void OnConnectionFailed(string errorMessage)
        {
            Debug.Log($"[GameManager] CONNECTION FAILED! {errorMessage}");
        }

        protected override void OnDisconnected()
        {
            Debug.Log("[GameManager] Disconnected.");
        }

        protected override void OnConnectionLost()
        {
            Debug.Log("[GameManager] CONNECTION LOST!");
        }

        protected override void SubscribeTopics()
        {
            // OMNIBUS UPGRADE: Scope the subscription strictly to THIS room's SessionName
            if (Runner != null && Runner.SessionInfo != null)
            {
                string myRoomName = Runner.SessionInfo.Name;
                string scopedTopic = $"game/manager/{myRoomName}/#";

                client.Subscribe(new string[] { scopedTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                Debug.Log($"[GameManager] Scoped MQTT Subscription to: {scopedTopic}");
            }
            else
            {
                Debug.LogWarning("[GameManager] Runner or SessionInfo is null. Cannot subscribe to scoped topics yet.");
            }
        }

        protected override void UnsubscribeTopics()
        {
            if (Runner != null && Runner.SessionInfo != null)
            {
                string myRoomName = Runner.SessionInfo.Name;
                client.Unsubscribe(new string[] { $"game/manager/{myRoomName}/#" });
            }
        }
        #endregion

        #region Message Decoding
        protected override void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msg = System.Text.Encoding.UTF8.GetString(message);
                string myRoomName = Runner.SessionInfo.Name;

                // Validate the message is actually for this room
                if (!topic.Contains($"game/manager/{myRoomName}/")) return;

                if (topic.Contains("livecar"))
                {
                    Debug.Log($"[GameManager - {myRoomName}] livecar msg: {msg}");
                    CreateCarConfig carConfig = new CreateCarConfig(msg);
                    TrackLevelId = int.Parse(carConfig.RacePlatformLevel);

                    NetworkObject car = grlLiveCarSpawn(_liveCarPrefab);
                    TrackDefinitionManager tdm = GetTrackDefinitionManager(TrackLevelId);

                    try
                    {
                        car.gameObject.transform.SetParent(tdm.gameObject.transform, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GameManager] LiveCar SetParent Failed: {e.StackTrace}");
                    }
                }
                else if (topic.Contains("ghostcar"))
                {
                    Debug.Log($"[GameManager - {myRoomName}] ghostcar msg: {msg}");
                    CreateCarConfig carConfig = new CreateCarConfig(msg);
                    TrackLevelId = int.Parse(carConfig.RacePlatformLevel);

                    NetworkObject car = grlGhostCarSpawn(_ghostCarPrefab);
                    TrackDefinitionManager tdm = GetTrackDefinitionManager(TrackLevelId);

                    try
                    {
                        car.gameObject.transform.SetParent(tdm.gameObject.transform, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GameManager] GhostCar SetParent Failed: {e.StackTrace}");
                    }
                }
                else if (topic.Contains("updatetrack"))
                {
                    Debug.Log($"[GameManager - {myRoomName}] RaceTrackUpdated msg: {msg}");
                    UpdateRaceTrackConfig raceTrackConfig = new UpdateRaceTrackConfig(msg);
                    int rtLevel = int.Parse(raceTrackConfig.RacePlatformLevel);

                    GetTrackDefinitionManager(rtLevel).TrackId = raceTrackConfig.trackId;
                }

                StoreMessage(msg);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] DecodeMessage EXCEPTION: {e.Message}");
            }
        }

        private void StoreMessage(string eventMsg) { eventMessages.Add(eventMsg); }
        private void ProcessMessage(string msg) { /* Process generic messages here if needed */ }
        #endregion

        public override void FixedUpdateNetwork()
        {
            base.Update(); // call ProcessMqttEvents()

            if (eventMessages.Count > 0)
            {
                foreach (string msg in eventMessages)
                {
                    ProcessMessage(msg);
                }
                eventMessages.Clear();
            }
        }

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
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}