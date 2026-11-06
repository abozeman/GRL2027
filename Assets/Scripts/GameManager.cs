using Assets.GRL.Scripts;
using Assets.GRL.Scripts.Managers;
using cryptokartz.Scripts.Car;
using cryptokartz.Scripts.Player;
using Fusion;
using Fusion.Sockets;
using M2MqttUnity;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;            // Added for payload encoding
using UnityEngine;
using UnityEngine.Networking; // Added for REST API
using uPLibrary.Networking.M2Mqtt.Messages;

namespace cryptokartz.Scripts.GameControllers
{
    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class GameManager : SimulationBehaviour, INetworkRunnerCallbacks, ISpawned, IDespawned
    {
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private NetworkObject _liveCarPrefab;
        [SerializeField] private NetworkObject _ghostCarPrefab;
        [SerializeField] private NetworkObject _simCarPrefab;

        private readonly Dictionary<PlayerRef, NetworkObject> _playerMap = new Dictionary<PlayerRef, NetworkObject>();
        private struct QueuedMessage
        {
            public string Topic;
            public string Payload;
        }
        private List<QueuedMessage> eventMessages = new List<QueuedMessage>();

        private int TrackLevelId { get; set; }
        private string Vid { get; set; }

        // Flask API URL (Update if not running locally on the same machine)
        private readonly string API_BASE_URL = "http://192.168.2.49:8001/api";
        public string SessionName;

        public string RaceType { get; set; } = "practice"; // Defaults to practice for safety

        public void Spawned()
        {
            SessionName = Runner.SessionInfo.Name.ToLower();
            Debug.Log($"[GameManager] has Spawned in room: {SessionName}");

            // Read the Race Type passed down from the Orchestrator/Database
            if (Runner.SessionInfo.Properties.TryGetValue("raceType", out var typeProp))
            {
                RaceType = typeProp;
            }

            Debug.Log($"[GameManager] Spawned in room: {SessionName} | Race Type: {RaceType}");


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
            if (!runner.IsServer) return;

            // 1. Unpack the Connection Token to identify the client platform
            byte[] tokenBytes = runner.GetPlayerConnectionToken(player);
            string clientPlatform = tokenBytes != null ? System.Text.Encoding.UTF8.GetString(tokenBytes) : "UNKNOWN";

            Debug.Log($"[GameManager] PlayerRef {player.PlayerId} joined as {clientPlatform}. Race Type is {RaceType}");

            // 2. The Gatekeeper Logic
            if (RaceType == "practice" && (clientPlatform == "WEBXR" || clientPlatform == "QUEST"))
            {
                // PURE SIMULATION: Spawn a simulated car and grant browser input authority
                NetworkObject simCar = grlSimulatedCarSpawn(_simCarPrefab, player);
                _playerMap[player] = simCar;
                runner.SetPlayerObject(player, simCar);
                Debug.Log("[GameManager] Granted WebXR user a simulated car for practice.");
            }
            else if (RaceType == "live" && clientPlatform == "WEBXR")
            {
                // REAL RACE SPECTATOR: Do NOT spawn a car. Do NOT grant input authority.
                // They exist in the room purely to receive the network transforms and watch.
                Debug.Log("[GameManager] WebXR Spectator connected to Live race. No vehicle spawned.");
            }
            else if (RaceType == "live" && clientPlatform == "QUEST")
            {
                // PHYSICAL COMPETITOR: Spawn the floating VR Avatar 
                NetworkObject character = grlAvatarSpawn(_playerPrefab, player);
                _playerMap[player] = character;
                runner.SetPlayerObject(player, character);
                Debug.Log("[GameManager] Quest user connected. Avatar spawned.");
            }

            // 3. Database Registration (Run for everyone who connects)

            string raceId = "unknown"; // Default to unknown if not provided
            // Read the Race Type passed down from the Orchestrator/Database
            if (Runner.SessionInfo.Properties.TryGetValue("raceId", out var typeProp))
            {
                raceId = typeProp;
            }

            string mockUserId = System.Guid.NewGuid().ToString();
            string mockWallet = "0xMockWalletAddress123456789";
            //StartCoroutine(WaitAndRegisterContestant(runner, mockUserId, mockWallet, "{}"));
        }

        //private IEnumerator WaitAndRegisterContestant(NetworkRunner runner, string userId, string walletAddress, string carConfigJson)
        //{
        //    string raceId = string.Empty;

        //    // Loop until the ServerManager successfully injects the valid UUID into the properties
        //    while (string.IsNullOrEmpty(raceId) || raceId == "unknown")
        //    {
        //        if (runner.SessionInfo.Properties.TryGetValue("raceId", out var typeProp))
        //        {
        //            raceId = typeProp;
        //        }

        //        if (string.IsNullOrEmpty(raceId) || raceId == "unknown")
        //        {
        //            yield return new WaitForSeconds(0.5f); // Wait half a second and check again
        //        }
        //    }

        //    Debug.Log($"[Database] raceId : {raceId}");


        //    // Once we have a valid UUID, execute the actual API payload
        //    StartCoroutine(RegisterContestantInDatabaseCoroutine(runner.SessionInfo.Name, userId, walletAddress, raceId, carConfigJson));
        //}

        //private IEnumerator RegisterContestantInDatabaseCoroutine(string roomName, string userId, string walletAddress, string raceId,string carConfigJson)
        //{
        //    //string url = $"http://192.168.2.49:8001/api/contestants/join";

        //    // Build the payload mapping to your Flask API fields
        //    //string jsonPayload = $@"{{
        //    //    ""race_id"": ""{raceId}"", 
        //    //    ""user_id"": ""{userId}"", 
        //    //    ""wallet_address"": ""{walletAddress}"", 
        //    //    ""car_config"": {carConfigJson}
        //    //}}";


        //    string url = $"http://192.168.2.49:8001/api/contestants/join";
        //    Debug.Log($"[Database] url : {url}");

        //    //string url = $"{API_BASE_URL}/races/create";
        //    string jsonPayload = $"{{\"race_id\":\"{raceId}\", \"user_id\":\"{userId}\", \"wallet_address\":\"{walletAddress}\", \"car_config\":\"{carConfigJson}\"}}";
        //    Debug.Log($"[Database] jsonPayload : {jsonPayload}");

        //    using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        //    {
        //        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        //        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        //        request.downloadHandler = new DownloadHandlerBuffer();
        //        request.SetRequestHeader("Content-Type", "application/json");

        //        yield return request.SendWebRequest();

        //        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        //        {
        //            Debug.LogError($"[Database] Failed to register contestant in room {roomName}: {request.error} | Response: {request.downloadHandler.text}");
        //        }
        //        else
        //        {
        //            Debug.Log($"[Database] Successfully registered contestant in room {roomName}. Response: {request.downloadHandler.text}");
        //        }
        //    }
        //}

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

        private NetworkObject grlSimulatedCarSpawn(NetworkObject _objPrefab, PlayerRef player)
        {
            return Runner.Spawn(_objPrefab, Vector3.zero, Quaternion.identity, inputAuthority: player, InitializeSimulatedCarBeforeSpawn);
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

        private void InitializeSimulatedCarBeforeSpawn(NetworkRunner runner, NetworkObject obj)
        {
            // Inherit the track level and color logic you already wrote
            InitializeCarBeforeSpawn(runner, obj);

            // Flag the car to intercept MQTT and use local Unity physics
            var inputManager = obj.GetComponent<CarInputManagerLive>();
            if (inputManager != null)
            {
                inputManager.IsSimulated = true;
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