using UnityEngine;

public class TrackSpotPreviewItem : MonoBehaviour
{
    private int _spotIndex;
    private LobbyTrackPlacementManager _manager;

    public void Initialize(int index, LobbyTrackPlacementManager manager)
    {
        _spotIndex = index;
        _manager = manager;
    }

    // Hook this method up to your XR Interactable (e.g., Interactable Unity Event "When Select")
    public void OnSelectSpot()
    {
        if (_manager != null)
        {
            _manager.SelectPlacementSpot(_spotIndex);
        }
    }
}