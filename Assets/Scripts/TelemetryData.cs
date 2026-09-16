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

}

