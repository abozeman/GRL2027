using Assets.GRL.Scripts.managers;
using Assets.GRL.Scripts.Models;
using cryptokartz.Scripts.GameControllers;
using Fusion;
using Fusion.Sockets;
using M2MqttUnity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;

//using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using uPLibrary.Networking.M2Mqtt.Messages;
using Application = UnityEngine.Application;

namespace Assets.GRL.Scripts.Managers
{
    

    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class ServerManager : ServerManagerBaseNetwork, INetworkRunnerCallbacks
    {
        [System.Serializable]
        private struct CreateRacePayload
        {
            public string lobby_name;
            public string room_name;
            public string track_level_id;
            public string track_id;
            public string race_type;
            public int target_laps;
            public int pre_race_secs;
            public int accept_contestants_secs;
            public int countdown_secs;

        }

        private List<string> eventMessages = new List<string>();

        // OMNIBUS UPGRADE: Track all active races (NetworkRunners) by their Session Name
        private Dictionary<string, NetworkRunner> _activeRaces = new Dictionary<string, NetworkRunner>();

        // Your Flask API URL (Update this to your actual server IP/Domain when deployed)
        private readonly string API_BASE_URL = "http://192.168.2.49:8001/api";
        private SceneRef sRef;
        private string _serverType;

        public void Awake()
        {
            // 1. Read the tag from the Docker environment (default to practice for safety)
            _serverType = System.Environment.GetEnvironmentVariable("SERVER_TYPE");
            if (string.IsNullOrEmpty(_serverType)) _serverType = "practice";

            Debug.Log($"[ServerManager] Booting as {_serverType.ToUpper()} server.");

            if (M2MqttUnityClientNetwork.Instance != null)
            {
                M2MqttUnityClientNetwork.Instance.OnMessageReceived += DecodeMessage;
                // 2. Subscribe ONLY to this server's specific topic
                string scopedTopic = $"server/manager/{_serverType}/#";
                M2MqttUnityClientNetwork.Instance.SubscribeToTopic(scopedTopic);
                Debug.Log($"[ServerManager] Subscribed to {scopedTopic}");
            }
            
        }

        protected async void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msg = System.Text.Encoding.UTF8.GetString(message);
                //Debug.Log($"[ServerManager] Master Agent Command Received on Topic: {topic} | Msg: {msg}");

                if (topic.Contains($"server/manager/{_serverType}/create_race"))
                {
                    var result = await CreateNewRaceRoom(msg);
                    Debug.Log("StartServer Result: " + result);

                }
                else if (topic.Contains($"server/manager/{_serverType}/close_race"))
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
            if (eventMessages.Count > 0)
            {
                eventMessages.Clear();
            }
        }

        public async Task<StartGameResult> CreateNewRaceRoom(string msg)
        {
            await Task.Yield();
            Application.targetFrameRate = 30;

            CreateRaceConfig config = new CreateRaceConfig(msg);

            System.Random random = new System.Random();

            string newSessionName = config.SessionName;
            string targetLobby = config.LobbyName;
            string trackId = config.TrackId;
            string trackLevelId = config.RacePlatformLevel;
            string raceType = config.RaceType;
            string raceId = string.Empty;


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

            NetworkRunner newRaceRunner = Instantiate(_runnerServerPrefab);
            newRaceRunner.name = $"ServerRunner_{newSessionName}";
            newRaceRunner.ProvideInput = true;
            newRaceRunner.GetComponent<GameManager>().SessionName = newSessionName;

            sRef = GetSceneRefFromPath("Assets/Scenes/GRLGame.unity");

            // 1. Pack ALL data including dynamic durations into SessionProperties
            var sessionProperties = new Dictionary<string, SessionProperty>
            {
                { "trackId", config.TrackId },
                { "racePlatformLevel", config.RacePlatformLevel },
                { "raceType", config.RaceType },
                { "raceId", "" }, // Initialized empty; populated via API response later
                { "lapTarget", config.LapTarget },
                { "preRaceSecs", config.PreRaceSecs },
                { "acceptContestantsSecs", config.AcceptContestantsSecs },
                { "countdownSecs", config.CountdownSecs }
            };

            // Start the Fusion Session
            var result = await StartSession(
                newRaceRunner,
                GameMode.Server,
                newSessionName,
                targetLobby,
                sRef,
                sessionProperties
            );

            if (result.Ok)
            {
                Debug.Log($"[Omnibus] Successfully started race room: {newSessionName}");
                _activeRaces.Add(newSessionName, newRaceRunner);

                // 2. Trigger DB registration to generate the raceId UUID
                StartCoroutine(RegisterRaceInDatabaseCoroutine(
                    newRaceRunner,
                    newSessionName,
                    targetLobby,
                    config
                ));
            }
            else
            {
                Debug.LogError($"[Omnibus] Error starting room {newSessionName}: {result.ShutdownReason}");
                await newRaceRunner.Shutdown();
            }

            return result;
        }

        public void CloseRaceRoom(string msg)
        {
            // Simple parsing to find which room to close
            CreateRaceConfig config = new CreateRaceConfig(msg);
            string sessionToClose = config.SessionName;

            if (!string.IsNullOrEmpty(sessionToClose) && _activeRaces.TryGetValue(sessionToClose, out NetworkRunner runner))
            {
                Debug.Log($"[Omnibus] Shutting down race: {sessionToClose}");
                runner.Shutdown();
                _activeRaces.Remove(sessionToClose);
            }
        }

        public Task<StartGameResult> StartSession(NetworkRunner runner, GameMode gameMode, string sessionName, string lobbyName, SceneRef scene, Dictionary<string, SessionProperty> customProps = null)
        {
            return runner.StartGame(new StartGameArgs()
            {
                CustomLobbyName = lobbyName,
                SessionName = sessionName,
                GameMode = gameMode,
                Address = Fusion.Sockets.NetAddress.Any(0),
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                Scene = scene,
                SessionProperties = customProps // <--- Injects properties directly into the Fusion room
            });
        }

        #region Database API Calls

        // Pass the runner into the Coroutine so it can update the session properties

        private System.Collections.IEnumerator RegisterRaceInDatabaseCoroutine(NetworkRunner runner, string roomName, string lobbyName, CreateRaceConfig _config)
        {
            string url = $"http://192.168.2.49:8001/api/races/create";
            Debug.Log($"[Database] url : {url}");

            // 1. Instantiate the local object mapping directly to the Python API requirements
            var payloadObj = new CreateRacePayload
            {
                lobby_name = lobbyName,
                room_name = roomName,
                track_level_id = _config.RacePlatformLevel,
                track_id = _config.TrackId,
                race_type = _config.RaceType,
                target_laps = _config.LapTarget,
                pre_race_secs = _config.PreRaceSecs,
                accept_contestants_secs = _config.AcceptContestantsSecs,
                countdown_secs = _config.CountdownSecs
            };

            // 2. Serialize safely using Newtonsoft
            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payloadObj);
            Debug.Log($"[Database] jsonPayload : {jsonPayload}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Parse the JSON response from your Flask API to get the new GUID
                    var responseJson = Newtonsoft.Json.Linq.JObject.Parse(request.downloadHandler.text);
                    string generatedRaceId = responseJson["race_id"]?.ToString();

                    // Inject the UUID directly into the live Fusion Session Properties
                    if (!string.IsNullOrEmpty(generatedRaceId) && runner != null && runner.SessionInfo != null)
                    {
                        var updatedProps = new Dictionary<string, SessionProperty>
                {
                    { "raceId", generatedRaceId } // This key is now available to all clients
                };

                        runner.SessionInfo.UpdateCustomProperties(updatedProps);
                        Debug.Log($"[ServerManager] Live Session Properties updated with Race ID: {generatedRaceId}");
                    }
                }
                else
                {
                    Debug.LogError($"[ServerManager] Failed to create race in DB: {request.error}");
                }
            }
        }


        public static SceneRef GetSceneRefFromPath(string scenePath)
        {
            // 1. Resolve the Unity build index from the specific asset path
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            // 2. Ensure the scene actually exists in the Build Settings
            if (buildIndex >= 0)
            {
                // 3. Convert the valid build index into a Fusion SceneRef
                return SceneRef.FromIndex(buildIndex);
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to create SceneRef: '{scenePath}' is not in the Build Settings.");
                return default;
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