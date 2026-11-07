using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    public SpriteRenderer background;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player == null || background == null)
            return;

        // background's world boundaries.
        Bounds bounds = background.bounds;

        // Calculate half of the camera's visible area.
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // Calculate the camera's allowed position.
        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;
        float minY = bounds.min.y + halfHeight;
        float maxY = bounds.max.y - halfHeight;

        // Follow the player but stay inside the background.
        float x = Mathf.Clamp(player.position.x, minX, maxX);
        float y = Mathf.Clamp(player.position.y, minY, maxY);

        // If the background is smaller than the camera view, it should keep the camera centered on that axis.
        if (minX > maxX)
            x = bounds.center.x;

        if (minY > maxY)
            y = bounds.center.y;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}