using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRaceStateData
{
    public string type;
    public string vid;
    public int lap;
    public bool overlapFlag;
    public bool offtrackFlag;
    public float px;
    public float pz;


    public VRaceStateData(string jsonString)
    {
        Dictionary<String, String> vRaceStateData2 = CreateFromJSON("{\"VRaceStateData\":" + jsonString + "}");
        Dictionary<String, String> vRaceStateData3 = CreateFromJSON(vRaceStateData2["VRaceStateData"]);

        this.type = vRaceStateData3["type"];
        this.vid = vRaceStateData3["vid"];
        this.lap = int.Parse(vRaceStateData3["lap"]);
        this.overlapFlag = bool.Parse(vRaceStateData3["overlap_flag"]);
        this.offtrackFlag = bool.Parse(vRaceStateData3["offtrack_flag"]);
        this.px = float.Parse(vRaceStateData3["posX"]);
        this.pz = float.Parse(vRaceStateData3["posZ"]);

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

