using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackDefinition
{
    public string type { get; set; }
    public Geometry geometry { get; set; }
    public Properties properties { get; set; }
}

public class Geometry
{
    public string type { get; set; }

    public float[][][] coordinates { get; set; }
}

public class Properties
{
    public float length { get; set; }
    public float width { get; set; }
    public int distance { get; set; }
}