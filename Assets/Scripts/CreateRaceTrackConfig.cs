using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateRaceTrackConfig
{
    public string RacePlatformLevel { get; set; }
    public string trackId { get; set; }
    public string SessionName { get; set; }
    public string LobbyName { get; set; }

    public CreateRaceTrackConfig() { }

    public CreateRaceTrackConfig(string jsonString)
    {
        try
        {

            var obj = JsonConvert.DeserializeObject<CreateRaceTrackConfig>(jsonString);
            this.RacePlatformLevel = obj.RacePlatformLevel;
            this.trackId = obj.trackId;
            this.SessionName = obj.SessionName;
            this.LobbyName = obj.LobbyName;

            Debug.Log($"CreateRaceTrackConfig obj {obj.ToString()}");


            //"{\r\n  \"SessionName\": \"OpenXR\",\r\n  \"customLobby\": \"GRLMROrlandoDev\",\r\n  \"port\": 27045,\r\n  \"raceType\": 300,\r\n  \"RacePlatformLevel\": \"1\",\r\n  \"trackId\": \"ovaltrack\"\r\n}"



        }
        catch (Exception e)
        {
            Debug.Log($"CreateRaceTrackConfig Failure Message {e.Message}");
            Debug.Log($"CreateRaceTrackConfig Failure Source {e.Source}");
            Debug.Log($"CreateRaceTrackConfig Failure Stack {e.StackTrace}");
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

