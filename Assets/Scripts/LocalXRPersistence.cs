using UnityEngine;
using UnityEngine.SceneManagement;
using Meta.XR.MRUtilityKit;

public class LocalXRPersistence : MonoBehaviour
{
    public static LocalXRPersistence Instance;

    [Header("Selected Anchor & Target Track")]
    public Transform selectedTrackAnchor;
    public string trackGameObjectName = "RaceTrackShell1";

    // Store initial local pose so we can reset if the player returns to the Lobby
    private Vector3 _initialRootPosition;
    private Quaternion _initialRootRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _initialRootPosition = transform.position;
        _initialRootRotation = transform.rotation;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetTargetTrackAnchor(Transform anchorTransform)
    {
        selectedTrackAnchor = anchorTransform;
    }

    public void CorralRoomBeforeTransition()
    {
        if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
        {
            Transform roomTransform = MRUK.Instance.GetCurrentRoom().transform;
            roomTransform.SetParent(this.transform, true);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GRLGame")
        {
            // Pass the newly loaded scene into the alignment method
            AlignRoomToTrack(scene);
        }
        else if (scene.name == "GRLLobby")
        {
            transform.position = _initialRootPosition;
            transform.rotation = _initialRootRotation;

            var placementManager = FindAnyObjectByType<LobbyTrackPlacementManager>();
            if (placementManager != null && MRUK.Instance != null)
            {
                placementManager.PopulateValidTrackSpots(MRUK.Instance.GetCurrentRoom());
            }
        }
    }

    // Require the loaded scene as a parameter
    private void AlignRoomToTrack(Scene loadedScene)
    {
        if (selectedTrackAnchor == null)
        {
            Debug.LogError("[Inverse Snap] No track anchor was selected in the lobby!");
            return;
        }

        // Pass the loaded scene into the search helper
        GameObject trackObject = FindInRunnerScene(loadedScene, trackGameObjectName);

        if (trackObject == null)
        {
            Debug.LogError($"[Inverse Snap] Could not find '{trackGameObjectName}' in game scene!");
            return;
        }

        Transform networkedTrack = trackObject.transform;

        float rotationOffset = networkedTrack.eulerAngles.y - selectedTrackAnchor.eulerAngles.y;
        transform.RotateAround(selectedTrackAnchor.position, Vector3.up, rotationOffset);

        Vector3 positionOffset = networkedTrack.position - selectedTrackAnchor.position;
        transform.position += positionOffset;

        Debug.Log($"[Inverse Snap] Aligned physical anchor '{selectedTrackAnchor.name}' to '{trackGameObjectName}'.");

        GetComponent<TrackCameraFramer>().trackTarget = networkedTrack;
        GetComponent<TrackCameraFramer>().FrameTrackView();


    }

    // Require the loaded scene as a parameter and search IT instead of gameObject.scene
    private GameObject FindInRunnerScene(Scene loadedScene, string objectName)
    {
        foreach (var rootObj in loadedScene.GetRootGameObjects())
        {
            Transform[] transforms = rootObj.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t.name == objectName) return t.gameObject;
            }
        }
        return null;
    }
}


