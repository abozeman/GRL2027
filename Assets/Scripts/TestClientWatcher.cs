using Fusion;
using UnityEngine;

public class TestClientWatcher : MonoBehaviour
{
    [Header("Assign your existing Client Runner Prefab")]
    public NetworkRunner ClientRunnerPrefab;
    
    private bool _clientStarted = false;

    void Update()
    {
        if (_clientStarted) return;

        // Poll the scene for active runners
        NetworkRunner[] activeRunners = FindObjectsByType<NetworkRunner>();
        
        foreach (var runner in activeRunners)
        {
            // Wait until the ServerManager has fully initialized the Server Runner
            if (runner.IsServer && runner.SessionInfo != null && runner.SessionInfo.IsValid)
            {
                _clientStarted = true;
                string activeSessionName = runner.SessionInfo.Name;
                JoinAsClient(activeSessionName);
                break;
            }
        }
    }

    private async void JoinAsClient(string sessionName)
    {
        Debug.Log($"[TestWatcher] Server detected on session {sessionName}. Booting Client...");
        
        NetworkRunner clientRunner = Instantiate(ClientRunnerPrefab);
        clientRunner.name = "Test_Client_Runner";
        clientRunner.ProvideInput = true;

        var clientArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            SceneManager = clientRunner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await clientRunner.StartGame(clientArgs);
        
        if (result.Ok)
        {
            Debug.Log($"[TestWatcher] Client successfully joined the test race: {sessionName}");
        }
        else
        {
            Debug.LogError($"[TestWatcher] Client failed to join: {result.ShutdownReason}");
        }
        
        /* Verification: Check the Unity Console for "[TestWatcher] Client successfully joined..." 
           after triggering the race creation from your web dashboard. */
    }
}