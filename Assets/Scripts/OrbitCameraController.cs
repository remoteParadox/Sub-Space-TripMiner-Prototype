using UnityEngine;
public class OrbitCameraController : MonoBehaviour
{
    public Transform target;
    public float distance = 10f;
    public float minDistance = 6f;
    public float maxDistance = 18f;
    public float dragSensitivity = 4f;
    public float zoomSensitivity = 2f;
    private float yaw = 25f;
    private float pitch = 18f;

    private void LateUpdate()
    {
        if (target == null) return;
        if (Input.GetMouseButton(2))
        {
            yaw += Input.GetAxis("Mouse X") * dragSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * dragSensitivity;
            pitch = Mathf.Clamp(pitch, -75f, 75f);
        }
        distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity * 10f, minDistance, maxDistance);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position + rotation * new Vector3(0f, 0f, -distance);
        transform.rotation = rotation;
    }
}