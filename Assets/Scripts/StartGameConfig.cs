using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartGameConfig
{
        public string sessionName { get; set; }
        public string customLobby { get; set; }
        public int port { get; set; }
        public int raceType { get; set; }
        public string level { get; set; }
        public string trackId { get; set; }

    public StartGameConfig() { }

    public StartGameConfig(string jsonString)
    {
        try
        {

            var obj = JsonConvert.DeserializeObject<StartGameConfig>(jsonString);
            this.sessionName = obj.sessionName;
            this.customLobby = obj.customLobby;
            this.port = obj.port;
            this.raceType = obj.raceType;
            this.level = obj.level;
            this.trackId = obj.trackId;

            Debug.Log($"AgentConfig obj {obj.ToString()}");


            //"{\r\n  \"SessionName\": \"OpenXR\",\r\n  \"customLobby\": \"GRLMROrlandoDev\",\r\n  \"port\": 27045,\r\n  \"raceType\": 300,\r\n  \"RacePlatformLevel\": \"1\",\r\n  \"trackId\": \"ovaltrack\"\r\n}"



        }
        catch (Exception e)
        {
            Debug.Log($"AgentConfig Failure Message {e.Message}");
            Debug.Log($"AgentConfig Failure Source {e.Source}");
            Debug.Log($"AgentConfig Failure Stack {e.StackTrace}");
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

