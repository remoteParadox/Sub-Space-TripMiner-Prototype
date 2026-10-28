using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    
    [Header("Orbit Settings")]
    public float distance = 10f;
    public float minDistance = 5f;
    public float maxDistance = 20f;
    
    public float xSpeed = 120f;
    public float ySpeed = 80f;
    
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
    
    [Header("Zoom")]
    public float scrollSpeed = 5f;
    
    private float x = 0f;
    private float y = 0f;
    
    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        if (Input.GetMouseButton(1)) // Right mouse button to orbit
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            
            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }
        
        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * scrollSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(0f, 0f, -distance) + target.position;
        
        transform.rotation = rotation;
        transform.position = position;
    }
    
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
