using UnityEngine;

public class DynamicCameraFollow : MonoBehaviour
{
    // Singleton instance so network objects can find this script instantly
    public static DynamicCameraFollow Instance;

    public Transform targetVehicle;
    public Vector3 offset = new Vector3(0, 2f, -5f); // Height and distance
    public float smoothSpeed = 10f;

    void Awake()
    {
        // Assign the singleton
        if (Instance == null) Instance = this;
    }

    void LateUpdate()
    {
        if (targetVehicle != null)
        {
            // Smoothly glide to the target position
            Vector3 desiredPosition = targetVehicle.position + targetVehicle.TransformDirection(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Look forward slightly above the center of the car
            transform.LookAt(targetVehicle.position + (Vector3.up * 1.5f));
        }
    }
}