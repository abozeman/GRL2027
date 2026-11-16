using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Assets.GRL.Scripts.Models
{
    [Serializable]
    public class CreateRaceConfig
    {
        public string SessionName;
        public string LobbyName;
        public string RacePlatformLevel;
        public string TrackId;
        public string RaceType;
        public int LapTarget;


        // The dynamically assigned timing durations
        public int PreRaceSecs;
        public int AcceptContestantsSecs;
        public int CountdownSecs;

        public CreateRaceConfig(string jsonMsg)
        {
            try
            {
                JsonConvert.PopulateObject(jsonMsg, this);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Config] Failed to parse CreateRaceConfig JSON: {e.Message}");
            }
        }
    }
}