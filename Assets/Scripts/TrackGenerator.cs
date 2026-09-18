using Assets.GRL.Scripts.Models;
using Fusion;
using Meta.XR.MRUtilityKit;
using RestClient.Scripts.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public void OnTrackDefinitionUpdate(TrackDefinition trackDefinition)
    {
        if (trackDefinition == null) return;

        if (trackDefinition.OuterBoundary != null && trackDefinition.OuterBoundary.Length > 0)
        {
            ExteriorCoordinatesLength = trackDefinition.OuterBoundary.Length;
            for (var i = 0; i < ExteriorCoordinatesLength; i++)
            {
                ExteriorCoordinates.Set(i, trackDefinition.OuterBoundary[i]);
            }
        }

        if (trackDefinition.InnerBoundary != null && trackDefinition.InnerBoundary.Length > 0)
        {
            InteriorCoordinatesLength = trackDefinition.InnerBoundary.Length;
            for (var i = 0; i < InteriorCoordinatesLength; i++)
            {
                InteriorCoordinates.Set(i, trackDefinition.InnerBoundary[i]);
            }
        }

        if (trackDefinition.StartLine != null && trackDefinition.StartLine.Length > 0)
        {
            StartlineCoordinatesLength = trackDefinition.StartLine.Length;
            for (var i = 0; i < StartlineCoordinatesLength; i++)
            {
                StartlineCoordinates.Set(i, trackDefinition.StartLine[i]);
            }
        }
    }

    public TrackDefinition GetTrackDefinition(
        NetworkArray<Vector2> _exteriorCoordinates,
        NetworkArray<Vector2> _interiorCoordinates,
        NetworkArray<Vector2> _startlineCoordinates,
        int _exteriorCoordinatesLength,
        int _interiorCoordinatesLength,
        int _startlineCoordinatesLength)
    {
        TrackDefinition trackDefinition = new TrackDefinition();

        if (_exteriorCoordinatesLength > 0)
        {
            trackDefinition.OuterBoundary = new Vector2[_exteriorCoordinatesLength];
            for (var i = 0; i < _exteriorCoordinatesLength; i++)
            {
                trackDefinition.OuterBoundary[i] = _exteriorCoordinates[i];
            }
        }

        if (_interiorCoordinatesLength > 0)
        {
            trackDefinition.InnerBoundary = new Vector2[_interiorCoordinatesLength];
            for (var i = 0; i < _interiorCoordinatesLength; i++)
            {
                trackDefinition.InnerBoundary[i] = _interiorCoordinates[i];
            }
        }

        if (_startlineCoordinatesLength > 0)
        {
            trackDefinition.StartLine = new Vector2[_startlineCoordinatesLength];
            for (var i = 0; i < _startlineCoordinatesLength; i++)
            {
                trackDefinition.StartLine[i] = _startlineCoordinates[i];
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

        // 1. Exterior Boundary
        if (trackDefinition.OuterBoundary != null && trackDefinition.OuterBoundary.Length > 0)
        {
            m_exteriorLineRenderer = TrackExterior.GetComponent<LineRenderer>();
            m_exteriorLineRenderer.startWidth = .05f;
            m_exteriorLineRenderer.endWidth = .05f;
            m_exteriorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            m_exteriorLineRenderer.positionCount = trackDefinition.OuterBoundary.Length;

            for (var i = 0; i < trackDefinition.OuterBoundary.Length; i++)
            {
                Vector2 point = trackDefinition.OuterBoundary[i];
                m_exteriorLineRenderer.SetPosition(i, new Vector3(point.x * scale.x, point.y * scale.y, 0f));
            }
        }

        // 2. Interior Boundary
        if (trackDefinition.InnerBoundary != null && trackDefinition.InnerBoundary.Length > 0)
        {
            m_interiorLineRenderer = TrackInterior.GetComponent<LineRenderer>();
            m_interiorLineRenderer.startWidth = .05f;
            m_interiorLineRenderer.endWidth = .05f;
            m_interiorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            m_interiorLineRenderer.positionCount = trackDefinition.InnerBoundary.Length;

            for (var i = 0; i < trackDefinition.InnerBoundary.Length; i++)
            {
                Vector2 point = trackDefinition.InnerBoundary[i];
                m_interiorLineRenderer.SetPosition(i, new Vector3(point.x * scale.x, point.y * scale.y, 0f));
            }
        }

        // 3. Start Line
        if (trackDefinition.StartLine == null || trackDefinition.StartLine.Length == 0)
        {
            TrackStartLine.SetActive(false);
            trackIsRendered = true;
            return;
        }

        try
        {
            TrackStartLine.SetActive(true);
            m_startLineRenderer = TrackStartLine.GetComponent<LineRenderer>();
            m_startLineRenderer.startWidth = .05f;
            m_startLineRenderer.endWidth = .05f;
            m_startLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            m_startLineRenderer.positionCount = trackDefinition.StartLine.Length;

            for (var i = 0; i < trackDefinition.StartLine.Length; i++)
            {
                Vector2 point = trackDefinition.StartLine[i];
                m_startLineRenderer.SetPosition(i, new Vector3(point.x * scale.x, point.y * scale.y, 0f));
            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }

        trackIsRendered = true;
    }
}