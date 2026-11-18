using Fusion;
using UnityEngine;
using System;
using System.Text;
using M2MqttUnity;

namespace Assets.GRL.Scripts
{
    public class SimulatedCarLocalizer : NetworkBehaviour
    {
        [SerializeField] private CarInputManagerLive _inputManager;

        public string RaceId = "default_race";
        public string Vid = "sim_car_01";
        
        [Header("Car Footprint (Meters)")]
        public float CarWidth = 0.20f;
        public float CarLength = 0.36f;

        [Header("Track Data (Populate on Spawn)")]
        public Vector2[] OuterBoundary;
        public Vector2[] InnerBoundary;
        public Vector2[] StartLine; // 2 points making up the line segment

        private Vector2 _lastPosition;
        private int _lapCount = 0;
        private bool _isInitialized = false;

        // JSON Structs mapping to your Python schemas
        [Serializable]
        private struct LapCrossPayload
        {
            public string vid;
            public string race_id;
            public double timestamp;
            public int lap_num;
        }

        [Serializable]
        private struct VRaceStatePayload
        {
            public string vid;
            public bool offtrackFlag;
            public bool overlapFlag;
            public double timestamp;
        }

        public override void Spawned()
        {
            if (Runner.IsServer)
            {
                _lastPosition = new Vector2(transform.position.x, transform.position.z);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // Only the server runs the localization, and only if it's a simulated car
            if (!Runner.IsServer || _inputManager == null || !_inputManager.IsSimulated) return;
            
            if (OuterBoundary == null || OuterBoundary.Length == 0) return; // Wait for track data

            Vector2 currentPos = new Vector2(transform.position.x, transform.position.z);
            double currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            // 1. Evaluate Start Line Crossing
            if (StartLine != null && StartLine.Length >= 2)
            {
                if (LineIntersects(_lastPosition, currentPos, StartLine[0], StartLine[1]))
                {
                    _lapCount++;
                    PublishLapCross(currentTimestamp);
                }
            }

            // 2. Evaluate Boundaries (Center Point for Off-Track)
            bool isOffTrack = !IsPointInDrivableArea(currentPos);
            bool isOverlapping = false;

            // 3. Evaluate Footprint (4 Corners for Overlap)
            if (!isOffTrack)
            {
                Vector2[] corners = GetCarCorners();
                foreach (Vector2 corner in corners)
                {
                    if (!IsPointInDrivableArea(corner))
                    {
                        isOverlapping = true;
                        break;
                    }
                }
            }

            // 4. Publish Violations
            if (isOffTrack || isOverlapping)
            {
                PublishVRaceState(isOffTrack, isOverlapping, currentTimestamp);
            }

            _lastPosition = currentPos;
        }

        // --- CORE GEOMETRY MATH (Replacing Shapely) ---
        
        private bool IsPointInDrivableArea(Vector2 point)
        {
            // Must be inside the outer boundary AND outside the inner boundary
            return IsPointInPolygon(point, OuterBoundary) && !IsPointInPolygon(point, InnerBoundary);
        }

        private bool IsPointInPolygon(Vector2 p, Vector2[] polygon)
        {
            // Standard Ray-Casting algorithm for Point-in-Polygon checks
            bool isInside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (((polygon[i].y > p.y) != (polygon[j].y > p.y)) &&
                   (p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        private bool LineIntersects(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            // Standard 2D Line Segment Intersection math
            float denominator = ((p1.x - p2.x) * (p3.y - p4.y)) - ((p1.y - p2.y) * (p3.x - p4.x));
            if (Mathf.Abs(denominator) < 0.001f) return false; // Parallel lines

            float t = (((p1.x - p3.x) * (p3.y - p4.y)) - ((p1.y - p3.y) * (p3.x - p4.x))) / denominator;
            float u = -(((p1.x - p2.x) * (p1.y - p3.y)) - ((p1.y - p2.y) * (p1.x - p3.x))) / denominator;

            return (t >= 0f && t <= 1f && u >= 0f && u <= 1f);
        }

        private Vector2[] GetCarCorners()
        {
            float hw = CarWidth / 2f;
            float hl = CarLength / 2f;
            
            // Local corners
            Vector3[] localCorners = {
                new Vector3(-hw, 0, -hl),
                new Vector3(hw, 0, -hl),
                new Vector3(hw, 0, hl),
                new Vector3(-hw, 0, hl)
            };

            Vector2[] worldCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 worldPos = transform.TransformPoint(localCorners[i]);
                worldCorners[i] = new Vector2(worldPos.x, worldPos.z);
            }
            return worldCorners;
        }

        // --- MQTT PUBLISHERS ---
        
        private void PublishLapCross(double timestamp)
        {
            LapCrossPayload payload = new LapCrossPayload
            {
                vid = Vid,
                race_id = RaceId,
                timestamp = timestamp,
                lap_num = _lapCount
            };
            string jsonString = JsonUtility.ToJson(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
            
            M2MqttUnityClientNetwork.Instance.client.Publish($"car/lap_cross/{RaceId}/{Vid}", bytes);
        }

        private void PublishVRaceState(bool offTrack, bool overlap, double timestamp)
        {
            VRaceStatePayload payload = new VRaceStatePayload
            {
                vid = Vid,
                offtrackFlag = offTrack,
                overlapFlag = overlap,
                timestamp = timestamp
            };
            string jsonString = JsonUtility.ToJson(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
            
            M2MqttUnityClientNetwork.Instance.client.Publish($"car/vracestate/{Vid}", bytes);
        }
    }
}