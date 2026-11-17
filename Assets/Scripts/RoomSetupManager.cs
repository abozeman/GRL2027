using Assets.GRL.Scripts.Managers;
using Assets.Scripts.Models;
using Meta.XR.MRUtilityKit;
using Newtonsoft.Json;
using RestClient.Core;
using RestClient.Core.Models;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static Meta.XR.MRUtilityKit.MRUK;
using static MetaAuthManager;

public class RoomSetupManager : MonoBehaviour
{
    private string baseUrl = "http://192.168.2.49:8001";
    public GameObject raceUIButtonContainer; // Assign this in the Unity Inspector
    public GameObject raceButtonPrefab; // Assign this in the Unity Inspector
    public GameObject _targetButton { get; set; } // Assign this in the Unity Inspector
    public TextAsset WarehouseJSON;
    public LobbyTrackPlacementManager trackPlacementManager;

    private bool uiReady = false;

    private void OnEnable()
    {
        if (trackPlacementManager != null)
        {
            trackPlacementManager.OnPlacementSpotSelected += HandleSpotSelected;
        }
    }

    private void OnDisable()
    {
        if (trackPlacementManager != null)
        {
            trackPlacementManager.OnPlacementSpotSelected -= HandleSpotSelected;
        }
    }

    // 2. This triggers ONLY when the user (or the auto-sorter) selects a spot
    private void HandleSpotSelected()
    {
        Debug.Log("[RoomSetupManager] Spot selected! Fetching races and joining game...");
        GetRaces();
    }

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
            // In MRUK v200+, this async method handles the heavy lifting  LoadSceneFromJsonString
            //var room = await MRUK.Instance.LoadSceneFromDevice();

            //For testing purposes, we can load a room from a JSON string instead of the device
            //var room = await MRUK.Instance.LoadSceneFromJsonString(WarehouseJSON.text);
            var json = WarehouseJSON.text;
            var room = await MRUK.Instance.LoadSceneFromJsonString(json);

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
    }



    public void GetRaces()
    {
        // TODO: Call the API you already wrote.
        // E.g., StartCoroutine(CallMyCustomAPI(metaUserId, metaUserName));
        Debug.Log($"Initiating API call to get races from the database...");
        // setup the request header
        // send a get request
        StartCoroutine(RestWebClient.Instance.HttpGet("http://192.168.2.49:8001/api/racelist", (r) => OnRequestComplete(r)));

    }

    private async Task OnRequestComplete(Response response)
    {
        Debug.Log($"Status Code: {response.StatusCode}");

        if (!string.IsNullOrEmpty(response.Error))
        {
            Debug.LogError($"API Error: {response.Error}");
            return;
        }

        if (!string.IsNullOrEmpty(response.Data))
        {
            await PopulateUIButtons(response.Data);
        }
    }

    private System.Collections.IEnumerator WaitAndClickRoutine()
    {
        Debug.Log("[Desktop Mode] UI populated. Waiting 5 seconds before auto-clicking...");

        // Pause execution for 30 seconds
        yield return new WaitForSeconds(5f);

        // Grab the first button that was dynamically generated in the UI container
        GRLRaceButtonClickHandller[] availableButtons = raceUIButtonContainer.GetComponentsInChildren<GRLRaceButtonClickHandller>();

        if (availableButtons != null && availableButtons.Length > 0)
        {
            for(int i = 0; i < availableButtons.Length; i++)
            {
                Race raceInfo = availableButtons[i].GetComponent<GRLRaceButtonClickHandller>()._raceInfo;
                if(raceInfo != null)
                {
                    if(raceInfo.status != "FINISHED")
                    {
                        availableButtons[i].HandleClick(); // Assign the first non-finished button as the target
                        Debug.Log($"[Desktop Mode] Button {i} - Lobby: {raceInfo.lobby_name}, Room: {raceInfo.room_name}, Status: {raceInfo.status}");
                        Debug.Log($"[Desktop Mode] Found button {i}: {availableButtons[i].gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Desktop Mode] Button {i} has no race info assigned!");
                }
            }
            Debug.Log("[Desktop Mode] 5 seconds reached! Artificially clicking the target race button.");
        }
        else
        {
            Debug.LogWarning("[Desktop Mode] Cannot auto-click. No active race buttons were found in the container.");
        }
    }

    private async Task PopulateUIButtons(string responseData)
    {

        // Deserialize the JSON string directly into your RaceList model
        RaceList raceList = JsonConvert.DeserializeObject<RaceList>(responseData);

        if (raceList != null && raceList.result == "success")
        {
            Debug.Log($"Successfully deserialized {raceList.data.Length} races.");

            // Iterate through the array to access individual Race properties
            foreach (Race race in raceList.data)
            {
                Debug.Log($"Lobby: {race.lobby_name} | Room: {race.room_name} | Status: {race.status} | TrackId: {race.track_id}");

                // Instantiate the race button prefab
                GameObject raceButton = Instantiate(raceButtonPrefab, raceUIButtonContainer.transform);
                raceButton.GetComponent<GRLRaceButtonClickHandller>()._raceInfo = race;

                // Highly recommended for UI elements to prevent weird stretching
                raceButton.transform.localScale = Vector3.one;

                // Set the button text to the lobby name
                TMPro.TextMeshProUGUI[] buttonTexts = raceButton.GetComponentsInChildren<TMPro.TextMeshProUGUI>();

                foreach (TMPro.TextMeshProUGUI textItem in buttonTexts)
                {
                    var parentName = textItem.gameObject.name;
                    Debug.Log($"Parent Object 1st Try: {textItem.gameObject.name}");

                    switch (parentName)
                    {
                        case "Session":
                            textItem.text = race.lobby_name;
                            break;
                        case "Name":
                            textItem.text = race.room_name;
                            break;
                        case "Status":
                            textItem.text = race.status;
                            break;
                    }


                }

            }

        }

        StartCoroutine(WaitAndClickRoutine());
    }

    
}