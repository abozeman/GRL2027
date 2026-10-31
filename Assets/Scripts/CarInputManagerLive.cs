using Assets.Scripts.Managers;
using Fusion;
using UnityEngine;

namespace Assets.GRL.Scripts
{
    public class CarInputManagerLive : NetworkBehaviour
    {
        private float steeringInput;
        private float throttleInput;

        [SerializeField] public CarControlDataLivePublisher _carControlPublisher;

        // THE MOTION GATE: Flag to detach MQTT and use Unity physics for WebXR
        [Networked] public NetworkBool IsSimulated { get; set; }

        // Adjust these to make the simulated car feel right in the browser
        public float MoveSpeed = 15f;
        public float TurnSpeed = 60f;

        public override void FixedUpdateNetwork()
        {
            // Both physical and simulated cars read input from the driver's client
            if (GetInput<Player.PlayerInputProvider.CarInput>(out var input) == false) return;

            throttleInput = input.carControlValue.y;
            steeringInput = input.carControlValue.x;

            if (IsSimulated)
            {
                // WEBXR SIMULATOR LOGIC: Server moves the NetworkTransform directly
                if (Runner.IsServer)
                {
                    var nTransform = GetComponent<NetworkTransform>();
                    if (nTransform != null)
                    {
                        // Basic arcade physics for the browser simulator
                        Vector3 moveVector = nTransform.transform.forward * throttleInput * MoveSpeed * Runner.DeltaTime;
                        nTransform.transform.position += moveVector;
                        nTransform.transform.Rotate(0, steeringInput * TurnSpeed * Runner.DeltaTime, 0);
                    }
                }
            }
            else
            {
                // PHYSICAL CAR LOGIC: Route directly to MQTT
                if (_carControlPublisher != null)
                {
                    _carControlPublisher.setControl(steeringInput, throttleInput);
                }
            }
        }
    }
}