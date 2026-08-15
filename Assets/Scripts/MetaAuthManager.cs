using Oculus.Platform;
using Oculus.Platform.Models;
using RestClient.Core;
using RestClient.Core.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Application = UnityEngine.Application;

public class MetaAuthManager : MonoBehaviour
{
    private string loggedInUserId;
    [SerializeField]
    private string baseUrl = "http://192.168.2.49:8001";

    void Start()
    {
        // Surround initialization with a try/catch block
        try
        {
            // Asynchronous initialization does not block the main thread
            Core.AsyncInitialize().OnComplete(OnInitCallback);
        }
        catch (UnityException e)
        {
            Debug.LogError("Platform failed to initialize: " + e.Message);
            HandleFailedEntitlement();
        }
    }

    private void OnInitCallback(Message<PlatformInitialize> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Failed to initialize Meta Platform SDK: " + msg.GetError().Message);
            HandleFailedEntitlement();
        }
        else
        {
            Debug.Log("Meta Platform SDK initialized successfully.");
            // Proceed to the entitlement check
            PerformEntitlementCheck();
        }
    }

    private void PerformEntitlementCheck()
    {
        Entitlements.IsUserEntitledToApplication().OnComplete(OnEntitlementCheckCallback);
    }

    private void OnEntitlementCheckCallback(Message msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Entitlement check failed: " + msg.GetError().Message);
            HandleFailedEntitlement();
        }
        else
        {
            Debug.Log("Entitlement check passed!");
            // User is verified, now retrieve their profile data
            GetLoggedInUser();
        }
    }

    private void HandleFailedEntitlement()
    {
        // TODO: Handle failure. You must handle this gracefully.
        // E.g., show an error UI to the user and then quit the app.
        Debug.LogError("User is not entitled to this application.");
        Application.Quit();
    }

    private void GetLoggedInUser()
    {
        // Request the currently logged-in user's profile
        Users.GetLoggedInUser().OnComplete(OnGetLoggedInUserCallback);
    }

    private void OnGetLoggedInUserCallback(Message<User> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Failed to get logged in user: " + msg.GetError().Message);
        }
        else
        {
            // Extract the user data
            User user = msg.Data;
            loggedInUserId = user.ID.ToString();
            string userName = user.OculusID; // The user's display name

            Debug.Log($"Successfully retrieved user. ID: {loggedInUserId}, Name: {userName}");

            // Now that you have the user ID, call your backend API
            SyncUserWithDatabase(loggedInUserId, userName);
        }
    }

    private void SyncUserWithDatabase(string metaUserId, string metaUserName)
    {
        // TODO: Call the API you already wrote.
        // E.g., StartCoroutine(CallMyCustomAPI(metaUserId, metaUserName));
        Debug.Log($"Initiating API call to add user {metaUserId} to the database...");
        // setup the request header
        RequestHeader header = new RequestHeader
        {
            Key = "Content-Type",
            Value = "application/json"
        };

        string jsonPayload = JsonUtility.ToJson(new GRLUser
        {
            provider_name = "Meta",
            provider_user_id = metaUserId,
            provider_email = metaUserName // Assuming the username is used as email here
        });

        string apiUrl = $"{baseUrl}/api/auth/login";

        Debug.Log($"[API DEBUG] URL IS EXACTLY: '{apiUrl}'");

        // send a post request
        StartCoroutine(RestWebClient.Instance.HttpPost("http://192.168.2.49:8001/api/auth/login", jsonPayload,
            (r) => OnRequestComplete(r), new List<RequestHeader> { header }));
    }

    void OnRequestComplete(Response response)
    {
        Debug.Log($"Status Code: {response.StatusCode}");
        Debug.Log($"Data: {response.Data}");
        Debug.Log($"Error: {response.Error}");

        GoToScene("GRLWhere");
    }

    public class GRLUser
    {
        public string provider_name;
        public string provider_user_id;
        public string provider_email;
    }

    public void GoToScene(string nextSceneName)
    {
        Debug.Log($"[Transition] Attempting to load scene: {nextSceneName}");

        // Load the scene asynchronously in the background to prevent VR freezing
        SceneManager.LoadSceneAsync(nextSceneName);
    }

}

