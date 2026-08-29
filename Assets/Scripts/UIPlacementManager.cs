using UnityEngine;

public class UIPlacementManager : MonoBehaviour
{
    public float distanceFromUser = 1.0f;
    private OVRCameraRig userCamera;

    void Start()
    {
        userCamera = Object.FindAnyObjectByType<OVRCameraRig>();
        PlaceAndLook();
    }

    [ContextMenu("Position Panel")]
    public void PlaceAndLook()
    {
        if (userCamera == null) return;

        // 1. Position the panel in front of the user
        Vector3 targetPosition = userCamera.transform.position + (userCamera.transform.forward * distanceFromUser);
        transform.position = targetPosition;

        // 2. Make the panel look at the user
        transform.LookAt(userCamera.transform.position);

        // 3. Flip the rotation by 180 degrees so the UI text isn't backwards
        transform.Rotate(0, 180, 0);
    }
}

