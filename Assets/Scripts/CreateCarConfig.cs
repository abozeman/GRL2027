using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCarConfig
{
        public string RacePlatformLevel { get; set; }
        //public string trackId { get; set; }

    public CreateCarConfig() { }

    public CreateCarConfig(string jsonString)
    {
        try
        {

            var obj = JsonConvert.DeserializeObject<CreateRaceTrackConfig>(jsonString);
            this.RacePlatformLevel = obj.RacePlatformLevel;
            //this.trackId = obj.trackId;

            Debug.Log($"CreateCarConfig obj {obj.ToString()}");


            //"{\r\n  \"sessionName\": \"OpenXR\",\r\n  \"customLobby\": \"GRLMROrlandoDev\",\r\n  \"port\": 27045,\r\n  \"raceType\": 300,\r\n  \"RacePlatformLevel\": \"1\",\r\n  \"trackId\": \"ovaltrack\"\r\n}"



        }
        catch (Exception e)
        {
            Debug.Log($"CreateCarConfig Failure Message {e.Message}");
            Debug.Log($"CreateCarConfig Failure Source {e.Source}");
            Debug.Log($"CreateCarConfig Failure Stack {e.StackTrace}");
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

