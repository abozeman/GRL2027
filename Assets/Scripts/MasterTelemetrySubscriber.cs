using Assets.GRL.Scripts.Utils;
using Assets.Scripts.M2MqttUnity.Scripts;
using Fusion;
using Fusion.Sockets;
using M2MqttUnity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf; // Required for Protobuf parser
using Grl; // Your generated Protobuf namespace

namespace Assets.GRL.Scripts.Managers
{
    public class MasterTelemetrySubscriber : NetworkBehaviour
    {
        private List<string> eventMessages = new List<string>();

        //Manager Transform Data
        public Vector3 masterPosition;
        public Quaternion masterRotation;

        //Car Metadata
        public int CurrentLap = 0;
        public bool IsOffTrack;
        public bool IsOverlapping;
        public float Velocity;
        public string SessionName;

        public Dictionary<string, GameObject> cars = new Dictionary<string, GameObject>();

        public override void Spawned()
        {
            if (Runner.IsServer)
            {
                cars = new Dictionary<string, GameObject>();
                SessionName = Runner.SessionInfo.Name.ToLower();
                if (M2MqttUnityClientNetwork.Instance != null)
                {
                    M2MqttUnityClientNetwork.Instance.OnMessageReceived += DecodeMessage;
                    // Subscribing explicitly to the Protobuf 'vr' topic[cite: 3, 5]
                    M2MqttUnityClientNetwork.Instance.SubscribeToTopic($"car/telemetry/vr/{SessionName}/#");

                    // Note: You must ensure 'vracestate' and 'lapupdate' are either published under this 
                    // root or you explicitly subscribe to their specific topics here as well.
                }
                Debug.Log("MasterTelemetrySubscriber Spawned & Subscribed to Protobuf stream...");
            }
            else
            {
                Debug.Log("MasterTelemetrySubscriber Disabled Itself...");
                gameObject.SetActive(false);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Runner.IsServer && M2MqttUnityClientNetwork.Instance != null)
            {
                M2MqttUnityClientNetwork.Instance.OnMessageReceived -= DecodeMessage;
                M2MqttUnityClientNetwork.Instance.UnsubscribeFromTopic($"car/telemetry/vr/{SessionName}/#");
            }
        }

        public void AddCar(string vid, GameObject car)
        {
            Debug.Log($"MasterTelemetrySubscriber Adding New VID {vid}");
            cars.Add(vid, car);
        }

        private void handleLapUpdate(LapData lapData)
        {
            Debug.Log("lap: " + lapData.lap);
            foreach (string lapTime in lapData.lapTimes)
            {
                Debug.Log(lapTime);
            }
        }

        private void handleTelemetryData(Telemetry pbData)
        {
            var vid = pbData.Vid;

            // Map directly to Unity's coordinate system from Protobuf properties (PascalCase)
            masterPosition = new Vector3(pbData.PosX, cars.ContainsKey(vid) ? cars[vid].transform.localPosition.y : 0f, pbData.PosZ);
            masterRotation = new Quaternion(pbData.RotX, pbData.RotY, pbData.RotZ, pbData.RotW);

            if (cars.ContainsKey(vid))
            {
                cars[vid].transform.localPosition = masterPosition;
                cars[vid].transform.localRotation = masterRotation;
            }
            else
            {
                Debug.Log("MasterTelemetrySubscriber: Car with vid " + pbData.Vid + " not found in cars dictionary.");
            }
        }

        private void handleVRaceStateData(VRaceStateData vRaceStateData)
        {
            IsOffTrack = vRaceStateData.offtrackFlag;
            IsOverlapping = vRaceStateData.overlapFlag;
        }

        protected void DecodeMessage(string topic, byte[] message)
        {
            string[] topicSegments = topic.Split('/');
            if (topicSegments.Length < 5) return;

            var vid = topicSegments[4];
            if (!cars.ContainsKey(vid)) return;

            try
            {
                if (topic.Contains("telemetry"))
                {
                    // DIRECT BINARY DESERIALIZATION: Zero string allocation overhead[cite: 3]
                    Telemetry pbData = Telemetry.Parser.ParseFrom(message);
                    handleTelemetryData(pbData);
                }
                else
                {
                    // Fallback string conversion ONLY for legacy JSON event messages
                    string msgRaw = System.Text.Encoding.UTF8.GetString(message);

                    if (topic.Contains("vracestate"))
                    {
                        IsOffTrack = false;
                        IsOverlapping = false;
                        VRaceStateData vRaceStateData = new VRaceStateData(msgRaw);
                        handleVRaceStateData(vRaceStateData);
                        StoreMessage(msgRaw);
                    }
                    else if (topic.Contains("lapupdate"))
                    {
                        LapData lapData = new LapData(msgRaw);
                        CurrentLap = lapData.lap;
                        handleLapUpdate(lapData);
                        StoreMessage(msgRaw);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("MasterTelemetrySubscriber Decode EXCEPTION: " + e.Message);
            }
        }

        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        private void ProcessMessage(string msg) { }

        public override void FixedUpdateNetwork()
        {
            if (eventMessages.Count > 0)
            {
                foreach (string msg in eventMessages)
                {
                    ProcessMessage(msg);
                }
                eventMessages.Clear();
            }
        }
    }
}