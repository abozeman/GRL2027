using Assets.GRL.Scripts.Utils;
using Assets.Scripts.M2MqttUnity.Scripts;
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
                    M2MqttUnityClientNetwork.Instance.SubscribeToTopic($"car/telemetry/json/{SessionName}/#");
                }
                Debug.Log("MasterTelemetrySubscriber Spawned & Started Connecting to broker...");
            }
            else
            {
                Debug.Log("MasterTelemetrySubscriber Disbaled Itself...");
                gameObject.SetActive(false);
            }

        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Runner.IsServer && M2MqttUnityClientNetwork.Instance != null)
            {
                M2MqttUnityClientNetwork.Instance.OnMessageReceived -= DecodeMessage;
                M2MqttUnityClientNetwork.Instance.UnsubscribeFromTopic($"car/telemetry/json/{SessionName}/#");
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
            Debug.Log("laptimes: " + lapData.lapTimes);

            foreach (string lapTime in lapData.lapTimes)
            {
                Debug.Log(lapTime);
            }

        }
        private void handleTelemetryData(TelemetryData telemetryData)
        {

            //var vid = "grlracer1";
            var vid = telemetryData.vid;

            //Get The Raw Measurement First
            masterPosition = new Vector3(telemetryData.posX, telemetryData.posY, telemetryData.posZ);
            masterRotation = new Quaternion(telemetryData.rotX, telemetryData.rotY, telemetryData.rotZ, telemetryData.rotW);

            if (cars.ContainsKey(vid))
            {
                cars[vid].transform.localPosition = masterPosition;
                cars[vid].transform.localRotation = masterRotation;
            }
            else
            {
                Debug.Log("MasterTelemetrySubscriber: Car with vid " + telemetryData.vid + " not found in cars dictionary.");
            }

            //Velocity = new Vector3(telemetryData.velX, 0, telemetryData.velZ).magnitude;
        }
        private void handleVRaceStateData(VRaceStateData vRaceStateData)
        {
            //Debug.Log($"Offtrack || Overlap: {vRaceStateData.overlapFlag || vRaceStateData.offtrackFlag}");

            IsOffTrack = vRaceStateData.offtrackFlag;
            IsOverlapping = vRaceStateData.overlapFlag;

        }

        protected void DecodeMessage(string topic, byte[] message)
        {

            // Safely check the topic length before splitting
            string[] topicSegments = topic.Split('/');
            if (topicSegments.Length < 5) return;

            var vid = topicSegments[4];
            if (!cars.ContainsKey(vid)) return;

            string msgRaw = System.Text.Encoding.UTF8.GetString(message);


            try
            {
                //Debug.Log("MasterTelemetrySubscriber msgRaw: " + msgRaw);

                if (topic.Contains("telemetry"))
                {
                    TelemetryData telemetryData = new TelemetryData(msgRaw);
                    handleTelemetryData(telemetryData);
                }

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

                

                StoreMessage(msgRaw);
            }
            catch (Exception e)
            {
                Debug.Log("MasterTelemetrySubscriber TelemetryData EXCEPTION: " + e.Message);
            }

        }

        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        private void ProcessMessage(string msg)
        {
            //Debug.Log("MasterTelemetrySubscriber Received: " + msg);
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

    }
}