using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCarConfig
{
        public string raceplatformlevel { get; set; }
        public string trackId { get; set; }
        public string vid { get; set; }

    public SpawnCarConfig() { }

    public SpawnCarConfig(string jsonString)
    {
        try
        {

            var obj = JsonConvert.DeserializeObject<SpawnCarConfig>(jsonString);
            this.raceplatformlevel = obj.raceplatformlevel;
            this.trackId = obj.trackId;
            this.vid = obj.vid;

            Debug.Log($"SpawnCarConfig obj {obj.ToString()}");


            //"{\r\n  \"sessionName\": \"OpenXR\",\r\n  \"customLobby\": \"GRLMROrlandoDev\",\r\n  \"port\": 27045,\r\n  \"raceType\": 300,\r\n  \"RacePlatformLevel\": \"1\",\r\n  \"trackId\": \"ovaltrack\"\r\n}"



        }
        catch (Exception e)
        {
            Debug.Log($"SpawnCarConfig Failure Message {e.Message}");
            Debug.Log($"SpawnCarConfig Failure Source {e.Source}");
            Debug.Log($"SpawnCarConfig Failure Stack {e.StackTrace}");
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

