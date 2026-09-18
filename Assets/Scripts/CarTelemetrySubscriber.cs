using System;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using M2MqttUnity;
using Fusion;

namespace Assets.GRL.Scripts.Managers
{
    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class CarTelemetrySubscriber : M2MqttUnityClientNetwork
    {
        private List<string> eventMessages = new List<string>();
        private bool firstTime = true;
        [SerializeField] private GameObject frontRightWheel;
        [SerializeField] private GameObject frontLeftWheel;
        [SerializeField] private GameObject rearRightWheel;
        [SerializeField] private GameObject rearLeftWheel;

        //public GameObject warningCube;
        //public TTSSpeaker ttsSpeaker;
        public Vector3 startLineOffset = new Vector3(-1.29799998f, 1.07f, 0.138999999f);

        //Car Transform Data
        private Vector3 newCarPosition;
        private Vector3 oldCarPosition;
        private Quaternion newCarRotation;
        private Quaternion oldCarRotation;

        //Car Metadata
        [SerializeField] public string vid = "echoliveracer1";
        public int CurrentLap = 0;
        public bool IsOffTrack;
        public bool IsOverlapping;
        public float Velocity;
        private NetworkTransform carNetworkTransform;

        //Car Steering/Throttle
        public float Steering = 0;
        public float Throttle = 0;

        [SerializeField] public bool righthanded = false;

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
            client.Subscribe(new string[] { string.Format("car.telemetry.vr.{0}", vid) }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            client.Subscribe(new string[] { string.Format("car.lapupdate.{0}", vid) }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            client.Subscribe(new string[] { string.Format("car.vracestate.{0}", vid) }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

        }

        protected override void UnsubscribeTopics()
        {
            client.Unsubscribe(new string[] { string.Format("car.telemetry.vr.{0}", vid) });
            client.Unsubscribe(new string[] { string.Format("car.lapupdate.{0}", vid) });
            client.Unsubscribe(new string[] { string.Format("car.vracestate.{0}", vid) });
        }

        #endregion

        #endregion

        public void Spawned()
        {
            if (Runner.IsServer)
            {
                Debug.Log("CarManager Spawned");
                carNetworkTransform = GetComponent<NetworkTransform>();
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

            //var lapUpdateObject = transform.Find("LapUpdateObject");
            //lapUpdateObject.GetComponent<CarEventManager>().EventActive = true;

        }
        private void handleTelemetryData(TelemetryData telemetryData)
        {
            //Get The Raw Measurement First
            var carPosition = new Vector3(telemetryData.posX, telemetryData.posY, telemetryData.posZ);
            var carRotation = new Quaternion(telemetryData.rotX, telemetryData.rotY, telemetryData.rotZ, telemetryData.rotW);

            if (righthanded)
            {
                transform.rotation = Quaternion.LookRotation(-transform.forward, Vector3.up);

            }

            if (firstTime)
            {
                oldCarPosition = carPosition;
                oldCarRotation = carRotation;

                transform.SetLocalPositionAndRotation(carPosition, carRotation);

                firstTime = false;

            }
            else
            {
                newCarPosition = carPosition;
                newCarRotation = carRotation;

                transform.SetLocalPositionAndRotation(carPosition, carRotation);


                //After that oldCarposition = carPosition
                oldCarPosition = carPosition;
                oldCarRotation = carRotation;

            }

            //Transform for Car
            var angles = transform.rotation.eulerAngles;
            angles.y += 180f;
            angles.z = 0.0f;
            angles.x = 0.0f;
            transform.rotation = Quaternion.Euler(angles.x, angles.y, angles.z);

            Velocity = getVelocity(telemetryData.velX, telemetryData.velZ);

            //Transform Front Left and Right Wheels for steering
            var frwAngles = frontRightWheel.transform.localEulerAngles;
            var flwAngles = frontLeftWheel.transform.localEulerAngles;
            var rrwAngles = rearRightWheel.transform.localEulerAngles;
            var rlwAngles = rearLeftWheel.transform.localEulerAngles;
            frwAngles.y = telemetryData.steeringAngle * 45f;
            flwAngles.y = telemetryData.steeringAngle * 45f;

            if (Velocity > 0.0f)
            {
                frwAngles = spinWheels(frwAngles);
                flwAngles = spinWheels(flwAngles);
                rrwAngles = spinWheels(rrwAngles);
                rlwAngles = spinWheels(rlwAngles);
            }

            frontRightWheel.transform.localEulerAngles = frwAngles;
            frontLeftWheel.transform.localEulerAngles = flwAngles;
            rearRightWheel.transform.localEulerAngles = rrwAngles;
            rearLeftWheel.transform.localEulerAngles = rlwAngles;

        }
        private void handleVRaceStateData(VRaceStateData vRaceStateData)
        {
            //Debug.Log($"Offtrack || Overlap: {vRaceStateData.overlapFlag || vRaceStateData.offtrackFlag}");

            IsOffTrack = vRaceStateData.offtrackFlag;
            IsOverlapping = vRaceStateData.overlapFlag;

            if(IsOffTrack)
            {
                var offtrackObject = transform.Find("OffTrackObject");
                offtrackObject.GetComponent<CarEventManager>().EventActive = true;
            }

            if (IsOverlapping)
            {
                var overlapObject = transform.Find("OverlapObject");
                overlapObject.GetComponent<CarEventManager>().EventActive = true;
            }
        }

        private Vector3 spinWheels(Vector3 wheelAngles)
        {

            //Transform All wheels for moving
            if (wheelAngles.x > 360f)
            {
                wheelAngles.x = 0f;
            }
            else
            {
                wheelAngles.x += 1f;
            }

            return wheelAngles;
        }

        protected override void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msg = System.Text.Encoding.UTF8.GetString(message);
                //string msg = "{"type": "1", "vid": "grlv0telemetry", "posX": "0.85", "posZ": "-0.018", "velX": "-0.0", "velZ": "-0.003", "rotW": "0.987", "rotX": "-0.117", "rotY": "0.014", "rotZ": "0.105", "strAngle": "0.0", "strThrottle": "0.0"}"
                //Debug.Log("msg: " + msg);
                if (topic.Contains("vracestate"))
                {
                    IsOffTrack = false;
                    IsOverlapping = false;
                    //Debug.Log("msg: " + msg);
                    VRaceStateData vRaceStateData = new VRaceStateData(msg);
                    handleVRaceStateData(vRaceStateData);
                }

                if (topic.Contains("lapupdate"))
                {
                    LapData lapData = new LapData(msg);
                    CurrentLap = lapData.lap;

                    handleLapUpdate(lapData);
                }

                if (topic.Contains("telemetry"))
                {
                    TelemetryData telemetryData = new TelemetryData(msg);
                    Steering = telemetryData.steeringAngle;
                    Throttle = telemetryData.throttle;
                    handleTelemetryData(telemetryData);
                }

                StoreMessage(msg);
            }
            catch (Exception)
            {
                //Debug.Log("EXCEPTION: " + e.Message);
            }

        }

        private float getVelocity(float velx, float velz)
        {
            var velocity = Math.Sqrt(Math.Pow(velx, 2) + Math.Pow(velz, 2));
            return (float)velocity;
        }

        private void StoreMessage(string eventMsg)
        {
            eventMessages.Add(eventMsg);
        }

        private void ProcessMessage(string msg)
        {
            //Debug.Log("Received: " + msg);
        }

        /// <summary>
        /// Fixed update network.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            base.Update(); // call ProcessMqttEvents()

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