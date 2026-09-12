using UnityEngine;
using Fusion;
using RestClient.Scripts.Clients;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.CompilerServices;

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

    public void OnTrackDefinitionUpdate(TrackDefinition m_trackDefinition)
    {
        ExteriorCoordinates.Clear();
        InteriorCoordinates.Clear();
        StartlineCoordinates.Clear();

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

    public TrackDefinition GetTrackDefinition()
    {
        return GetTrackDefinition(ExteriorCoordinates, InteriorCoordinates, StartlineCoordinates, ExteriorCoordinatesLength, InteriorCoordinatesLength, StartlineCoordinatesLength);
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
        if (m_trackDefinition != null)
        {
            foreach (var listener in m_trackDefinitionReadyListener)
            {
                NotifyTrackDefinitionReadyListener(LevelId);
                Debug.Log("Notified listener of track definition ready.");

            }
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
        } else
        {

        }
       
    }

    /// <summary>
    /// Fixed update network.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if(!Runner.IsServer)
        {
            return;
        }

        foreach (string propertyName in _changes.DetectChanges(this))
        {
            switch (propertyName)
            {
                case nameof(ExteriorCoordinates):
                    m_trackDefinition = GetTrackDefinition(ExteriorCoordinates, InteriorCoordinates, StartlineCoordinates, ExteriorCoordinatesLength, InteriorCoordinatesLength, StartlineCoordinatesLength);
                    if (m_trackDefinition != null)
                    {
                        foreach (var listener in m_trackDefinitionReadyListener)
                        {
                            NotifyTrackDefinitionReadyListener(LevelId);
                            Debug.Log("Notified listener of track definition ready.");
                            
                        }
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
    public void RPC_ReceivedTrackDefinition()
    {
    }



}
