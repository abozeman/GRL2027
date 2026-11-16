using UnityEngine;

public class TrackCameraFramer : MonoBehaviour
{
    [Tooltip("Assign the RaceTrackShell permanently located at 0,0,0")]
    public Transform trackTarget;

    public void FrameTrackView()
    {
        // 1. Dynamically locate the active camera (Finds WebXR Main Camera OR Quest CenterEyeAnchor)
        Camera activeCam = Camera.main;

        if (activeCam == null)
        {
            Debug.LogError("[Camera Framer] No camera tagged 'MainCamera' found in the scene.");
            return;
        }

        // 2. Branch logic based on the active client hardware
        if (UnityEngine.XR.XRSettings.enabled)
        {
            // VR CLIENT: The headset strictly controls the camera transform. 
            // To aim the camera at the track, we rotate the entire room container around the user's head.
            Transform environmentRoot = transform.root; // The [Local_XR_Environment] container

            // Calculate the flat direction from the headset to the track
            Vector3 directionToTrack = trackTarget.position - activeCam.transform.position;
            directionToTrack.y = 0;

            // Calculate where the user's head is currently pointing
            Vector3 currentGaze = activeCam.transform.forward;
            currentGaze.y = 0;

            // Pivot the entire room so the track lands exactly in the center of their field of view
            float angleOffset = Vector3.SignedAngle(currentGaze, directionToTrack, Vector3.up);
            environmentRoot.RotateAround(activeCam.transform.position, Vector3.up, angleOffset);

            Debug.Log("[Camera Framer] VR gaze aligned to track.");
        }
        else
        {
            // WEBXR / SPECTATOR CLIENT: The camera is decoupled from physical hardware tracking[cite: 6].
            // We can directly manipulate its position and rotation to create a tactical view.

            // Position the camera slightly elevated and pulled back from the track center
            Vector3 tacticalOffset = new Vector3(0f, 4.0f, -6.0f);
            activeCam.transform.position = trackTarget.position + tacticalOffset;

            // Aim the camera directly at the track (or start lights by adding a Vector3 offset)
            activeCam.transform.LookAt(trackTarget.position + (Vector3.up * 0.5f));

            Debug.Log("[Camera Framer] WebXR camera snapped to tactical track view.");
        }
    }
}