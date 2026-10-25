using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Fusion;

namespace M2MqttUnity
{
    public class M2MqttUnityClientNetwork : MonoBehaviour // Stripped from SimulationBehaviour to act as a pure global Singleton
    {
        public static M2MqttUnityClientNetwork Instance { get; private set; }

        // Public event that passes the topic and raw byte array to any subscribers
        public event Action<string, byte[]> OnMessageReceived;
        public event Action ConnectionSucceeded;

        [Header("MQTT broker configuration")]
        public string brokerAddress = "192.168.2.49";
        public int brokerPort = 1883;
        public bool isEncrypted = false;

        [Header("Connection parameters")]
        public int connectionDelay = 500;
        public int timeoutOnConnection = MqttSettings.MQTT_CONNECT_TIMEOUT;
        public bool autoConnect = true;
        public string mqttUserName = null;
        public string mqttPassword = null;
        public string clientId = null;

        public MqttClient client;
        private List<MqttMsgPublishEventArgs> messageQueue1 = new List<MqttMsgPublishEventArgs>();
        private List<MqttMsgPublishEventArgs> messageQueue2 = new List<MqttMsgPublishEventArgs>();
        private List<MqttMsgPublishEventArgs> frontMessageQueue = null;
        private List<MqttMsgPublishEventArgs> backMessageQueue = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensures the connection survives scene transitions

            frontMessageQueue = messageQueue1;
            backMessageQueue = messageQueue2;
        }

        private void Start()
        {
            if (autoConnect) Connect();
        }

        public void Connect()
        {
            if (client == null || !client.IsConnected) StartCoroutine(DoConnect());
        }

        public void Disconnect()
        {
            if (client != null && client.IsConnected) StartCoroutine(DoDisconnect());
        }

        public void SubscribeToTopic(string topic)
        {
            if (client != null && client.IsConnected)
                client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        public void UnsubscribeFromTopic(string topic)
        {
            if (client != null && client.IsConnected)
                client.Unsubscribe(new string[] { topic });
        }

        private void Update()
        {
            if (Instance != this) return;
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();
        }

        private void ProcessMqttMessageBackgroundQueue()
        {
            try
            {
                foreach (MqttMsgPublishEventArgs msg in backMessageQueue)
                {
                    OnMessageReceived?.Invoke(msg.Topic, msg.Message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MQTT] Message dispatch failed: {e}");
            }
            finally
            {
                // This guarantees the queue is wiped every single time, 
                // preventing the infinite loop!
                backMessageQueue.Clear();
            }
        }

        private void SwapMqttMessageQueues()
        {
            frontMessageQueue = frontMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
            backMessageQueue = backMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
        }

        private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs msg)
        {
            frontMessageQueue.Add(msg);
        }

        public IEnumerator DoConnect()
        {
            yield return new WaitForSecondsRealtime(connectionDelay / 1000f);

            if (client == null)
            {
                client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.TLSv1_2 : MqttSslProtocols.None);
            }

            client.Settings.TimeoutOnConnection = timeoutOnConnection;
            if (string.IsNullOrEmpty(clientId)) clientId = Guid.NewGuid().ToString();

            try
            {
                client.Connect(clientId, mqttUserName, mqttPassword);
                if (client.IsConnected)
                {
                    client.MqttMsgPublishReceived += OnMqttMessageReceived;
                    Debug.Log($"[MQTT] Global Client Connected to {brokerAddress}");
                    ConnectionSucceeded?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MQTT] Connection Failed: {e}");
            }
        }

        private IEnumerator DoDisconnect()
        {
            yield return new WaitForEndOfFrame();
            if (client != null)
            {
                client.Disconnect();
                client.MqttMsgPublishReceived -= OnMqttMessageReceived;
                client = null;
            }
        }
    }
}