using NUnit.Framework;
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

    [Header("Scene Routing")]
    [Tooltip("The exact name of the next scene to load upon successful auth")]
    public string nextSceneName = "GRLLobby";

    void Start()
    {

#if UNITY_EDITOR
        // 1. EDITOR BYPASS: Skip Meta hardware check and instantly mock a DB login
        Debug.Log("[Auth] Running in Unity Editor. Bypassing Meta hardware entitlement.");
        SyncUserWithDatabase("editor_mock_id_999", "EditorUser@test.com");
#else
        // 2. COMPILED CLIENT: Run the actual Meta Entitlement check
        try
        {
            Core.AsyncInitialize().OnComplete(OnInitCallback);
        }
        catch (UnityException e)
        {
            Debug.LogError("Platform failed to initialize: " + e.Message);
            HandleFailedEntitlement();
        }
#endif

    }

    private void OnInitCallback(Message msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Failed to initialize Meta Platform SDK: " + msg.GetError().Message);
            HandleFailedEntitlement();
        }
        else
        {
            Debug.Log("Meta Platform SDK initialized successfully.");
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
            GetLoggedInUser();
        }
    }

    private void HandleFailedEntitlement()
    {
        Debug.LogError("User is not entitled to this application. Quitting.");
        Application.Quit();
    }

    private void GetLoggedInUser()
    {
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
            User user = msg.Data;
            loggedInUserId = user.ID.ToString();
            string userName = user.OculusID;

            Debug.Log($"Successfully retrieved user. ID: {loggedInUserId}, Name: {userName}");
            SyncUserWithDatabase(loggedInUserId, userName);
        }
    }

    private void SyncUserWithDatabase(string metaUserId, string metaUserName)
    {
        Debug.Log($"Initiating API call to add user {metaUserId} to the database...");

        RequestHeader header = new RequestHeader
        {
            Key = "Content-Type",
            Value = "application/json"
        };

        string jsonPayload = JsonUtility.ToJson(new GRLUser
        {
            provider_name = "Meta",
            provider_user_id = metaUserId,
            provider_email = metaUserName
        });

        string apiUrl = $"{baseUrl}/api/auth/login";

        //StartCoroutine(RestWebClient.Instance.HttpPost(apiUrl, jsonPayload,
        //    (r) => OnRequestComplete(r), new List<RequestHeader> { header }));

        StartCoroutine(RestWebClient.Instance.HttpPost("http://192.168.2.49:8001/api/auth/login", jsonPayload,
           (r) => OnRequestComplete(r), new List<RequestHeader> { header }));
    }

    void OnRequestComplete(Response response)
    {
        Debug.Log($"[Database Auth] Status Code: {response.StatusCode}");

        // If the Flask API returns 200 (Existing User) or 201 (New User Created)
        if (response.StatusCode == 200 || response.StatusCode == 201)
        {
            Debug.Log("[Database Auth] Success! Transitioning to next scene...");
            GoToScene(nextSceneName);
        }
        else
        {
            Debug.LogError($"[Database Auth] Failed. Error: {response.Error} | Data: {response.Data}");
        }
    }

    public void GoToScene(string targetScene)
    {
        Debug.Log($"[Transition] Loading scene: {targetScene} asynchronously...");
        SceneManager.LoadSceneAsync(targetScene);
    }

    public class GRLUser
    {
        public string provider_name;
        public string provider_user_id;
        public string provider_email;
    }
}