using Fusion;
using UnityEngine;

public class RaceAnimationController : NetworkBehaviour
{
    // 1. The Authoritative Networked Struct
    [Networked]
    public RaceStateBools AnimationStates { get; set; }

    private ChangeDetector _changes;

    [Header("Animation & VFX References")]
    [SerializeField] private ParticleSystem _preRaceMaterializationVFX;
    [SerializeField] private Animator _raceStartLightAnimator;
    [SerializeField] private Animator _spectatorUIGlitchAnimator;
    [SerializeField] private Camera _mainCamera; // For FOV zooms

    [Header("Audio Pipeline")]
    [SerializeField] private AudioSource _announcerAudioSource;
    [SerializeField] private AudioClip _countdownClip; // "Three, two, one..."
    [SerializeField] private AudioClip _greenFlagClip; // "...green flag!"
    [SerializeField] private AudioClip _checkeredFlagClip;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        // (Keep your existing Server MQTT subscription logic here)
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
            if (_preRaceMaterializationVFX != null) _preRaceMaterializationVFX.Play();
        }
        else if (AnimationStates.IsCountdown)
        {
            Debug.Log("[Animation Party] State: COUNTDOWN");
            // Spawn or animate the holographic start-light array over the track center.
            if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("StartSequence");

            // Play the pre-cached announcer prompt pulled directly from cache memory to prevent latency.
            if (_announcerAudioSource != null && _countdownClip != null)
                _announcerAudioSource.PlayOneShot(_countdownClip);
        }
        else if (AnimationStates.IsFinalCountdown)
        {
            Debug.Log("[Animation Party] State: FINAL_COUNTDOWN");
            // E.g., The lights turn red, tense bass thud audio
            if (_raceStartLightAnimator != null) _raceStartLightAnimator.SetTrigger("FinalRed");
        }
        else if (AnimationStates.IsInProgress)
        {
            Debug.Log("[Animation Party] State: IN_PROGRESS");
            // Trigger a momentary CRT screen-tear glitch on the spectator UI[cite: 1].
            if (_spectatorUIGlitchAnimator != null) _spectatorUIGlitchAnimator.SetTrigger("ScreenTear");

            // Execute a rapid camera zoom[cite: 1]. 
            // (You can trigger a coroutine here to quickly lerp the _mainCamera.fieldOfView)

            if (_announcerAudioSource != null && _greenFlagClip != null)
                _announcerAudioSource.PlayOneShot(_greenFlagClip);
        }
        else if (AnimationStates.IsFinished)
        {
            Debug.Log("[Animation Party] State: FINISHED");
            // Trigger a time-dilation effect (slow motion) and automatically transition the camera rig to the PODIUM coordinate station[cite: 1].
            // Time.timeScale = 0.5f; // (Careful with time scale in multiplayer, usually better to handle via camera/UI pacing)

            if (_announcerAudioSource != null && _checkeredFlagClip != null)
                _announcerAudioSource.PlayOneShot(_checkeredFlagClip);
        }
    }
}