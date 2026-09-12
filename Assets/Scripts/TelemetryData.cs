using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

[Serializable]
public class TelemetryData
{
    public string type;
    public string vid;
    public float posX;
    public float posY;
    public float posZ;
    public float velX;
    public float velZ;
    public float rotW;
    public float rotX;
    public float rotY;
    public float rotZ;
    public float steeringAngle;
    public float throttle;

    // New simplified constructor
    public TelemetryData(string jsonString)
    {
        try
        {
            // Deserialize directly into this object instance
            JsonConvert.PopulateObject(jsonString, this);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse Telemetry: {e.Message}");
        }
    }

    //public TelemetryData (string jsonString)
    //{

    //    Dictionary<String, String> telemetryData2 = CreateFromJSON("{\"TelemetryData\":" + jsonString + "}");
    //    Dictionary<String, String> telemetryData3 = CreateFromJSON(telemetryData2["TelemetryData"]);

    //    this.type = telemetryData3["type"];
    //    this.vid = telemetryData3["vid"];
    //    this.posX = float.Parse(telemetryData3["posX"]);
    //    this.posY = float.Parse(telemetryData3["posY"]);
    //    this.posZ = float.Parse(telemetryData3["posZ"]);
    //    this.velX = float.Parse(telemetryData3["velX"]);
    //    this.velZ = float.Parse(telemetryData3["velZ"]);
    //    this.rotW = float.Parse(telemetryData3["rotW"]);
    //    this.rotX = float.Parse(telemetryData3["rotX"]);
    //    this.rotY = float.Parse(telemetryData3["rotY"]);
    //    this.rotZ = float.Parse(telemetryData3["rotZ"]);
    //    this.steeringAngle = float.Parse(telemetryData3["steeringAngle"]);
    //    this.throttle = float.Parse(telemetryData3["throttle"]);


    //}

    /// <summary>
    /// Creates from JSON.
    /// </summary>
    /// <param name="jsonString">The json string.</param>
    /// <returns><![CDATA[Dictionary<String, String>]]></returns>
    public Dictionary<String, String> CreateFromJSON(string jsonString)
    {
        return JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonString);
    }

    /// <summary>
    /// Creates from JSON.
    /// </summary>
    /// <param name="jsonString">The json string.</param>
    /// <returns><![CDATA[Dictionary<String, String>]]></returns>
    public static Dictionary<String, String> CreateFromJSON2(string jsonString)
    {
        return JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonString);
    }


}

