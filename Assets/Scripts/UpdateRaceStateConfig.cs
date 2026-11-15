using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateRaceStateConfig
{
        public string state { get; set; }

    public UpdateRaceStateConfig() { }

    public UpdateRaceStateConfig(string jsonString)
    {
        try
        {

            var obj = JsonConvert.DeserializeObject<UpdateRaceStateConfig>(jsonString);
            this.state = obj.state;

            Debug.Log($"UpdateRaceStateConfig obj {obj.ToString()}");
        }
        catch (Exception e)
        {
            Debug.Log($"UpdateRaceStateConfig Failure Message {e.Message}");
            Debug.Log($"UpdateRaceStateConfig Failure Source {e.Source}");
            Debug.Log($"UpdateRaceStateConfig Failure Stack {e.StackTrace}");
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

