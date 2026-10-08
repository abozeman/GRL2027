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
    public class MasterTelemetrySubscriber : MqttNetworkBehaviour
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

        public Dictionary<string, GameObject> cars;


        #region MQTT Client

        #region Broker Settings
        /// <summary>
        /// Set ClientId.
        /// </summary>
        /// <param name="clientId">The clientId.</param>
        public void SetClientId(string clientId)
        {
            int randomInt = UnityEngine.Random.Range(1, 10000);
            this.clientId = clientId + randomInt;
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
            Debug.Log("MasterTelemetrySubscriber Connecting to broker on " + brokerAddress + ":" + brokerPort.ToString() + "...\n");
        }

        protected override void OnConnected()
        {
            base.OnConnected();
            Debug.Log("MasterTelemetrySubscriber Connected to broker on " + brokerAddress + "\n");
            SubscribeTopics();
        }

        protected override void OnConnectionFailed(string errorMessage)
        {
            Debug.Log("MasterTelemetrySubscriber CONNECTION FAILED! " + errorMessage);
        }

        protected override void OnDisconnected()
        {
            Debug.Log("MasterTelemetrySubscriber Disconnected.");
        }

        protected override void OnConnectionLost()
        {
            Debug.Log("MasterTelemetrySubscriber CONNECTION LOST!");
            UnsubscribeTopics();
        }
        #endregion

        #region Subscription/Unsubscription
        protected override void SubscribeTopics()
        {
            

            client.Subscribe(new string[] { string.Format("car/telemetry/json/{0}/{1}", SessionName, "#") }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            //client.Subscribe(new string[] { string.Format("car/lapupdate/{0}", "#") }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
            //client.Subscribe(new string[] { string.Format("car/vracestate/{0}", "#") }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

        }

        protected override void UnsubscribeTopics()
        {
            //client.Unsubscribe(new string[] { string.Format("car/telemetry/vr/{0}", "#") });
            //client.Unsubscribe(new string[] { string.Format("car/lapupdate/{0}", "#") });
            //client.Unsubscribe(new string[] { string.Format("car/vracestate/{0}", "#") });
        }


        #endregion

        #endregion

        public override void Spawned()
        {


            if (Runner.IsServer)
            {
                cars = new Dictionary<string, GameObject>();
                SetClientId("MasterTelemetrySubscriber");
                SessionName = Runner.SessionInfo.Name.ToLower();
                base.Connect();
                Debug.Log("MasterTelemetrySubscriber Spawned & Started Connecting to broker...");
            }
            else
            {
                Debug.Log("MasterTelemetrySubscriber Disbaled Itself...");
                //this.gameObject.SetActive(false);
            }


           


        }

        public void AddCar(string vid, GameObject car)
        {
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

        protected override void DecodeMessage(string topic, byte[] message)
        {

            var vid = topic.Split('/')[4];
            Debug.Log("vid msgRaw: " + vid);

            string msgDebug = System.Text.Encoding.UTF8.GetString(message);
            //Debug.Log("MasterTelemetrySubscriber msgRaw: " + msgDebug);

            if (!cars.ContainsKey(vid)) return;


            try
            {
                string msgRaw = System.Text.Encoding.UTF8.GetString(message);
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
            Debug.Log("MasterTelemetrySubscriber Received: " + msg);
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
       
    }
}