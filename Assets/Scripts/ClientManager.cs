using Assets.CryptoKartz.Scripts.managers;
using Assets.CryptoKartz.Scripts.Utils;
using Fusion;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Application = UnityEngine.Application;

namespace Assets.CryptoKartz.Scripts.Managers
{

    public class ClientManager : ClientManagerBase
    {

        Fusion.NetworkRunner runnerClient;
        private SceneRef sRef;

        protected async Task Start()
        {
            // Start the client
            _ = StartClient();
        }


        public async Task<StartGameResult> StartClient()
        {
            sRef = GetSceneRefFromPath("Assets/Scenes/GRLGame.unity");

            int loadGRLTask = await LoadGRLAsync(sRef);

            StartGameResult startGRLTask = await StartSessionAsync("GRLGame", sRef);
            return startGRLTask;
        }

        public async Task<int> LoadGRLAsync(SceneRef sceneRef) // assume we return an int from this long running operation 
        {
            await SceneManager.LoadSceneAsync(sceneRef.AsIndex, LoadSceneMode.Single);
            return 1;
        }

        public async Task<StartGameResult> StartSessionAsync(string sessionName, SceneRef scene) // assume we return an int from this long running operation 
        {
            runnerClient = GetRunner("Client");

            var result = await StartSession(runnerClient, GameMode.Client, sessionName, "GRLOrlandoDev", scene);

            // Check if all went fine
            if (result.Ok)
            {

                Log.Debug($"Runner Start session success");
                //_instanceRunner.DestroySafely();
            }
            else
            {
                Log.Debug($"Runner Start Session failed");
            }

            return result;
        }

        private NetworkRunner GetRunner(string name)
        {

            var runner = Instantiate(_runnerClientPrefab);
            runner.name = name;
            runner.ProvideInput = true;

            return runner;
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        /// <param name="runner">The runnerServer.</param>
        /// <param name="gameMode">The game mode.</param>
        /// <param name="sessionName">The session name.</param>
        /// <returns><![CDATA[Task<StartGameResult>]]></returns>
        public Task<StartGameResult> StartSession(
            NetworkRunner runner,
            GameMode gameMode,
            string sessionName,
            string lobbyName,
            SceneRef scene
          )
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

        public SceneRef GetSceneRefFromPath(string scenePath)
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

    }

    
}