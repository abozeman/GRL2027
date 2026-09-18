using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.GRL.Scripts.Models
{

    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using UnityEngine;

    [Serializable]
    public class GeoJsonTrackCollection
    {
        [JsonProperty("type")]
        public string Type; // "FeatureCollection"

        [JsonProperty("properties")]
        public TrackProperties Properties;

        [JsonProperty("features")]
        public List<TrackFeature> Features;
    }

    [Serializable]
    public class TrackProperties
    {
        [JsonProperty("length")]
        public float Length;

        [JsonProperty("width")]
        public float Width;

        [JsonProperty("distance")]
        public float Distance;
    }

    [Serializable]
    public class TrackFeature
    {
        [JsonProperty("type")]
        public string Type; // "Feature"

        [JsonProperty("properties")]
        public FeatureTypeProperty Properties;

        [JsonProperty("geometry")]
        public FeatureGeometry Geometry;
    }

    [Serializable]
    public class FeatureTypeProperty
    {
        [JsonProperty("type")]
        public string Type; // "outer_boundary", "inner_boundary", "start_line"
    }

    [Serializable]
    public class FeatureGeometry
    {
        [JsonProperty("type")]
        public string GeometryType; // "Polygon" or "LineString"

        [JsonProperty("coordinates")]
        public Newtonsoft.Json.Linq.JToken Coordinates;
    }


    public class TrackDefinition
    {
        public Vector2[] OuterBoundary;
        public Vector2[] InnerBoundary;
        public Vector2[] StartLine;
        public TrackProperties Metadata;

        public static TrackDefinition Parse(string jsonString)
        {
            var trackData = JsonConvert.DeserializeObject<GeoJsonTrackCollection>(jsonString);
            var track = new TrackDefinition { Metadata = trackData.Properties };

            foreach (var feature in trackData.Features)
            {
                string featureType = feature.Properties?.Type;
                string geomType = feature.Geometry.GeometryType;

                if (geomType == "Polygon")
                {
                    // Polygon coordinates: [[[x, y], [x, y], ...]]
                    var polyCoords = feature.Geometry.Coordinates.ToObject<List<List<List<float>>>>();
                    Vector2[] points = ConvertCoordsToVector2(polyCoords[0]);

                    if (featureType == "outer_boundary") track.OuterBoundary = points;
                    else if (featureType == "inner_boundary") track.InnerBoundary = points;
                }
                else if (geomType == "LineString")
                {
                    // LineString coordinates: [[x, y], [x, y], ...]
                    var lineCoords = feature.Geometry.Coordinates.ToObject<List<List<float>>>();
                    Vector2[] points = ConvertCoordsToVector2(lineCoords);

                    if (featureType == "start_line") track.StartLine = points;
                }
            }
            return track;
        }

        private static Vector2[] ConvertCoordsToVector2(List<List<float>> rawCoords)
        {
            Vector2[] result = new Vector2[rawCoords.Count];
            for (int i = 0; i < rawCoords.Count; i++)
            {
                result[i] = new Vector2(rawCoords[i][0], rawCoords[i][1]);
            }
            return result;
        }
    }
}
