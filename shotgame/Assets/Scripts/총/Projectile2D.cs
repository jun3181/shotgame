using UnityEngine;

[DisallowMultipleComponent]
public sealed class Projectile2D : MonoBehaviour
{
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const string DefaultProjectileLayerName = "Bullet";
    private static readonly string[] DefaultHitTargetLayerNames = { "Box", "Enemy" };

    [SerializeField, KoreanLabel("피격 대상 레이어")] private LayerMask hitTargetLayers;
    [SerializeField, KoreanLabel("피격 이펙트 프리팹")] private GameObject hitEffectPrefab;
    [SerializeField, KoreanLabel("피격 이펙트 크기"), Min(0f)] private float hitEffectScale = 1f;
    [SerializeField, KoreanLabel("피격 이펙트 지속 시간"), Min(0f)] private float hitEffectLifetime = 1f;
    [SerializeField, KoreanLabel("피격 시 총알 삭제")] private bool destroyOnHit = true;

    private float speed = 12f;
    private Vector2 direction = Vector2.right;
    private Camera targetCamera;
    private float viewportPadding = 0.1f;
    private float maxLifetime = 5f;
    private float spawnedAtTime;
    private bool hasHit;

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
        hasHit = false;
        ApplyDefaultHitTargetLayerIfEmpty();
        AlignToDirection();
    }

    private void Reset()
    {
        ApplyDefaultHitTargetLayerIfEmpty();
        ApplyDefaultProjectileLayerIfUnnamedOrDefault();
    }

    private void OnValidate()
    {
        hitEffectScale = Mathf.Max(0f, hitEffectScale);
        hitEffectLifetime = Mathf.Max(0f, hitEffectLifetime);
        ApplyDefaultHitTargetLayerIfEmpty();
        ApplyDefaultProjectileLayerIfUnnamedOrDefault();
    }

    private void OnEnable()
    {
        spawnedAtTime = Time.time;
        hasHit = false;
        ApplyDefaultHitTargetLayerIfEmpty();
        ApplyDefaultProjectileLayerIfUnnamedOrDefault();
        AlignToDirection();
    }

    private void Update()
    {
        Vector3 currentPosition = transform.position;
        Vector2 startPosition = currentPosition;
        Vector2 movement = direction.normalized * speed * Time.deltaTime;

        if (!hasHit && TryHitTargetOnMove(startPosition, movement, currentPosition.z))
        {
            return;
        }

        transform.position = currentPosition + (Vector3)movement;

        if (IsLifetimeExpired() || IsOutsideCameraView())
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        HandleHit(other.gameObject, other.ClosestPoint(transform.position));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        Vector2 hitPosition = collision.contactCount > 0
            ? collision.GetContact(0).point
            : collision.collider.ClosestPoint(transform.position);
        HandleHit(collision.collider.gameObject, hitPosition);
    }

    private bool TryHitTargetOnMove(Vector2 startPosition, Vector2 movement, float positionZ)
    {
        if (movement.sqrMagnitude < MinDirectionSqrMagnitude || hitTargetLayers.value == 0)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            startPosition,
            movement.normalized,
            movement.magnitude,
            hitTargetLayers
        );

        if (hit.collider == null)
        {
            return false;
        }

        transform.position = new Vector3(hit.point.x, hit.point.y, positionZ);
        HandleHit(hit.collider.gameObject, hit.point);
        return true;
    }

    private void HandleHit(GameObject hitObject, Vector2 hitPosition)
    {
        if (hasHit || hitObject == null || !IsInLayerMask(hitObject.layer, hitTargetLayers))
        {
            return;
        }

        hasHit = true;
        SpawnHitEffect(hitPosition);

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect(Vector2 hitPosition)
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = new Vector3(hitPosition.x, hitPosition.y, transform.position.z);
        GameObject effectObject = Instantiate(hitEffectPrefab, spawnPosition, transform.rotation);
        effectObject.transform.localScale *= hitEffectScale;

        if (hitEffectLifetime > 0f)
        {
            Destroy(effectObject, hitEffectLifetime);
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

    private void ApplyDefaultHitTargetLayerIfEmpty()
    {
        if (hitTargetLayers.value != 0)
        {
            return;
        }

        int layerMask = 0;
        for (int i = 0; i < DefaultHitTargetLayerNames.Length; i++)
        {
            int layer = LayerMask.NameToLayer(DefaultHitTargetLayerNames[i]);
            if (layer >= 0)
            {
                layerMask |= 1 << layer;
            }
        }

        if (layerMask != 0)
        {
            hitTargetLayers = layerMask;
        }
    }

    private void ApplyDefaultProjectileLayerIfUnnamedOrDefault()
    {
        int bulletLayer = LayerMask.NameToLayer(DefaultProjectileLayerName);
        if (bulletLayer < 0)
        {
            return;
        }

        if (gameObject.layer == 0 || string.IsNullOrEmpty(LayerMask.LayerToName(gameObject.layer)))
        {
            gameObject.layer = bulletLayer;
        }
    }

    private static bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
