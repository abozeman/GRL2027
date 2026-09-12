using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateRaceTrackConfig
{
        public string RacePlatformLevel { get; set; }
        public string trackId { get; set; }

    public UpdateRaceTrackConfig() { }

    public UpdateRaceTrackConfig(string jsonString)
    {
        try
        {

            var obj = JsonConvert.DeserializeObject<UpdateRaceTrackConfig>(jsonString);
            this.RacePlatformLevel = obj.RacePlatformLevel;
            this.trackId = obj.trackId;

            Debug.Log($"UpdateRaceTrackConfig obj {obj.ToString()}");
        }
        catch (Exception e)
        {
            Debug.Log($"UpdateRaceTrackConfig Failure Message {e.Message}");
            Debug.Log($"UpdateRaceTrackConfig Failure Source {e.Source}");
            Debug.Log($"UpdateRaceTrackConfig Failure Stack {e.StackTrace}");
        }

    }

    /// <summary>
    /// Creates from JSON.
    /// </summary>
    /// <param name="jsonString">The json string.</param>
    /// <returns><![CDATA[Dictionary<String, String>]]></returns>
    public Dictionary<String, String> CreateFromJSON(string jsonString)
    {
        return JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonString);
    }
}

