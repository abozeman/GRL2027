using Fusion;
using UnityEngine;
using System;
using System.Text;
using M2MqttUnity;

namespace Assets.GRL.Scripts
{
    public class SimulatedCarTelemetryPublisher : NetworkBehaviour
    {
        [SerializeField] private CarInputManagerLive _inputManager;

        // Define these in the inspector or dynamically assign them upon spawning
        public string RaceId = "default_race";
        public string Vid = "sim_car_01";

        // Control the broadcast rate (e.g., 30 Hz = ~0.033f)
        public float PublishRate = 0.033f;
        private float _timeSinceLastPublish = 0f;

        // 1. Create a lightweight struct tailored for Unity's JSON serializer
        [Serializable]
        private struct JsonTelemetryPayload
        {
            public string type;
            public string vid;
            public float posX;
            public float posZ;
            public float velX;
            public float velZ;
            public float rotW;
            public float rotX;
            public float rotY;
            public float rotZ;
            public double Timestamp;
        }

        public override void FixedUpdateNetwork()
        {
            // Only the server publishes telemetry, and only if the car is simulated
            if (!Runner.IsServer || _inputManager == null || !_inputManager.IsSimulated) return;

            _timeSinceLastPublish += Runner.DeltaTime;

            if (_timeSinceLastPublish >= PublishRate)
            {
                PublishTelemetry();
                _timeSinceLastPublish = 0f;
            }
        }

        private void PublishTelemetry()
        {
            if (M2MqttUnityClientNetwork.Instance == null) return;

            // 2. Construct the data object
            JsonTelemetryPayload payload = new JsonTelemetryPayload
            {
                type = "1",
                vid = Vid,
                posX = transform.position.x,
                posZ = transform.position.z,
                velX = 0f,
                velZ = 0f,
                rotW = transform.rotation.w,
                rotX = transform.rotation.x,
                rotY = transform.rotation.y,
                rotZ = transform.rotation.z,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0
            };

            // 3. Serialize to a standard JSON string
            string jsonString = JsonUtility.ToJson(payload);

            // 4. Convert the string directly to UTF-8 bytes
            byte[] rawBytes = Encoding.UTF8.GetBytes(jsonString);

            // 5. Publish to the specific Race ID and VID topics
            string topic = $"car/telemetry/vr/{RaceId}/{Vid}";
            M2MqttUnityClientNetwork.Instance.client.Publish(topic, rawBytes);
        }
    }
}