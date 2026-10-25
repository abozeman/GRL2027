using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt.Messages;
using M2MqttUnity;
using Fusion;
using Assets.GRL.Scripts.Utils;
using System.Runtime.ConstrainedExecution;

namespace Assets.GRL.Scripts.Managers
{
    [SimulationBehaviour(Modes = SimulationModes.Server)]
    public class CarControlDataLivePublisher : SimulationBehaviour
    {
        private List<string> eventMessages = new List<string>();
       
        //Car Steering/Throttle
        public float Steering { get; set; }
        public float Throttle { get; set; }

        [SerializeField] public string vid;

        public void Spawned()
        {
            if (Runner.IsServer)
            {

            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Runner.IsServer && M2MqttUnityClientNetwork.Instance != null)
            {
            }
        }

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
                M2MqttUnityClientNetwork.Instance.client.Publish(string.Format("car.cc.{0}", vid), System.Text.Encoding.UTF8.GetBytes("{\"steering\": \"" + steeringToSend + "\",\"throttle\": \"" + throttleToSend + "\"}"));
            }
            catch (Exception e)
            {

                Debug.Log("ControlPublish Exception: " + e);
                
            }

            yield return null;

        }


        protected void DecodeMessage(string topic, byte[] message)
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