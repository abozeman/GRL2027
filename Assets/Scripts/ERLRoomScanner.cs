using Meta.XR.MRUtilityKit;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class ERLRoomScanner : MonoBehaviour
{
    private void Start()
    {
        // Initial check to see if we have room data on device
        StartCoroutine(InitializeMRUK());
    }

    public void LoadRoom()
    {
        // Initial check to see if we have room data on device
        //StartCoroutine(InitializeMRUK());
    }

    private IEnumerator InitializeMRUK()
    {
        // Wait for MRUK to be ready
        while (MRUK.Instance == null) yield return null;

        // Attempt to load the room from the device
        MRUK.Instance.LoadSceneFromDevice();
    }

    // This is called by MRUK when LoadSceneFromDevice fails (e.g., no room exists)
    public void OnLoadFailed()
    {
        Debug.Log("No room data found. Requesting Space Setup...");
        // This pauses your app and opens the Quest system scanner
        OVRScene.RequestSpaceSetup();
    }

    // Use Unity's OnApplicationFocus to detect when the user returns from the scanner
    private async Task OnApplicationFocus(bool focus)
    {
        if (focus && MRUK.Instance != null)
        {
            Debug.Log("User returned to app. Attempting to reload room...");
            // Now that the user has scanned, we must manually trigger the load again
            MRUK.LoadDeviceResult result = await MRUK.Instance.LoadSceneFromDevice();
            if (result == MRUK.LoadDeviceResult.Success)
            {
                Debug.Log("Room loaded successfully after scanning.");
            }
            else
            {
                Debug.LogError("Failed to load room after scanning.");
            }
        }
    }
}