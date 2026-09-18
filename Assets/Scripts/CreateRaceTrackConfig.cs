using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Assets.GRL.Scripts.Models
{
    [Serializable]
    public class CreateRaceTrackConfig
    {
        public string SessionName;
        public string LobbyName;
        public string RacePlatformLevel;
        public string TrackId;

        // Constructor that takes the raw MQTT JSON string and maps it to these variables
        public CreateRaceTrackConfig(string jsonMsg)
        {
            try
            {
                JsonConvert.PopulateObject(jsonMsg, this);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Config] Failed to parse CreateRaceTrackConfig JSON: {e.Message}");
            }
        }
    }
}