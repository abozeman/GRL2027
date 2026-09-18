using Assets.GRL.Scripts.Models;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Assets.CryptoKartz.Scripts
{
    public class GRLTrackVisualsManagerV2 : NetworkBehaviour, INetworkRunnerCallbacks, ITrackDefinitionManager
    {
        public GameObject TrackExterior;
        public GameObject TrackInterior;
        public GameObject TrackStartline;
        public GameObject TrackSurface;

        private GameObject m_TrackSurface;
        private GameObject m_TrackExterior;
        private GameObject m_TrackInterior;
        private GameObject m_TrackStartline;

        public bool trackIsRendered = false;
        public bool trackIsReady = false;
        private LineRenderer m_exteriorLineRenderer;
        private LineRenderer m_interiorLineRenderer;
        private LineRenderer m_startLineRenderer;
        public TrackDefinition m_trackDefinition { get; set; } = new();
        private TrackDefinitionManager _trackDefinitionManager;

        public float renderDelay { get; private set; } = .1f;

        public bool isTestMode { get; set; } = true;

        public override void Spawned()
        {
            if (!Runner.IsServer || isTestMode)
            {
                _trackDefinitionManager = transform.GetComponent<TrackDefinitionManager>();
                _trackDefinitionManager.RegisterTrackDefinitionReadyListener(this);

                trackIsRendered = false;

                m_TrackSurface = Instantiate(TrackSurface, transform);
                m_TrackExterior = Instantiate(TrackExterior, transform);
                m_TrackInterior = Instantiate(TrackInterior, transform);
                m_TrackStartline = Instantiate(TrackStartline, transform);

                StartCoroutine(GetTrackDefinitionAsync());
            }
        }

        public void SetTrackIsReady() => trackIsReady = true;

        void ITrackDefinitionManager.OnTrackDefinitionReady(int trackLevelId)
        {
            trackIsRendered = false;
        }

        private IEnumerator GetTrackDefinitionAsync()
        {
            m_trackDefinition = _trackDefinitionManager.GetTrackDefinition();
            Debug.Log("Track Definition retrieved asynchronously.");

            yield return new WaitUntil(() => m_trackDefinition != null);
        }

        private IEnumerator isTrackIsReady()
        {
            yield return new WaitUntil(() => m_TrackExterior != null && m_TrackExterior.GetComponent<LineRenderer>() != null
                                            && m_TrackInterior != null && m_TrackInterior.GetComponent<LineRenderer>() != null
                                            && m_TrackStartline != null && m_TrackStartline.GetComponent<LineRenderer>() != null);

            SetTrackIsReady();
            Debug.Log("Track is now ready to render a definition.");
            yield return true;
        }

        private IEnumerator RenderTrack(TrackDefinition trackDefinition, Vector3 scale)
        {
            StartCoroutine(isTrackIsReady());
            yield return new WaitUntil(() => trackIsReady);

            _trackDefinitionManager = transform.GetComponent<TrackDefinitionManager>();
            trackDefinition = _trackDefinitionManager.GetTrackDefinition();

            if (trackDefinition == null) yield break;

            // 1. Render Exterior Boundary
            if (trackDefinition.OuterBoundary != null && trackDefinition.OuterBoundary.Length > 0)
            {
                m_exteriorLineRenderer = m_TrackExterior.GetComponent<LineRenderer>();
                m_exteriorLineRenderer.startWidth = .05f;
                m_exteriorLineRenderer.endWidth = .05f;
                m_exteriorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                m_exteriorLineRenderer.positionCount = trackDefinition.OuterBoundary.Length;

                for (var i = 0; i < trackDefinition.OuterBoundary.Length; i++)
                {
                    Vector2 point = trackDefinition.OuterBoundary[i];
                    m_exteriorLineRenderer.SetPosition(i, new Vector3(point.x * scale.x, point.y * scale.y, 0f));
                    yield return new WaitForSeconds(renderDelay);
                }
            }

            // 2. Render Interior Boundary
            if (trackDefinition.InnerBoundary != null && trackDefinition.InnerBoundary.Length > 0)
            {
                m_interiorLineRenderer = m_TrackInterior.GetComponent<LineRenderer>();
                m_interiorLineRenderer.startWidth = .05f;
                m_interiorLineRenderer.endWidth = .05f;
                m_interiorLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                m_interiorLineRenderer.positionCount = trackDefinition.InnerBoundary.Length;

                for (var i = 0; i < trackDefinition.InnerBoundary.Length; i++)
                {
                    Vector2 point = trackDefinition.InnerBoundary[i];
                    m_interiorLineRenderer.SetPosition(i, new Vector3(point.x * scale.x, point.y * scale.y, 0f));
                    yield return new WaitForSeconds(renderDelay);
                }
            }

            // 3. Render Start Line
            if (trackDefinition.StartLine == null || trackDefinition.StartLine.Length == 0)
            {
                m_TrackStartline.SetActive(false);
            }
            else
            {
                m_TrackStartline.SetActive(true);
                m_startLineRenderer = m_TrackStartline.GetComponent<LineRenderer>();
                m_startLineRenderer.startWidth = .05f;
                m_startLineRenderer.endWidth = .05f;
                m_startLineRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                m_startLineRenderer.positionCount = trackDefinition.StartLine.Length;

                for (var i = 0; i < trackDefinition.StartLine.Length; i++)
                {
                    Vector2 point = trackDefinition.StartLine[i];
                    m_startLineRenderer.SetPosition(i, new Vector3(point.x * scale.x, point.y * scale.y, 0f));
                    yield return new WaitForSeconds(renderDelay);
                }
            }

            trackIsRendered = true;
            yield return trackIsRendered;
        }

        public override void Render()
        {
            if (!trackIsRendered && trackIsReady)
            {
                StartCoroutine(RenderTrack(m_trackDefinition, transform.localScale));
            }
        }

        #region Unused INetworkRunnerCallbacks Implementation
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
            if (runner != null)
            {
                StartCoroutine(GetTrackDefinitionAsync());
            }
        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
        }
        #endregion
    }
}