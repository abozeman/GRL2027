using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using M2MqttUnity;
using Fusion;
using Assets.CryptoKartz.Scripts.Utils;
using System.Runtime.ConstrainedExecution;

namespace Assets.CryptoKartz.Scripts.Managers
{
    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class CarControlDataLivePublisher : M2MqttUnityClientNetwork
    {
        private List<string> eventMessages = new List<string>();
       
        //Car Steering/Throttle
        public float Steering { get; set; }
        public float Throttle { get; set; }

        [SerializeField] public string vid;



        public void setControl(float steering, float throttle)
        {

            try
            {
                StartCoroutine(ControlPublish(steering, throttle));
            }
            catch (Exception e)
            {
                Debug.Log("CarControlPublisher setControl Exception: " + e);
            }

        }

        IEnumerator ControlPublish(float steering, float throttle)
        {
            float steeringToSend = 0.0f;
            float throttleToSend = 0.0f;

            steeringToSend = steering;
            throttleToSend = throttle;

            try
            {
                client.Publish(string.Format("car.cc.{0}", vid), System.Text.Encoding.UTF8.GetBytes("{\"steering\": \"" + steeringToSend + "\",\"throttle\": \"" + throttleToSend + "\"}"));
            }
            catch (Exception e)
            {

                Debug.Log("ControlPublish Exception: " + e);
                
            }

            yield return null;


        }


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
        /// Set broker address.
        /// </summary>
        /// <param name="brokerAddress">The broker address.</param>
        //public void SetBrokerAddress(string brokerAddress)
        //{
        //    this.brokerAddress = brokerAddress;
        //}

        /// <summary>
        /// Set broker port.
        /// </summary>
        /// <param name="brokerPort">The broker port.</param>
        //public void SetBrokerPort(string brokerPort)
        //{
        //    int.TryParse(brokerPort, out this.brokerPort);
        //}

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
            Debug.Log("CarControlDataLivePublisher CONNECTION LOST!");
            UnsubscribeTopics();
        }
        #endregion

        #region Subscription/Unsubscription
        protected override void SubscribeTopics()
        {
        }

        protected override void UnsubscribeTopics()
        {
        }

        #endregion

        #endregion
        
        public void Spawned()
        {
        }
      
        protected override void DecodeMessage(string topic, byte[] message)
        {
            try
            {
                string msg = System.Text.Encoding.UTF8.GetString(message);
                StoreMessage(msg);
            }
            catch (Exception)
            {
                //Debug.Log("EXCEPTION: " + e.Message);
            }

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