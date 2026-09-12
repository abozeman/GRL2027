using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class LapData
{
    public string type;
    public string vid;
    public string trackId;
    public int lap;
    public bool lapIsValid;
    public bool lapIsPerfect;
    public int invalidLapCount;
    public float overLap;
    public float offTrack;
    public List<string> lapTimes = new();
    
    

    public LapData(string jsonString)
    {

        Dictionary<string, string> lapData2 = CreateFromJSON("{\"LapData\":" + jsonString + "}");
        Dictionary<string, string> lapData3 = CreateFromJSON(lapData2["LapData"]);

        // "laptimes\": \"[1673094402.543392, 1673094419.1086211, 1673094434.249424, 1673094449.4291246, 1673094464.279381, 1673094481.5408165, 1673094496.1035337, 1673094510.894316]\"}"

        type = lapData3["type"];
        vid = lapData3["vid"];
        trackId = lapData3["track_id"];
        lap = int.Parse(lapData3["lap"]);
        lapIsValid = bool.Parse(lapData3["lapIsInvalid"]);
        //this.lapIsPerfect = bool.Parse(lapData3["lapIsPerfect"]);
        invalidLapCount = int.Parse(lapData3["invalid_lap_count"]);
        overLap = float.Parse(lapData3["overlap"]);
        offTrack = float.Parse(lapData3["offtrack"]);

        lapTimes = JsonConvert.DeserializeObject<List<string>>(lapData3["laptimes"]);
    }

    /// <summary>
    /// Creates from JSON.
    /// </summary>
    /// <param name="jsonString">The json string.</param>
    /// <returns><![CDATA[Dictionary<string, string>]]></returns>
    public Dictionary<string, string> CreateFromJSON(string jsonString)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
    }

    /// <summary>
    /// Get lap seconds.
    /// </summary>
    /// <returns>A string</returns>
    public string GetLapSeconds()
    {
        if (!(lapTimes.Count > 1)) return null;

        double valStart, valEnd, lapSeconds;
        var scoreboardTxt = new System.Text.StringBuilder();

        try
        {

            for (var i = 0; i < lapTimes.Count - 2; i++)
            {
                // getting parsed value
                valStart = double.Parse(lapTimes[i]);
                valEnd = double.Parse(lapTimes[i + 1]);
                lapSeconds = valEnd - valStart;

                scoreboardTxt.Append("Lap " + (i + 1) + ": " + string.Format("{0:N2}", lapSeconds) + "s<br>");

            }

            //Debug.Log(scoreboardTxt.ToString());
            return scoreboardTxt.ToString();

        }

        catch (FormatException)
        {
            Console.WriteLine("Can't Parsed");
        }

        return null;
    }
}
