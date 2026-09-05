using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class SubmachineGunFire : MonoBehaviour
{
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float MinAttackSpeed = 0.1f;
    private const int MinBulletCount = 1;
#if UNITY_EDITOR
    private const string DefaultGunDataPath = "Assets/SO/총/기관단총SO.asset";
#endif

    [SerializeField] private GunSO gunData;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform aimGraphic;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 leftMuzzleLocalOffset = new Vector2(0.2f, -0.11f);
    [SerializeField, Min(0f)] private float leftFireDeadZone = 0.05f;
    [SerializeField, Min(0f)] private float projectileScale = 0.45f;
    [SerializeField] private Vector2 projectileVisualLocalOffset = new Vector2(-0.03f, -0.29f);
    [SerializeField] private int projectileSortingOrder = 5;
    [SerializeField, Min(0f)] private float destroyViewportPadding = 0.1f;
    [SerializeField, Min(0f)] private float maxProjectileLifetime = 5f;
    [SerializeField] private Vector2 fallbackMuzzleLocalOffset = new Vector2(1.4f, 0.28f);
    [SerializeField, InspectorName("장전 애니메이션 부모")] private Transform reloadAnimationParent;
    [SerializeField, InspectorName("장전 애니메이션 위치")] private Vector2 reloadAnimationLocalOffset = Vector2.zero;
    [SerializeField, InspectorName("장전 상승 높이"), Min(0f)] private float reloadAnimationRiseHeight = 1f;
    [SerializeField, InspectorName("장전 상승 시간 비율"), Range(0.05f, 0.8f)] private float reloadAnimationRisePortion = 0.25f;

    private float nextShotTime;
    private int currentBulletCount;
    private bool isReloading;
    private float reloadStartedTime;
    private float reloadCompleteTime;
    private GameObject reloadAnimationObject;
#if ENABLE_INPUT_SYSTEM
    private InputAction fireAction;
    private InputAction pointerPositionAction;
#endif

    private void Reset()
    {
        FindReferences();
    }

    private void Awake()
    {
        FindReferences();
        FillBulletCount();
    }

    private void OnEnable()
    {
        FindReferences();
        FillBulletCount();

#if ENABLE_INPUT_SYSTEM
        fireAction ??= new InputAction("Fire", InputActionType.Button, "<Pointer>/press");
        pointerPositionAction ??= new InputAction("PointerPosition", InputActionType.Value, "<Pointer>/position");
        fireAction.Enable();
        pointerPositionAction.Enable();
#endif
    }

    private void OnDisable()
    {
        nextShotTime = 0f;
        isReloading = false;
        reloadStartedTime = 0f;
        reloadCompleteTime = 0f;
        HideReloadAnimation();

#if ENABLE_INPUT_SYSTEM
        fireAction?.Disable();
        pointerPositionAction?.Disable();
#endif
    }

    private void LateUpdate()
    {
        UpdateReload();

        if (!ReadFireHeld())
        {
            if (!isReloading)
            {
                nextShotTime = 0f;
            }

            return;
        }

        if (isReloading || Time.time < nextShotTime || !CanCreateProjectile())
        {
            return;
        }

        if (currentBulletCount <= 0)
        {
            BeginReload();
            return;
        }

        Camera cameraToUse = GetCamera();
        if (cameraToUse == null || !TryReadMouseScreenPosition(out Vector2 mouseScreenPosition))
        {
            return;
        }

        Vector3 mouseWorldPosition = GetMouseWorldPosition(cameraToUse, mouseScreenPosition, transform.position.z);
        bool firingLeft = mouseWorldPosition.x < transform.position.x - leftFireDeadZone;
        Vector3 spawnPosition = GetMuzzleWorldPosition(firingLeft);
        mouseWorldPosition = GetMouseWorldPosition(cameraToUse, mouseScreenPosition, spawnPosition.z);
        Vector2 direction = mouseWorldPosition - spawnPosition;
        if (direction.sqrMagnitude < MinDirectionSqrMagnitude)
        {
            return;
        }

        FireProjectile(spawnPosition, direction.normalized, cameraToUse);
        ConsumeBullet();
        nextShotTime = Time.time + GetShotInterval();
    }

    private void FindReferences()
    {
        if (gunData == null)
        {
            gunData = LoadDefaultGunData();
        }

        if (muzzle == null)
        {
            Transform foundMuzzle = transform.Find("ArmsGun/Muzzle");
            if (foundMuzzle != null)
            {
                muzzle = foundMuzzle;
            }
        }

        if (aimGraphic == null)
        {
            aimGraphic = transform.Find("ArmsGun");
        }

        if (reloadAnimationParent == null)
        {
            reloadAnimationParent = transform;
        }
    }

    private bool CanCreateProjectile()
    {
        return gunData != null && (gunData.BulletPrefab != null || gunData.BulletSprite != null);
    }

    private Camera GetCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera;
        }

        Camera[] cameras = Camera.allCameras;
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private Vector3 GetMuzzleWorldPosition(bool firingLeft)
    {
        if (firingLeft && aimGraphic != null)
        {
            return aimGraphic.TransformPoint(leftMuzzleLocalOffset);
        }

        if (muzzle != null)
        {
            return muzzle.position;
        }

        if (aimGraphic != null)
        {
            return aimGraphic.TransformPoint(fallbackMuzzleLocalOffset);
        }

        return transform.position;
    }

    private void FireProjectile(Vector3 spawnPosition, Vector2 direction, Camera cameraToUse)
    {
        Quaternion rotation = GetRotation(direction);
        GameObject projectileObject = CreateProjectileObject(spawnPosition, rotation);
        if (projectileObject == null)
        {
            return;
        }

        ApplyProjectileSpriteFallback(projectileObject);

        Projectile2D projectile = projectileObject.GetComponent<Projectile2D>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<Projectile2D>();
        }

        projectile.Launch(
            direction,
            GetBulletFireSpeed(),
            cameraToUse,
            destroyViewportPadding,
            maxProjectileLifetime
        );
    }

    private GameObject CreateProjectileObject(Vector3 spawnPosition, Quaternion rotation)
    {
        GameObject bulletPrefab = gunData != null ? gunData.BulletPrefab : null;
        if (bulletPrefab != null)
        {
            return Instantiate(bulletPrefab, spawnPosition, rotation);
        }

        Sprite bulletSprite = gunData != null ? gunData.BulletSprite : null;
        if (bulletSprite == null)
        {
            return null;
        }

        GameObject projectileObject = new GameObject("SubmachineGunShot");
        projectileObject.transform.SetPositionAndRotation(spawnPosition, rotation);

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(projectileObject.transform, false);
        visualObject.transform.localPosition = projectileVisualLocalOffset;
        visualObject.transform.localScale = Vector3.one * projectileScale;

        SpriteRenderer spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = bulletSprite;
        spriteRenderer.sortingOrder = projectileSortingOrder;

        return projectileObject;
    }

    private void ApplyProjectileSpriteFallback(GameObject projectileObject)
    {
        Sprite bulletSprite = gunData != null ? gunData.BulletSprite : null;
        if (projectileObject == null || bulletSprite == null)
        {
            return;
        }

        SpriteRenderer[] spriteRenderers = projectileObject.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && spriteRenderers[i].sprite == null)
            {
                spriteRenderers[i].sprite = bulletSprite;
            }
        }
    }

    private float GetShotInterval()
    {
        return 1f / Mathf.Max(MinAttackSpeed, GetAttackSpeed());
    }

    private float GetAttackSpeed()
    {
        return gunData != null ? gunData.AttackSpeed : MinAttackSpeed;
    }

    private float GetBulletFireSpeed()
    {
        return gunData != null ? gunData.BulletFireSpeed : 0f;
    }

    private int GetMaxBulletCount()
    {
        return gunData != null ? Mathf.Max(MinBulletCount, gunData.BulletCount) : 0;
    }

    private float GetReloadSpeed()
    {
        return gunData != null ? Mathf.Max(0f, gunData.ReloadSpeed) : 0f;
    }

    private void ConsumeBullet()
    {
        currentBulletCount = Mathf.Max(0, currentBulletCount - 1);

        if (currentBulletCount <= 0)
        {
            BeginReload();
        }
    }

    private void BeginReload()
    {
        if (isReloading || gunData == null)
        {
            return;
        }

        float reloadSpeed = GetReloadSpeed();
        if (reloadSpeed <= 0f)
        {
            FillBulletCount();
            return;
        }

        isReloading = true;
        reloadStartedTime = Time.time;
        reloadCompleteTime = Time.time + reloadSpeed;
        ShowReloadAnimation();
    }

    private void UpdateReload()
    {
        if (!isReloading)
        {
            return;
        }

        UpdateReloadAnimationPosition();

        if (Time.time < reloadCompleteTime)
        {
            return;
        }

        FillBulletCount();
    }

    private void FillBulletCount()
    {
        currentBulletCount = GetMaxBulletCount();
        isReloading = false;
        reloadStartedTime = 0f;
        reloadCompleteTime = 0f;
        HideReloadAnimation();
    }

    private void ShowReloadAnimation()
    {
        GameObject reloadAnimationPrefab = gunData != null ? gunData.ReloadAnimationPrefab : null;
        if (reloadAnimationPrefab == null)
        {
            return;
        }

        DestroyReloadAnimationObject();

        Transform parent = reloadAnimationParent != null ? reloadAnimationParent : transform;
        reloadAnimationObject = Instantiate(reloadAnimationPrefab, parent);
        reloadAnimationObject.name = reloadAnimationPrefab.name;

        if (reloadAnimationObject == null)
        {
            return;
        }

        reloadAnimationObject.SetActive(true);
        reloadAnimationObject.transform.localRotation = Quaternion.identity;
        SetReloadAnimationPosition(0f);
    }

    private void UpdateReloadAnimationPosition()
    {
        if (reloadAnimationObject == null || reloadCompleteTime <= reloadStartedTime)
        {
            return;
        }

        float reloadProgress = Mathf.InverseLerp(reloadStartedTime, reloadCompleteTime, Time.time);
        SetReloadAnimationPosition(reloadProgress);
    }

    private void SetReloadAnimationPosition(float reloadProgress)
    {
        if (reloadAnimationObject == null)
        {
            return;
        }

        float risePortion = Mathf.Clamp(reloadAnimationRisePortion, 0.05f, 0.8f);
        float yOffset;
        if (reloadProgress < risePortion)
        {
            float riseProgress = Mathf.SmoothStep(0f, 1f, reloadProgress / risePortion);
            yOffset = Mathf.Lerp(0f, reloadAnimationRiseHeight, riseProgress);
        }
        else
        {
            float descendProgress = Mathf.SmoothStep(0f, 1f, (reloadProgress - risePortion) / (1f - risePortion));
            yOffset = Mathf.Lerp(reloadAnimationRiseHeight, 0f, descendProgress);
        }

        reloadAnimationObject.transform.localPosition = new Vector3(
            reloadAnimationLocalOffset.x,
            reloadAnimationLocalOffset.y + yOffset,
            0f
        );
    }

    private void HideReloadAnimation()
    {
        DestroyReloadAnimationObject();
    }

    private void DestroyReloadAnimationObject()
    {
        if (reloadAnimationObject == null)
        {
            return;
        }

        Destroy(reloadAnimationObject);
        reloadAnimationObject = null;
    }

    private static Quaternion GetRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

    private static Vector3 GetMouseWorldPosition(Camera cameraToUse, Vector2 screenPosition, float worldZ)
    {
        Ray mouseRay = cameraToUse.ScreenPointToRay(screenPosition);
        Plane worldPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, worldZ));
        if (worldPlane.Raycast(mouseRay, out float distance))
        {
            return mouseRay.GetPoint(distance);
        }

        float cameraDistance = Mathf.Abs(worldZ - cameraToUse.transform.position.z);
        return cameraToUse.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, cameraDistance));
    }

    private GunSO LoadDefaultGunData()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GunSO>(DefaultGunDataPath);
#else
        return null;
#endif
    }

    private bool ReadFireHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (fireAction != null && fireAction.IsPressed())
        {
            return true;
        }

        if (Pointer.current != null && Pointer.current.press.isPressed)
        {
            return true;
        }

        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.isPressed;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    private bool TryReadMouseScreenPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (pointerPositionAction != null)
        {
            screenPosition = pointerPositionAction.ReadValue<Vector2>();
            return true;
        }

        if (Pointer.current != null)
        {
            screenPosition = Pointer.current.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = Input.mousePosition;
        return true;
#else
        screenPosition = default;
        return false;
#endif
    }
}
