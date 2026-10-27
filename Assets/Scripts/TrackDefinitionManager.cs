using Assets.GRL.Scripts.Models;
using Fusion;
using RestClient.Scripts.Clients;
using System.Collections.Generic;
using UnityEngine;

public class TrackDefinitionManager : NetworkBehaviour, ITrackAPI
{
    public RestClientTrackAPI RestClientTrackGenerator;

    [Networked]
    public string TrackId { get; set; }

    [Networked]
    public int LevelId { get; set; }

    // THE GOLDEN TICKET: Our simple state flag
    [Networked]
    public NetworkBool IsTrackLoaded { get; set; }

    public bool trackIsRendered { get; set; } = false;
    public TrackDefinition m_trackDefinition { get; set; }

    [Networked, Capacity(60)]
    public NetworkArray<Vector2> ExteriorCoordinates { get; }
    [Networked] public int ExteriorCoordinatesLength { get; set; }

    [Networked, Capacity(60)]
    public NetworkArray<Vector2> InteriorCoordinates { get; }
    [Networked] public int InteriorCoordinatesLength { get; set; }

    [Networked, Capacity(4)]
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

        // SERVER ONLY: Once arrays are packed, declare the track fully loaded
        if (Object.HasStateAuthority)
        {
            IsTrackLoaded = true;
        }
    }

    public TrackDefinition GetTrackDefinition()
    {
        TrackDefinition trackDefinition = new TrackDefinition();

        if (ExteriorCoordinatesLength > 0)
        {
            trackDefinition.OuterBoundary = new Vector2[ExteriorCoordinatesLength];
            for (var i = 0; i < ExteriorCoordinatesLength; i++)
            {
                trackDefinition.OuterBoundary[i] = ExteriorCoordinates[i];
            }
        }

        if (InteriorCoordinatesLength > 0)
        {
            trackDefinition.InnerBoundary = new Vector2[InteriorCoordinatesLength];
            for (var i = 0; i < InteriorCoordinatesLength; i++)
            {
                trackDefinition.InnerBoundary[i] = InteriorCoordinates[i];
            }
        }

        if (StartlineCoordinatesLength > 0)
        {
            trackDefinition.StartLine = new Vector2[StartlineCoordinatesLength];
            for (var i = 0; i < StartlineCoordinatesLength; i++)
            {
                trackDefinition.StartLine[i] = StartlineCoordinates[i];
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
            Debug.Log("Server: API callback complete. Arrays populated and listeners notified.");
        }
    }

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        if (Runner.IsServer)
        {
            // 1. Extract the trackId from the Runner's Session Properties
            if (Runner.SessionInfo.Properties.TryGetValue("trackId", out var trackIdProp))
            {
                // Assign it to the Networked property so it replicates to all clients
                TrackId = trackIdProp;
                Debug.Log($"[TrackDefinitionManager] Track ID retrieved from Runner Properties: {TrackId}");
            }

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
        else
        {
            // LATE-JOINER CATCH: Bypass the change detector if the server already loaded it
            if (IsTrackLoaded)
            {
                CheckAndNotifyClientTrackLoaded();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_changes == null) return;

        foreach (string propertyName in _changes.DetectChanges(this))
        {
            switch (propertyName)
            {
                // ACTIVE CLIENT CATCH: Trigger when the server flips the bool mid-session
                case nameof(ExteriorCoordinatesLength):
                case nameof(InteriorCoordinatesLength):
                case nameof(StartlineCoordinatesLength):
                case nameof(ExteriorCoordinates):
                case nameof(IsTrackLoaded):
                    if (IsTrackLoaded && !Runner.IsServer)
                    {
                        CheckAndNotifyClientTrackLoaded();
                    }
                    break;

                case nameof(TrackId):
                    if (Runner.IsServer)
                    {
                        try
                        {
                            RestClientTrackGenerator.GetTrackDefinition(TrackId);
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log(e.Message);
                        }
                    }
                    break;
            }
        }
    }

    private void CheckAndNotifyClientTrackLoaded()
    {
        m_trackDefinition = GetTrackDefinition();

        if (m_trackDefinition != null && m_trackDefinition.OuterBoundary != null && m_trackDefinition.OuterBoundary.Length > 0)
        {
            NotifyTrackDefinitionReadyListener(LevelId);
            Debug.Log($"[Client] Track state verified loaded. Exterior points: {ExteriorCoordinatesLength}");
        }
    }
}