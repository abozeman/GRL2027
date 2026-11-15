using Fusion;
using M2MqttUnity;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RaceAnimationController : NetworkBehaviour
{
    // 1. The Authoritative Networked Struct
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



    private ChangeDetector _changes;

    [Header("Animation & VFX References")]
    //[SerializeField] private ParticleSystem _preRaceMaterializationVFX;
    //[SerializeField] private ParticleSystem _acceptingContestantsMaterializationVFX;
    private Animator _raceStartLightAnimator;
    //[SerializeField] private Animator _spectatorUIGlitchAnimator;
    //[SerializeField] private Camera _mainCamera; // For FOV zooms

    //[Header("Audio Pipeline")]
    //[SerializeField] private AudioSource _announcerAudioSource;
    //[SerializeField] private AudioClip _countdownClip; // "Three, two, one..."
    //[SerializeField] private AudioClip _greenFlagClip; // "...green flag!"
    //[SerializeField] private AudioClip _checkeredFlagClip;

    [Header("AnimationStates ")]

    [Networked]
    public RaceStateBools AnimationStates { get; set; }

    private struct QueuedMessage
    {
        public string Topic;
        public string Payload;
    }
    private List<QueuedMessage> eventMessages = new List<QueuedMessage>();
    String SessionName;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        SessionName = Runner.SessionInfo.Name.ToLower();
        Debug.Log($"[RaceAnimationController] has Spawned in room: {SessionName}");

        // (Keep your existing Server MQTT subscription logic here)
        if (M2MqttUnityClientNetwork.Instance != null && Runner.IsServer)
        {
            // Subscribe to the global event
            M2MqttUnityClientNetwork.Instance.OnMessageReceived += DecodeMessage;

            string raceStateTopic = $"race/{SessionName}/state";
            M2MqttUnityClientNetwork.Instance.SubscribeToTopic(raceStateTopic);
            Debug.Log($"[RaceAnimationController] Hooked into Singleton and subscribed to {raceStateTopic}");

        }
    }

    public void AddRaceLightsAnimator(Animator _animator)
    {
        _raceStartLightAnimator = _animator;
    }

    protected void DecodeMessage(string topic, byte[] message)
    {
        if (topic.Contains($"telemetry")) return;

        Debug.Log($"[RaceAnimationController] Topic: {topic}");


        try
        {
            string msg = System.Text.Encoding.UTF8.GetString(message);
            string myRoomName = Runner.SessionInfo.Name.ToLower();

            Debug.Log($"[RaceAnimationController] Topic | Message: {topic} | {msg}");


            if (topic.Contains($"race/{myRoomName}/state"))
            {
                eventMessages.Add(new QueuedMessage { Topic = topic, Payload = msg });
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"[RaceAnimationController] DecodeMessage EXCEPTION: {e.StackTrace}");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (eventMessages.Count > 0)
        {
            foreach (var msg in eventMessages)
            {
                ProcessMessage(msg.Topic, msg.Payload);
            }
            eventMessages.Clear();
        }
    }

    private void ProcessMessage(string topic, string msg)
    {
        try
        {
            string myRoomName = Runner.SessionInfo.Name.ToLower();

            if (topic.Contains("state"))
            {
                // FIX 2: Safely parse the JSON using Newtonsoft (JObject) 
                // to completely ignore case-sensitivity mismatches in the Python payloads
                var jsonData = Newtonsoft.Json.Linq.JObject.Parse(msg);
                string newState = (string)jsonData.GetValue("state", StringComparison.OrdinalIgnoreCase);
                UpdateRaceState(newState);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaceAnimationController] ProcessMessage EXCEPTION: {e.Message}");
            Debug.LogError($"[RaceAnimationController] ProcessMessage StackTrace: {e.StackTrace}");
        }
    }

    private void UpdateRaceState(string newState)
    {
        if (string.IsNullOrEmpty(newState))
        {
            Debug.LogError("[RaceAnimationController] Received empty race state. Ignoring.");
            return;
        }

        Debug.Log($"[RaceAnimationController] raceAnimationController == : {newState}");

        switch (newState)
        {
            case "PRE_RACE":
                AnimationStates = new RaceStateBools
                {
                    IsPreRace = true,
                    IsAcceptingContestants = false,
                    IsCountdown = false,
                    IsFinalCountdown = false,
                    IsInProgress = false,
                    IsFinished = false
                };
                break;
            case "ACCEPT_CONTESTANTS":
                AnimationStates = new RaceStateBools
                {
                    IsPreRace = false,
                    IsAcceptingContestants = true,
                    IsCountdown = false,
                    IsFinalCountdown = false,
                    IsInProgress = false,
                    IsFinished = false
                };
                break;
            case "COUNTDOWN":
                AnimationStates = new RaceStateBools
                {
                    IsPreRace = false,
                    IsAcceptingContestants = false,
                    IsCountdown = true,
                    IsFinalCountdown = false,
                    IsInProgress = false,
                    IsFinished = false
                };
                break;
            case "FINAL_COUNTDOWN":
                AnimationStates = new RaceStateBools
                {
                    IsPreRace = false,
                    IsAcceptingContestants = false,
                    IsCountdown = false,
                    IsFinalCountdown = true,
                    IsInProgress = false,
                    IsFinished = false
                };
                break;
            case "IN_PROGRESS":
                AnimationStates = new RaceStateBools
                {
                    IsPreRace = false,
                    IsAcceptingContestants = false,
                    IsCountdown = false,
                    IsFinalCountdown = false,
                    IsInProgress = true,
                    IsFinished = false
                };
                break;
            case "FINISHED":
                AnimationStates = new RaceStateBools
                {
                    IsPreRace = false,
                    IsAcceptingContestants = false,
                    IsCountdown = false,
                    IsFinalCountdown = false,
                    IsInProgress = false,
                    IsFinished = true
                };
                break;
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
                    CheckAndTriggerAnimations();
                }
            }
        }
    }

    // 2. The Client-Side Animation Party
    private void CheckAndTriggerAnimations()
    {
        // Because the server resets all bools to false before setting the active one to true,
        // we can safely evaluate these without fear of overlap.

        if (AnimationStates.IsPreRace)
        {
            Debug.Log("[Animation Party] State: PRE_RACE");
            // Trigger the neon wireframe box to solid chassis digital materialization or dissolve shader.
            //if (_preRaceMaterializationVFX != null) _preRaceMaterializationVFX.Play();
            if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("PreRace");
        }
        else if (AnimationStates.IsAcceptingContestants)
        {
            Debug.Log("[Animation Party] State: ACCEPTING_CONTESTANTS");
            // Trigger the neon wireframe box to solid chassis digital materialization or dissolve shader.
            //if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("StartSequence");
        }
        else if (AnimationStates.IsCountdown)
        {
            Debug.Log("[Animation Party] State: COUNTDOWN");
            // Spawn or animate the holographic start-light array over the track center.
            if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("Countdown");

            // Play the pre-cached announcer prompt pulled directly from cache memory to prevent latency.
            //if (_announcerAudioSource != null && _countdownClip != null)
            //    _announcerAudioSource.PlayOneShot(_countdownClip);
        }
        else if (AnimationStates.IsFinalCountdown)
        {
            Debug.Log("[Animation Party] State: FINAL_COUNTDOWN");
            // E.g., The lights turn red, tense bass thud audio
            if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("FinalCountdown");
        }
        else if (AnimationStates.IsInProgress)
        {
            Debug.Log("[Animation Party] State: IN_PROGRESS");
            // Trigger a momentary CRT screen-tear glitch on the spectator UI[cite: 1].
            if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("InProgress");

            //if (_spectatorUIGlitchAnimator != null) _spectatorUIGlitchAnimator.SetTrigger("ScreenTear");

            // Execute a rapid camera zoom[cite: 1]. 
            // (You can trigger a coroutine here to quickly lerp the _mainCamera.fieldOfView)

            //if (_announcerAudioSource != null && _greenFlagClip != null)
            //    _announcerAudioSource.PlayOneShot(_greenFlagClip);
        }
        else if (AnimationStates.IsFinished)
        {
            Debug.Log("[Animation Party] State: FINISHED");
            // Trigger a time-dilation effect (slow motion) and automatically transition the camera rig to the PODIUM coordinate station[cite: 1].
            // Time.timeScale = 0.5f; // (Careful with time scale in multiplayer, usually better to handle via camera/UI pacing)
            if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("Finished");

            //if (_announcerAudioSource != null && _checkeredFlagClip != null)
            //    _announcerAudioSource.PlayOneShot(_checkeredFlagClip);
        }
    }
}
