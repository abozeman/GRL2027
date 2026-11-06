using UnityEngine;
using Meta.XR.MRUtilityKit;

public class LocalXRPersistence : MonoBehaviour
{
    public static LocalXRPersistence Instance;

    void Awake()
    {
        // Prevent duplicate XR rigs if the user returns to the Lobby
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // DDOL only works on root objects
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Call this right before SceneManager.LoadSceneAsync to rescue the loose room meshes.
    /// </summary>
    public void CorralRoomBeforeTransition()
    {
        if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
        {
            Transform roomTransform = MRUK.Instance.GetCurrentRoom().transform;

            // Re-parent the loose room hierarchy into this protected DDOL object
            roomTransform.SetParent(this.transform);
            Debug.Log("[XR Persistence] Successfully corralled the MRUK Room into the persistent root.");
        }
        else
        {
            Debug.LogWarning("[XR Persistence] No MRUK room found to corral.");
        }
    }
}