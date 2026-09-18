using Assets.GRL.Scripts.Models;
using Fusion;
using Meta.XR.MRUtilityKit;
using RestClient.Scripts.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class TrackDefinitionManager : NetworkBehaviour, ITrackAPI
{
    public RestClientTrackAPI RestClientTrackGenerator;

    [Networked, Capacity(20)]
    public string TrackId { get; set; }

    [Networked]
    public int LevelId { get; set; }

    public bool trackIsRendered { get; set; } = false;
    public TrackDefinition m_trackDefinition { get; set; }

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

    public readonly List<ITrackDefinitionManager> m_trackDefinitionReadyListener = new();

    public float renderDelay { get; private set; } = .1f;

    private ChangeDetector _changes;

    public void RegisterTrackDefinitionReadyListener(ITrackDefinitionManager listener)
    {
        if (!m_trackDefinitionReadyListener.Contains(listener))
        {
            m_trackDefinitionReadyListener.Add(listener);
        }
    }

    public void UnregisterTrackDefinitionReadyListener(ITrackDefinitionManager listener)
    {
        if (m_trackDefinitionReadyListener.Contains(listener))
        {
            m_trackDefinitionReadyListener.Remove(listener);
        }
    }

    private void NotifyTrackDefinitionReadyListener(int level)
    {
        foreach (var listener in m_trackDefinitionReadyListener)
        {
            listener.OnTrackDefinitionReady(level);
        }
    }

    public void OnTrackDefinitionUpdate(TrackDefinition trackDefinition)
    {
        ExteriorCoordinates.Clear();
        InteriorCoordinates.Clear();
        StartlineCoordinates.Clear();

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

    public TrackDefinition GetTrackDefinition()
    {
        return GetTrackDefinition(ExteriorCoordinates, InteriorCoordinates, StartlineCoordinates, ExteriorCoordinatesLength, InteriorCoordinatesLength, StartlineCoordinatesLength);
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
        if (m_trackDefinition != null)
        {
            NotifyTrackDefinitionReadyListener(LevelId);
            Debug.Log("Notified listener of track definition ready.");
        }
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        if (Runner.IsServer)
        {
            RestClientTrackGenerator.RegisterGetTrackDefinitionCompleteListener(this);

            try
            {
                if (!string.IsNullOrEmpty(TrackId))
                {
                    RestClientTrackGenerator.GetTrackDefinition(TrackId);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;

        foreach (string propertyName in _changes.DetectChanges(this))
        {
            switch (propertyName)
            {
                case nameof(ExteriorCoordinates):
                    m_trackDefinition = GetTrackDefinition(ExteriorCoordinates, InteriorCoordinates, StartlineCoordinates, ExteriorCoordinatesLength, InteriorCoordinatesLength, StartlineCoordinatesLength);
                    if (m_trackDefinition != null)
                    {
                        NotifyTrackDefinitionReadyListener(LevelId);
                        Debug.Log("Notified listener of track definition ready.");
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, InvokeLocal = false)]
    public void RPC_ReceivedTrackDefinition() { }
}