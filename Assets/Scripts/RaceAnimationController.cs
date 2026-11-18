using Fusion;
using UnityEngine;
using UnityEngine.Playables;
using System;
using Newtonsoft.Json.Linq;
using M2MqttUnity;

public class RaceAnimationController : NetworkBehaviour
{
    [System.Serializable]
    public struct RaceStateBools : INetworkStruct
    {
        public NetworkBool IsPreRace;
        public NetworkBool IsAcceptingContestants;
        public NetworkBool IsCountdown;
        public NetworkBool IsFinalCountdown;
        public NetworkBool IsInProgress;
        public NetworkBool IsFinished;
    }

    [Networked] public RaceStateBools AnimationStates { get; set; }

    // The exact Fusion simulation time when the Python Race Manager changed the state
    [Networked] public double StateStartTime { get; set; }

    private ChangeDetector _changes;

    [Header("Timeline Orchestrator")]
    [SerializeField] private PlayableDirector _sequenceDirector;

    [Header("Cinematic Chunks (Playable Assets)")]
    [SerializeField] private PlayableAsset _preRaceChunk;
    [SerializeField] private PlayableAsset _acceptContestantsChunk;
    [SerializeField] private PlayableAsset _countdownChunk;
    [SerializeField] private PlayableAsset _finalCountdownChunk;
    [SerializeField] private PlayableAsset _inProgressChunk;

    // Local caches for the dynamic durations
    private int _preRaceSecs = 10;
    private int _acceptContestantsSecs = 60;
    private int _countdownSecs = 30;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        // 1. Retrieve the dynamic phase durations assigned by the Orchestrator
        if (Runner.SessionInfo.Properties.TryGetValue("preRaceSecs", out var preRaceProp))
            _preRaceSecs = preRaceProp;

        if (Runner.SessionInfo.Properties.TryGetValue("acceptContestantsSecs", out var acceptProp))
            _acceptContestantsSecs = acceptProp;

        if (Runner.SessionInfo.Properties.TryGetValue("countdownSecs", out var countdownProp))
            _countdownSecs = countdownProp;

        Debug.Log($"[RaceAnimationController] Loaded Dynamic Phase Durations -> PreRace: {_preRaceSecs}s | Accept: {_acceptContestantsSecs}s | Countdown: {_countdownSecs}s");

        if (Runner.IsServer)
        {
            if (M2MqttUnityClientNetwork.Instance != null)
            {
                M2MqttUnityClientNetwork.Instance.OnMessageReceived += DecodeStateMessage;
                string topic = $"race/{Runner.SessionInfo.Name}/state";
                M2MqttUnityClientNetwork.Instance.SubscribeToTopic(topic);
            }
        }
        else
        {
            // LATE JOINER CATCH: Evaluate immediately upon entering the room
            EvaluateAndPlayCurrentState();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Runner.IsServer && M2MqttUnityClientNetwork.Instance != null)
        {
            M2MqttUnityClientNetwork.Instance.OnMessageReceived -= DecodeStateMessage;
        }
    }

    private void DecodeStateMessage(string topic, byte[] message)
    {
        if (!topic.EndsWith("/state")) return;

        try
        {
            string msgRaw = System.Text.Encoding.UTF8.GetString(message);
            JObject json = JObject.Parse(msgRaw);
            string newState = json["state"]?.ToString();

            if (string.IsNullOrEmpty(newState)) return;

            var states = AnimationStates;
            states.IsPreRace = false;
            states.IsAcceptingContestants = false;
            states.IsCountdown = false;
            states.IsFinalCountdown = false;
            states.IsInProgress = false;
            states.IsFinished = false;

            switch (newState)
            {
                case "PRE_RACE": states.IsPreRace = true; break;
                case "ACCEPT_CONTESTANTS": states.IsAcceptingContestants = true; break;
                case "COUNTDOWN": states.IsCountdown = true; break;
                case "FINAL_COUNTDOWN": states.IsFinalCountdown = true; break;
                case "IN_PROGRESS": states.IsInProgress = true; break;
                case "FINISHED": states.IsFinished = true; break;
            }

            AnimationStates = states;

            // Stamp the global Fusion time to sync all clients perfectly
            StateStartTime = Runner.SimulationTime;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaceAnimationController] MQTT Parse Exception: {e.Message}");
        }
    }

    public override void Render()
    {
        if (_changes != null)
        {
            foreach (string propertyName in _changes.DetectChanges(this))
            {
                if (propertyName == nameof(AnimationStates))
                {
                    // ACTIVE PLAYER CATCH: The server just flipped the state switch
                    EvaluateAndPlayCurrentState();
                }
            }
        }
    }

    private void EvaluateAndPlayCurrentState()
    {
        if (AnimationStates.IsPreRace) PlaySyncedTimelineChunk(_preRaceChunk);
        else if (AnimationStates.IsAcceptingContestants) PlaySyncedTimelineChunk(_acceptContestantsChunk);
        else if (AnimationStates.IsCountdown) PlaySyncedTimelineChunk(_countdownChunk);
        else if (AnimationStates.IsFinalCountdown) PlaySyncedTimelineChunk(_finalCountdownChunk);
        else if (AnimationStates.IsInProgress) PlaySyncedTimelineChunk(_inProgressChunk);
    }

    private void PlaySyncedTimelineChunk(PlayableAsset timelineChunk)
    {
        if (timelineChunk == null) return;

        if (_sequenceDirector.state == PlayState.Playing)
        {
            _sequenceDirector.Stop();
        }

        _sequenceDirector.playableAsset = timelineChunk;

        // Calculate exact time elapsed since the server broadcast the state change
        double elapsedTime = Runner.SimulationTime - StateStartTime;
        if (elapsedTime < 0) elapsedTime = 0;

        // 1. If the late-joiner missed the cinematic entirely, snap to the final visual state
        if (elapsedTime >= timelineChunk.duration)
        {
            Debug.Log($"[Playables] Chunk {timelineChunk.name} already finished. Snapping to final holding frame.");

            // Fast-forward to the very end of the clip
            _sequenceDirector.time = timelineChunk.duration;

            // Evaluate() forces the timeline to instantly apply the visual changes (lights, shaders, transforms) 
            // of the final frame without actually playing the animation block.
            _sequenceDirector.Evaluate();
            return;
        }

        // 2. Play the cinematic from the perfectly synchronized timestamp
        _sequenceDirector.time = elapsedTime;
        _sequenceDirector.Play();
        Debug.Log($"[Playables] Executing {timelineChunk.name} chunk dynamically at {elapsedTime:F2}s");
    }
}