using UnityEngine;
using Fusion;
using RestClient.Scripts.Clients;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;
using System;

public class TrackGenerator : NetworkBehaviour, ITrackAPI
{
    public RestClientTrackAPI RestClientTrackGenerator;
    public GameObject TrackExterior;
    public GameObject TrackInterior;
    public GameObject TrackStartLine;

    [Networked, Capacity(20)]
    public string TrackId { get; set; }

    [Networked]
    public int LevelId { get; set; }

    public bool trackIsRendered { get; set; } = false;
    private LineRenderer m_exteriorLineRenderer;
    private LineRenderer m_interiorLineRenderer;
    private LineRenderer m_startLineRenderer;
    private TrackDefinition m_trackDefinition;

    private Mesh TrackMesh { get; set; }

    [Networked]
    [Capacity(60)]
    public NetworkArray<Vector2> ExteriorCoordinates { get; }
    [Networked] public int ExteriorCoordinatesLength { get; set; }

    [Networked]
    [Capacity(60)]
    public NetworkArray<Vector2> InteriorCoordinates { get; }
    [Networked] public int InteriorCoordinatesLength { get; set; }
    [Networked]
    [Capacity(4)]
    public NetworkArray<Vector2> StartlineCoordinates { get; }
    [Networked] public int StartlineCoordinatesLength { get; set; }

    public float renderDelay { get; private set; } = .1f;

    private ChangeDetector _changes;

    public void OnTrackDefinitionUpdate(TrackDefinition m_trackDefinition)
    {
        if (m_trackDefinition == null) return;

        if (m_trackDefinition.geometry.coordinates[0].Length > 0)
        {
            ExteriorCoordinatesLength = m_trackDefinition.geometry.coordinates[0].Length;
            for (var i = 0; i < ExteriorCoordinatesLength; i++)
            {
                ExteriorCoordinates.Set(i, new Vector2(m_trackDefinition.geometry.coordinates[0][i][0], m_trackDefinition.geometry.coordinates[0][i][1]));
            }
        }

        if (m_trackDefinition.geometry.coordinates[1].Length > 0)
        {
            InteriorCoordinatesLength = m_trackDefinition.geometry.coordinates[1].Length;
            for (var i = 0; i < InteriorCoordinatesLength; i++)
            {
                InteriorCoordinates.Set(i, new Vector2(m_trackDefinition.geometry.coordinates[1][i][0], m_trackDefinition.geometry.coordinates[1][i][1]));
            }
        }

        if (m_trackDefinition.geometry.coordinates[2].Length > 0)
        {
            StartlineCoordinatesLength = m_trackDefinition.geometry.coordinates[2].Length;
            for (var i = 0; i < StartlineCoordinatesLength; i++)
            {
                StartlineCoordinates.Set(i, new Vector2(m_trackDefinition.geometry.coordinates[2][i][0], m_trackDefinition.geometry.coordinates[2][i][1]));
            }
        }


    }

    public TrackDefinition GetTrackDefinition(NetworkArray<Vector2> _exteriorCoordinates, NetworkArray<Vector2> _interiorCoordinates, NetworkArray<Vector2> _startlineCoordinates, int _exteriorCoordinatesLength, int _interiorCoordinatesLength, int _startlineCoordinatesLength)
    {
        TrackDefinition trackDefinition = new TrackDefinition();
        trackDefinition.geometry = new Geometry();
        trackDefinition.properties = new Properties();

        var elrpc = _exteriorCoordinatesLength;
        var ilrpc = _interiorCoordinatesLength;
        var slrpc = _startlineCoordinatesLength;

        trackDefinition.geometry.coordinates = new float[3][][] {
            new float[elrpc][],
            new float[ilrpc][],
            new float[slrpc][],
        };


        try
        {
            if (_exteriorCoordinatesLength > 0)
            {
                for (var i = 0; i < _exteriorCoordinatesLength; i++)
                {
                    trackDefinition.geometry.coordinates[0][i] = new float[2];
                    trackDefinition.geometry.coordinates[0][i][0] = _exteriorCoordinates[i].x;
                    trackDefinition.geometry.coordinates[0][i][1] = _exteriorCoordinates[i].y;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }

        if (_interiorCoordinatesLength > 0)
        {
            for (var i = 0; i < _interiorCoordinatesLength; i++)
            {
                trackDefinition.geometry.coordinates[1][i] = new float[2];
                trackDefinition.geometry.coordinates[1][i][0] = _interiorCoordinates[i].x;
                trackDefinition.geometry.coordinates[1][i][1] = _interiorCoordinates[i].y;
            }
        }

        if (_startlineCoordinatesLength > 0)
        {
            for (var i = 0; i < _startlineCoordinatesLength; i++)
            {
                trackDefinition.geometry.coordinates[2][i] = new float[2];
                trackDefinition.geometry.coordinates[2][i][0] = _startlineCoordinates[i].x;
                trackDefinition.geometry.coordinates[2][i][1] = _startlineCoordinates[i].y;
            }
        }

        return trackDefinition;


    }

    void ITrackAPI.OnTrackDefinitionReceived(TrackDefinition trackDefinition)
    {
        m_trackDefinition = trackDefinition;
        OnTrackDefinitionUpdate(m_trackDefinition);
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        if (Runner.IsServer)
        {

            if (string.IsNullOrEmpty(TrackId)) return;
            RestClientTrackGenerator.RegisterGetTrackDefinitionCompleteListener(this);

            try
            {
                RestClientTrackGenerator.GetTrackDefinition(TrackId);

            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }
        }
       
    }

    //public void Reparent()
    //{

    //    GameObject _trackLevelObject = GameObject.Find("RaceLevelsV2");


    //    if (!Runner.IsServer)
    //    {
    //        switch (LevelId)
    //        {
    //            case 1:
    //                _trackLevelObject = GameObject.Find("RaceLevelsV2/Level_1");
    //                break;
    //            case 2:
    //                _trackLevelObject = GameObject.Find("RaceLevelsV2/Level_2");
    //                break;
    //            case 3:
    //                _trackLevelObject = GameObject.Find("RaceLevelsV2/Level_3");
    //                break;
    //            case 4:
    //                _trackLevelObject = GameObject.Find("RaceLevelsV2/Level_4");
    //                break;
    //            default:
    //                break;
    //        }

    //        transform.SetParent(_trackLevelObject.transform);
    //        transform.SetLocalPositionAndRotation(new Vector3(0,0,0.003f), transform.rotation);
    //        transform.localScale = new Vector3(.05f, .05f, .05f);

    //    }
    //}

    /// <summary>
    /// Fixed update network.
    /// </summary>
    public override void FixedUpdateNetwork()
    {

        foreach (string propertyName in _changes.DetectChanges(this))
        {
            switch (propertyName)
            {
                case nameof(ExteriorCoordinates):
                    if (m_trackDefinition != null)
                    {
                        RenderTrack(m_trackDefinition, transform.localScale);
                    }

                    break;
                case nameof(TrackId):
                    try
                    {
                        RestClientTrackGenerator.GetTrackDefinition(TrackId);

                    }
                    catch (System.Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                    break;

            }
        }
       
    }

    public override void Render()
    {
        if (!trackIsRendered && ExteriorCoordinatesLength > 0)
        {
            RenderTrack(GetTrackDefinition(ExteriorCoordinates, InteriorCoordinates, StartlineCoordinates, ExteriorCoordinatesLength, InteriorCoordinatesLength, StartlineCoordinatesLength), transform.localScale);
        }
    }

    private void RenderTrack(TrackDefinition trackDefinition, Vector3 scale)
    {
        scale = new Vector3(1f, 1f, 1f);
        m_exteriorLineRenderer = TrackExterior.GetComponent<LineRenderer>();
        m_exteriorLineRenderer.startWidth = .05f;
        m_exteriorLineRenderer.endWidth = .05f;

        m_exteriorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        m_exteriorLineRenderer.positionCount = trackDefinition.geometry.coordinates[0].Length;

        for (var i = 0; i < trackDefinition.geometry.coordinates[0].Length; i++)
        {
            var point = trackDefinition.geometry.coordinates[0][i];
            if (point != null) m_exteriorLineRenderer.SetPosition(i, new Vector3(point[0] * scale.x, (point[1]) * scale.y, 0f));
            new WaitForSeconds(renderDelay);
        }

        m_interiorLineRenderer = TrackInterior.GetComponent<LineRenderer>();
        m_interiorLineRenderer.startWidth = .05f;
        m_interiorLineRenderer.endWidth = .05f;

        m_interiorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        m_interiorLineRenderer.positionCount = trackDefinition.geometry.coordinates[1].Length;

        for (var i = 0; i < trackDefinition.geometry.coordinates[1].Length; i++)
        {
            var point = trackDefinition.geometry.coordinates[1][i];
            if (point != null)
            {
                m_interiorLineRenderer.SetPosition(i, new Vector3(point[0] * scale.x, point[1] * scale.y, 0f));
                new WaitForSeconds(renderDelay);
            }
        }

        if (trackDefinition.geometry.coordinates.Length < 3)
        {
            TrackStartLine.SetActive(false);
            return;
        }

        try
        {
            TrackStartLine.SetActive(true);
            m_startLineRenderer = TrackStartLine.GetComponent<LineRenderer>();
            m_startLineRenderer.startWidth = .05f;
            m_startLineRenderer.endWidth = .05f;

            m_startLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            m_startLineRenderer.positionCount = trackDefinition.geometry.coordinates[2].Length;

            for (var i = 0; i < trackDefinition.geometry.coordinates[2].Length; i++)
            {
                var point = trackDefinition.geometry.coordinates[2][i];
                if (point != null) m_startLineRenderer.SetPosition(i, new Vector3(point[0] * scale.x, point[1] * scale.y, 0f));
                new WaitForSeconds(renderDelay);

            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }


        trackIsRendered = true;

    }


    //public List<DelaunayTriangle> TriangulatePolygons()
    //{

    //    List<PolygonPoint> outerPolygonPoints = new List<PolygonPoint>();
    //    List<PolygonPoint> innerPolygonPoints = new List<PolygonPoint>();

    //    // The data type 'Polygon' is part of the external library.
    //    // The library handles converting the bounded area into a list of triangles.
    //    Vector2[] exteriorCoords = new Vector2[ExteriorCoordinatesLength];
    //    ExteriorCoordinates.CopyTo(exteriorCoords);
    //    List<Vector2> outerPoints2D = new List<Vector2>();
    //    outerPoints2D.AddRange(exteriorCoords);

    //    Vector2[] interiorCoords = new Vector2[InteriorCoordinatesLength];
    //    InteriorCoordinates.CopyTo(interiorCoords);
    //    List<Vector2> innerPoints2D = new List<Vector2>();
    //    innerPoints2D.AddRange(interiorCoords);

    //    foreach (var outerPont in outerPoints2D)
    //    {
    //        outerPolygonPoints.Add(new PolygonPoint(outerPont.x, outerPont.y));
    //    }

    //    foreach (var innerPont in innerPoints2D)
    //    {
    //        innerPolygonPoints.Add(new PolygonPoint(innerPont.x, innerPont.y));
    //    }

    //    Polygon trackPolygon = new Polygon(outerPolygonPoints);
    //    Polygon holePolygon = new Polygon(innerPolygonPoints);
    //    trackPolygon.AddHole(holePolygon);

    //    List<DelaunayTriangle> triangles = (List<DelaunayTriangle>)trackPolygon.Triangles;

    //    return triangles;

    //}

    //public void GenerateMesh(List<DelaunayTriangle> triangles)
    //{
    //    TrackMesh = new Mesh();
    //    List<Vector3> vertices3D = new List<Vector3>();
    //    List<int> triangleIndices = new List<int>();
    //    Dictionary<Vector2, int> vertexMap = new Dictionary<Vector2, int>();
    //    int vertexCount = 0;

    //    // 1. Collect Vertices and Triangle Indices
    //    foreach (var tri in triangles)
    //    {
    //        for (int i = 0; i < 3; i++)
    //        {
    //            Vector2 p2D = new Vector2((float)tri.Points[i].X, (float)(tri.Points[i].Y)); // Get the 2D point from the triangle

    //            if (!vertexMap.ContainsKey(p2D))
    //            {
    //                // New vertex: map 2D point to an index and store the 3D position
    //                vertexMap.Add(p2D, vertexCount);

    //                // Convert 2D back to 3D (assuming XZ plane)
    //                vertices3D.Add(new Vector3(p2D.x, 0f, p2D.y));
    //                vertexCount++;
    //            }

    //            // Add the index to the triangle list
    //            triangleIndices.Add(vertexMap[p2D]);
    //        }
    //    }

    //    // 2. Create the Unity Mesh

    //    TrackMesh.vertices = vertices3D.ToArray();
    //    TrackMesh.triangles = triangleIndices.ToArray();

    //    // 3. Calculate Other Mesh Data
    //    TrackMesh.RecalculateNormals(); // Essential for lighting
    //    TrackMesh.RecalculateBounds();  // Essential for culling

    //    // For simple flat meshes, UVs can be projected from the 2D coordinates
    //    Vector2[] uvs = new Vector2[vertices3D.Count];
    //    for (int i = 0; i < vertices3D.Count; i++)
    //    {
    //        // Simple UV mapping based on XZ plane coordinates
    //        uvs[i] = new Vector2(vertices3D[i].x, vertices3D[i].z);
    //    }
    //    TrackMesh.uv = uvs;

    //    // 4. Apply the Mesh to the GameObject
    //    GetComponent<MeshFilter>().mesh = TrackMesh;
    //    // You'll also need a MeshRenderer with a material to see it.

    //}



}
