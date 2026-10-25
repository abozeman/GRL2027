using Assets.GRL.Scripts.Managers;
using Assets.Scripts.Models;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GRLRaceButtonClickHandller : MonoBehaviour
{
    private Button button;
    public Race _raceInfo { get; set; }

    void Awake()
    {
        button = transform.GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    public void HandleClick()
    {
        Debug.Log("Button handled its own click event natively.");
        Debug.Log($"Race Info Lobby Name: {_raceInfo.lobby_name}");
        Debug.Log($"Race Info Room Name: {_raceInfo.room_name}");
        Debug.Log($"Race Info Status: {_raceInfo.status}");
        Debug.Log($"Race Info Track ID: {_raceInfo.track_id}");

        StartCoroutine(StartClientRoutine());
    }

    private IEnumerator StartClientRoutine()
    {
        // FIX: Search the entire scene for the ClientManager instead of just looking on the button
        var clientManager = FindAnyObjectByType<ClientManager>();

        // Safety check to ensure it was actually found in the scene hierarchy
        if (clientManager != null)
        {
            yield return clientManager.StartClient(_raceInfo.lobby_name, _raceInfo.room_name);
        }
        else
        {
            Debug.LogError("[RaceButton] CRITICAL: ClientManager could not be found anywhere in the scene!");
        }
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(HandleClick);
    }
}