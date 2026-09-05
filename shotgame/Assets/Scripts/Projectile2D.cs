using UnityEngine;

[DisallowMultipleComponent]
public sealed class Projectile2D : MonoBehaviour
{
    private const float MinDirectionSqrMagnitude = 0.0001f;

    [SerializeField, Min(0f)] private float speed = 12f;
    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0f)] private float viewportPadding = 0.1f;
    [SerializeField, Min(0f)] private float maxLifetime = 5f;

    private float spawnedAtTime;

    public void Launch(
        Vector2 launchDirection,
        float launchSpeed,
        Camera cameraToUse,
        float cameraPadding,
        float lifetime
    )
    {
        if (launchDirection.sqrMagnitude >= MinDirectionSqrMagnitude)
        {
            direction = launchDirection.normalized;
        }

        speed = Mathf.Max(0f, launchSpeed);
        targetCamera = cameraToUse;
        viewportPadding = Mathf.Max(0f, cameraPadding);
        maxLifetime = Mathf.Max(0f, lifetime);
        spawnedAtTime = Time.time;
        AlignToDirection();
    }

    private void OnEnable()
    {
        spawnedAtTime = Time.time;
        AlignToDirection();
    }

    private void Update()
    {
        transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);

        if (IsLifetimeExpired() || IsOutsideCameraView())
        {
            Destroy(gameObject);
        }
    }

    private bool IsLifetimeExpired()
    {
        return maxLifetime > 0f && Time.time - spawnedAtTime >= maxLifetime;
    }

    private bool IsOutsideCameraView()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : GetCamera();
        if (cameraToUse == null)
        {
            return false;
        }

        Vector3 viewportPosition = cameraToUse.WorldToViewportPoint(transform.position);
        return viewportPosition.z < 0f
            || viewportPosition.x < -viewportPadding
            || viewportPosition.x > 1f + viewportPadding
            || viewportPosition.y < -viewportPadding
            || viewportPosition.y > 1f + viewportPadding;
    }

    private static Camera GetCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera;
        }

        Camera[] cameras = Camera.allCameras;
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private void AlignToDirection()
    {
        if (direction.sqrMagnitude < MinDirectionSqrMagnitude)
        {
            return;
        }

        Vector2 normalizedDirection = direction.normalized;
        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
