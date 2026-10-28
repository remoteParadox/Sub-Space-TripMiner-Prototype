using UnityEngine;
using UnityEngine.EventSystems;

public class CameraOrbit : MonoBehaviour
{
    public Transform target;
    public float distance = 12f;
    public float minDistance = 6f;
    public float maxDistance = 25f;
    public float xSpeed = 150f;
    public float ySpeed = 100f;
    public float yMinLimit = -30f;
    public float yMaxLimit = 80f;
    public float zoomSpeed = 8f;
    private float x;
    private float y = 20f;

    private void Start()
    {
        if (target == null)
        {
            GridManager gm = FindFirstObjectByType<GridManager>();
            if (gm != null) target = gm.transform;
        }
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButton(1) && !pointerOverUI)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
            y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
        }

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (!shiftHeld && !pointerOverUI && Mathf.Abs(scroll) > 0.01f)
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(y, x, 0);
        transform.rotation = rotation;
        transform.position = rotation * new Vector3(0f, 0f, -distance) + target.position;
    }
}