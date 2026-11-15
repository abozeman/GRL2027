using Assets.GRL.Scripts.Models;
using Fusion;
using System.Collections;
using UnityEngine;

namespace Assets.CryptoKartz.Scripts
{
    // Removed INetworkRunnerCallbacks since they are unused and add clutter
    public class GRLTrackVisualsManagerV2 : NetworkBehaviour, ITrackDefinitionManager
    {
        public GameObject TrackExterior;
        public GameObject TrackInterior;
        public GameObject TrackStartline;
        public GameObject TrackSurface;
        public GameObject RaceLights;

        private GameObject m_TrackSurface;
        private GameObject m_TrackExterior;
        private GameObject m_TrackInterior;
        private GameObject m_TrackStartline;
        private GameObject m_RaceLights;
        private GameObject m_raceAnimationController;

        public bool trackIsRendered = false;
        private LineRenderer m_exteriorLineRenderer;
        private LineRenderer m_interiorLineRenderer;
        private LineRenderer m_startLineRenderer;

        public TrackDefinition m_trackDefinition { get; set; }
        private TrackDefinitionManager _trackDefinitionManager;

        public override void Spawned()
        {
            if (!Runner.IsServer)
            {
                _trackDefinitionManager = transform.GetComponent<TrackDefinitionManager>();
                _trackDefinitionManager.RegisterTrackDefinitionReadyListener(this);

                trackIsRendered = false;

                // Instantiate containers immediately
                m_TrackSurface = Instantiate(TrackSurface, transform, false);
                m_TrackExterior = Instantiate(TrackExterior, m_TrackSurface.transform, false);
                m_TrackInterior = Instantiate(TrackInterior, m_TrackSurface.transform, false);
                m_TrackStartline = Instantiate(TrackStartline, m_TrackSurface.transform, false);
                m_RaceLights = Instantiate(RaceLights, transform.parent, false);
                m_RaceLights.transform.localPosition = new Vector3(0f, 0f, 0f);
                m_RaceLights.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                m_raceAnimationController = GameObject.Find("RaceAnimationController");
                m_raceAnimationController.GetComponent<RaceAnimationController>().AddRaceLightsAnimator(m_RaceLights.GetComponent<Animator>());

                // LATE-JOINER CATCH: Use our new highly reliable NetworkBool!
                if (_trackDefinitionManager.IsTrackLoaded)
                {
                    m_trackDefinition = _trackDefinitionManager.GetTrackDefinition();
                    if (m_trackDefinition != null)
                    {
                        StartCoroutine(RenderTrackCoroutine(m_trackDefinition, transform.localScale));
                    }
                }
            }
        }

        // ACTIVE CLIENT CATCH: Called by TrackDefinitionManager when IsTrackLoaded flips to true mid-session
        void ITrackDefinitionManager.OnTrackDefinitionReady(int trackLevelId)
        {
            if (trackIsRendered) return;

            m_trackDefinition = _trackDefinitionManager.GetTrackDefinition();
            if (m_trackDefinition != null)
            {
                StartCoroutine(RenderTrackCoroutine(m_trackDefinition, transform.localScale));
            }
        }

        private IEnumerator RenderTrackCoroutine(TrackDefinition trackDefinition, Vector3 scale)
        {
            // Wait until instantiated objects and LineRenderers are valid
            yield return new WaitUntil(() => m_TrackExterior != null && m_TrackExterior.GetComponent<LineRenderer>() != null
                                          && m_TrackInterior != null && m_TrackInterior.GetComponent<LineRenderer>() != null
                                          && m_TrackStartline != null && m_TrackStartline.GetComponent<LineRenderer>() != null);

            if (trackDefinition == null) yield break;

            // 1. Exterior LineRenderer
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
                }
            }

            // 2. Interior LineRenderer
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
                }
            }

            // 3. Startline LineRenderer
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
                }
            }

            

            trackIsRendered = true;
            Debug.Log("[GRLTrackVisualsManagerV2] Track successfully rendered on Client.");
        }
    }
}