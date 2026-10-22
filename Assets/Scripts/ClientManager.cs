using Assets.GRL.Scripts.managers;
using Fusion;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.GRL.Scripts.Managers
{
    public class ClientManager : ClientManagerBase
    {
        public async Task<StartGameResult> StartClient(string lobbyName, string raceName)
        {
            SceneRef sRef = GetSceneRefFromPath("Assets/Scenes/GRLGame.unity");

            // 1. Instantiate the Client Runner
            NetworkRunner runnerClient = GetRunner("Client");

            // 2. Start the game natively. Fusion handles the scene load and connection automatically.
            var result = await runnerClient.StartGame(new StartGameArgs()
            {
                CustomLobbyName = lobbyName,
                SessionName = raceName,
                GameMode = GameMode.Client, // Strictly Client mode
                SceneManager = runnerClient.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                Scene = sRef,
            });

            if (result.Ok)
            {
                Debug.Log($"[ClientManager] Successfully joined session {raceName} as a Client.");
            }
            else
            {
                Debug.LogError($"[ClientManager] Failed to join session: {result.ShutdownReason}");
            }

            return result;
        }

        private NetworkRunner GetRunner(string name)
        {
            // Ensure _runnerClientPrefab does not have a NetworkObject attached to it in the inspector
            var runner = Instantiate(_runnerClientPrefab);
            runner.name = name;
            runner.ProvideInput = true;
            return runner;
        }

        public SceneRef GetSceneRefFromPath(string scenePath)
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (buildIndex >= 0)
            {
                return SceneRef.FromIndex(buildIndex);
            }
            else
            {
                Debug.LogError($"Failed to create SceneRef: '{scenePath}' is not in the Build Settings.");
                return default;
            }
        }
    }
}