using Assets.GRL.Scripts.Managers;
using Assets.Scripts.Models;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GRLRaceButtonClickHandller : MonoBehaviour
{
    private Button button;
    public Race _raceInfo {get; set;}

    void Awake()
    {
        button = transform.GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    public void HandleClick()
    {
        // Put prefab logic here, or find a manager class in the scene
        Debug.Log("Button handled its own click event natively.");
        Debug.Log($"Race Info Lobby Name: {_raceInfo.lobby_name}");
        Debug.Log($"Race Info Room Name: {_raceInfo.room_name}");
        Debug.Log($"Race Info Status: {_raceInfo.status}");
        Debug.Log($"Race Info Track ID: {_raceInfo.track_id}");

        StartCoroutine(StartClientRoutine());

    }

    private IEnumerator StartClientRoutine()
    {
        var clientManager = button.GetComponent<ClientManager>();
        yield return clientManager.StartClient(_raceInfo.lobby_name, _raceInfo.room_name);

    }

    void OnDestroy()
    {
        // Best practice: clean up listeners when destroyed to prevent memory leaks
        button.onClick.RemoveListener(HandleClick);
    }
}
