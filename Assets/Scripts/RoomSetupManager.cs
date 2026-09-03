using Assets.Scripts.Models;
using Meta.XR.MRUtilityKit;
using Newtonsoft.Json;
using RestClient.Core;
using RestClient.Core.Models;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Meta.XR.MRUtilityKit.MRUK;
using static MetaAuthManager;

public class RoomSetupManager : MonoBehaviour
{
    private string baseUrl = "http://192.168.2.49:8001";
    public GameObject raceUIButtonContainer; // Assign this in the Unity Inspector
    public GameObject raceButtonPrefab; // Assign this in the Unity Inspector


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

        GetRaces();

    }



    private void GetRaces()
    {
        // TODO: Call the API you already wrote.
        // E.g., StartCoroutine(CallMyCustomAPI(metaUserId, metaUserName));
        Debug.Log($"Initiating API call to get races from the database...");
        // setup the request header
        // send a get request
        StartCoroutine(RestWebClient.Instance.HttpGet("http://192.168.2.49:8001/api/racelist", (r) => OnRequestComplete(r)));

    }

    void OnRequestComplete(Response response)
    {
        Debug.Log($"Status Code: {response.StatusCode}");

        if (!string.IsNullOrEmpty(response.Error))
        {
            Debug.LogError($"API Error: {response.Error}");
            return;
        }

        if (!string.IsNullOrEmpty(response.Data))
        {
            // Deserialize the JSON string directly into your RaceList model
            RaceList raceResponse = JsonConvert.DeserializeObject<RaceList>(response.Data);

            if (raceResponse != null && raceResponse.result == "success")
            {
                Debug.Log($"Successfully deserialized {raceResponse.data.Length} races.");

                // Iterate through the array to access individual Race properties
                foreach (Race race in raceResponse.data)
                {
                    Debug.Log($"Lobby: {race.lobby_name} | Room: {race.room_name} | Status: {race.status}");

                    // Instantiate the race button prefab
                    GameObject raceButton = Instantiate(raceButtonPrefab, raceUIButtonContainer.transform);

                    // Highly recommended for UI elements to prevent weird stretching
                    raceButton.transform.localScale = Vector3.one;

                    // Set the button text to the lobby name
                    TMPro.TextMeshProUGUI buttonText = raceButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = race.lobby_name;
                    }
                }

                // TODO: Populate your scrollable UI panel with this list
            }
        }
    }

    public void GoToScene(string nextSceneName)
    {
        Debug.Log($"[Transition] Attempting to load scene: {nextSceneName}");

        // Load the scene asynchronously in the background to prevent VR freezing
        SceneManager.LoadSceneAsync(nextSceneName);
    }
}