using Meta.XR.MRUtilityKit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using static Meta.XR.MRUtilityKit.MRUK;

public class RoomSetupManager : MonoBehaviour
{
    private async void Start()
    {
        // Subscribe to the event so we know when the room is fully built
        // This fires BOTH when loaded from device AND after a successful new scan
        MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomReady);
        await InitializeRoom();
    }

    private void OnDestroy()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.RemoveListener(OnRoomReady);
        }
    }

    // Call this method when your scene finishes loading and you are ready for MR
    public async Task InitializeRoom()
    {
        Debug.Log("[MRUK] Attempting to load existing room data from headset...");

        // Step 1: Attempt to load the current room from the device cache
        bool success = await LoadRoomAsync();

        if (success)
        {
            Debug.Log("[MRUK] Room loaded successfully! Ready for gameplay.");
            // The RoomCreatedEvent will fire automatically now.
        }
        else
        {
            // Step 2: Loading failed (no room scanned, or user cleared cache). Trigger Scene Setup.
            Debug.Log("[MRUK] No valid room found. Triggering Meta OS Space Setup...");
            await TriggerRoomScan();
        }
    }

    private async Task<bool> LoadRoomAsync()
    {
        try
        {
            // In MRUK v200+, this async method handles the heavy lifting
            var room = await MRUK.Instance.LoadSceneFromDevice();

            // If the room object is not null, the headset had a saved room
            return room == LoadDeviceResult.Success;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MRUK] Failed to load scene from device: {e.Message}");
            return false;
        }
    }

    private async Task TriggerRoomScan()
    {
        Debug.Log("[MRUK] Triggering Meta OS Space Setup...");

        // The modern v200+ replacement for OVRSceneManager.RequestSceneCapture
        bool captureRequested = await OVRScene.RequestSpaceSetup();

        if (!captureRequested)
        {
            Debug.LogError("[MRUK] Failed to launch Space Setup. Ensure 'Scene' permission is enabled in Edit > Project Settings > OVRManager.");
        }
    }

    private void OnRoomReady(MRUKRoom room)
    {
        // STEP 3: The ultimate success state.
        Debug.Log($"[MRUK] Room generated successfully! Found {room.Anchors.Count} anchors.");

        // --> YOUR NEXT MVP STEP GOES HERE <--
        // e.g., Spawn the RC Car, connect to Photon Fusion, or load the UI dashboard.
    }
}