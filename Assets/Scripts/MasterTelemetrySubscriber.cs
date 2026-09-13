using Assets.CryptoKartz.Scripts.Utils;
using Fusion;
using Fusion.Sockets;
using M2MqttUnity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Assets.CryptoKartz.Scripts.Managers
{
    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class MasterTelemetrySubscriber : M2MqttUnityClientNetwork, INetworkRunnerCallbacks
    {
        private List<string> eventMessages = new List<string>();

        //Manager Transform Data
        public Vector3 masterPosition;
        public Quaternion masterRotation;

        //Car Metadata
        [SerializeField] public string vid = "vid";
        public int CurrentLap = 0;
        public bool IsOffTrack;
        public bool IsOverlapping;
        public float Velocity;

        private NetworkTransform masterTransform;


        #region MQTT Client

        #region Broker Settings
        /// <summary>
        /// Set ClientId.
        /// </summary>
        /// <param name="clientId">The clientId.</param>
        public void SetClientId(string clientId)
        {
            this.clientId = clientId;
        }

        /// <summary>
        /// Set the encrypted.
        /// </summary>
        /// <param name="isEncrypted">If true, is encrypted.</param>
        public void SetEncrypted(bool isEncrypted)
        {
            this.isEncrypted = isEncrypted;
        }
        #endregion

        #region Connection Methods
        protected override void OnConnecting()
        {
            base.OnConnecting();
            Debug.Log("Connecting to broker on " + brokerAddress + ":" + brokerPort.ToString() + "...\n");
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            Debug.Log("Connected to broker on " + brokerAddress + "\n");
            SubscribeTopics();
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
            Debug.Log("CarPositionLiveSubscriber CONNECTION LOST!");
            UnsubscribeTopics();
        }
        #endregion

        #region Subscription/Unsubscription
        protected override void SubscribeTopics()
        {
            client.Subscribe(new string[] { string.Format("car/telemetry/json/#", vid) }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            //client.Subscribe(new string[] { string.Format("car/lapupdate/{0}", vid) }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            //client.Subscribe(new string[] { string.Format("car/vracestate/{0}", vid) }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

        }

        protected override void UnsubscribeTopics()
        {
            client.Unsubscribe(new string[] { string.Format("car/telemetry/json/#", vid) });
            //client.Unsubscribe(new string[] { string.Format("car/lapupdate/{0}", vid) });
            //client.Unsubscribe(new string[] { string.Format("car/vracestate/{0}", vid) });
        }


        #endregion

        #endregion

        public void Spawned()
        {
            Runner.AddCallbacks(this);
            if (Runner.IsServer)
            {
                Debug.Log("masterTelemetryManager Spawned");
            }


        }

        private void handleLapUpdate(LapData lapData)
        {
            Debug.Log("lap: " + lapData.lap);
            Debug.Log("laptimes: " + lapData.lapTimes);

            foreach (string lapTime in lapData.lapTimes)
            {
                Debug.Log(lapTime);
            }

        }
        private void handleTelemetryData(TelemetryData telemetryData)
        {
            //Get The Raw Measurement First
            masterPosition = new Vector3(telemetryData.posX, telemetryData.posY, telemetryData.posZ);
            masterRotation = new Quaternion(telemetryData.rotX, telemetryData.rotY, telemetryData.rotZ, telemetryData.rotW);
        }
        private void handleVRaceStateData(VRaceStateData vRaceStateData)
        {
            //Debug.Log($"Offtrack || Overlap: {vRaceStateData.overlapFlag || vRaceStateData.offtrackFlag}");

            IsOffTrack = vRaceStateData.offtrackFlag;
            IsOverlapping = vRaceStateData.overlapFlag;

        }

        protected override void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msgRaw = System.Text.Encoding.UTF8.GetString(message);
                //string msg = "{"type": "1", "vid": "grlv0telemetry", "posX": "0.85", "posZ": "-0.018", "velX": "-0.0", "velZ": "-0.003", "rotW": "0.987", "rotX": "-0.117", "rotY": "0.014", "rotZ": "0.105", "strAngle": "0.0", "strThrottle": "0.0"}"
                Debug.Log("msgRaw: " + msgRaw);
                //string msg = msgRaw.Replace("\\", "");
                //Debug.Log("msg: " + msg);
                //string cleanJson = JToken.Parse(msgRaw).ToString();
                //Debug.Log("cleanJson: " + cleanJson);


                if (topic.Contains("vracestate"))
                {
                    IsOffTrack = false;
                    IsOverlapping = false;
                    //Debug.Log("msg: " + msg);
                    VRaceStateData vRaceStateData = new VRaceStateData(msgRaw);
                    handleVRaceStateData(vRaceStateData);
                }

                if (topic.Contains("lapupdate"))
                {
                    LapData lapData = new LapData(msgRaw);
                    CurrentLap = lapData.lap;

                    handleLapUpdate(lapData);
                }

                if (topic.Contains("telemetry"))
                {
                    TelemetryData telemetryData = new TelemetryData(msgRaw);
                    handleTelemetryData(telemetryData);
                }

                StoreMessage(msgRaw);
            }
            catch (Exception e)
            {
                Debug.Log("TelemetryData EXCEPTION: " + e.Message);
            }

        }

        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        private void ProcessMessage(string msg)
        {
            Debug.Log("Received: " + msg);
        }

        /// <summary>
        /// Fixed update network.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            //base.Update(); // call ProcessMqttEvents()

            if (eventMessages.Count > 0)
            {
                foreach (string msg in eventMessages)
                {
                    ProcessMessage(msg);
                }
                eventMessages.Clear();
            }

        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnValidate()
        {

        }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
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

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
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

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }
    }
}