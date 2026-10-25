using UnityEngine;
using UnityEngine.SceneManagement;
using M2MqttUnity;

public class MQTTBootstrapper : MonoBehaviour
{
    [Tooltip("The exact name of the Server/Lobby scene in your Build Settings")]
    public string nextSceneName = "GRLServerScene";

    private void Start()
    {
        // Hook into the Singleton's connection event
        if (M2MqttUnityClientNetwork.Instance != null)
        {
            M2MqttUnityClientNetwork.Instance.ConnectionSucceeded += OnMqttConnected;
        }
        else
        {
            Debug.LogError("[Bootstrapper] MQTT Singleton not found in the scene!");
        }
    }

    private void OnMqttConnected()
    {
        Debug.Log("[Bootstrapper] MQTT Connected successfully. Transitioning to next scene...");

        // Unsubscribe immediately to prevent this from firing again if the connection drops and reconnects later
        if (M2MqttUnityClientNetwork.Instance != null)
        {
            M2MqttUnityClientNetwork.Instance.ConnectionSucceeded -= OnMqttConnected;
        }

        // Asynchronously load the Server Manager scene
        SceneManager.LoadSceneAsync(nextSceneName);
    }

    private void OnDestroy()
    {
        // Safety cleanup just in case the object is destroyed before connection completes
        if (M2MqttUnityClientNetwork.Instance != null)
        {
            M2MqttUnityClientNetwork.Instance.ConnectionSucceeded -= OnMqttConnected;
        }
    }
}