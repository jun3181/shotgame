using UnityEngine;
using UnityEngine.Serialization;

public readonly struct GunFireContext
{
    public GunFireContext(
        Vector3 muzzlePosition,
        Vector2 direction,
        Camera camera,
        MonoBehaviour coroutineRunner
    )
    {
        MuzzlePosition = muzzlePosition;
        Direction = direction;
        Camera = camera;
        CoroutineRunner = coroutineRunner;
        Owner = coroutineRunner != null ? coroutineRunner.transform : null;
    }

    public Vector3 MuzzlePosition { get; }
    public Vector2 Direction { get; }
    public Camera Camera { get; }
    public MonoBehaviour CoroutineRunner { get; }
    public Transform Owner { get; }
}

[CreateAssetMenu(fileName = "총SO", menuName = "Shotgame/총/공통 총")]
public class GunSO : ScriptableObject
{
    [FormerlySerializedAs("bulletPrefab")]
    [SerializeField, KoreanLabel("총알 프리팹")] private GameObject 총알프리팹;

    [FormerlySerializedAs("bulletSprite")]
    [SerializeField, KoreanLabel("총알 스프라이트")] private Sprite 총알스프라이트;

    [FormerlySerializedAs("bulletCount")]
    [SerializeField, KoreanLabel("총알 갯수"), Min(1)] private int 총알갯수 = 1;

    [FormerlySerializedAs("attackSpeed")]
    [SerializeField, KoreanLabel("공격 속도"), Min(0.1f)] private float 공격속도 = 1f;

    [FormerlySerializedAs("bulletFireSpeed")]
    [SerializeField, KoreanLabel("총알 발사 속도"), Min(0f)] private float 총알발사속도 = 12f;

    [FormerlySerializedAs("damage")]
    [SerializeField, KoreanLabel("데미지"), Min(0f)] private float 데미지;

    [FormerlySerializedAs("reloadSpeed")]
    [SerializeField, KoreanLabel("장전 속도"), Min(0f)] private float 장전속도 = 1f;

    [FormerlySerializedAs("reloadAnimationPrefab")]
    [SerializeField, KoreanLabel("탄창 애니메이션 프리팹")] private GameObject 장전애니메이션프리팹;

    [FormerlySerializedAs("projectileScale")]
    [SerializeField, KoreanLabel("탄환 크기"), Min(0f)] private float 탄환크기 = 0.45f;

    [FormerlySerializedAs("projectileVisualLocalOffset")]
    [SerializeField, KoreanLabel("탄환 시각 위치 보정")] private Vector2 탄환시각위치보정 = new Vector2(-0.03f, -0.29f);

    [FormerlySerializedAs("projectileSortingOrder")]
    [SerializeField, KoreanLabel("탄환 정렬 순서")] private int 탄환정렬순서 = 5;

    [FormerlySerializedAs("destroyViewportPadding")]
    [SerializeField, KoreanLabel("화면 밖 삭제 여유"), Min(0f)] private float 화면밖삭제여유 = 0.1f;

    [FormerlySerializedAs("maxProjectileLifetime")]
    [SerializeField, KoreanLabel("탄환 최대 생존 시간"), Min(0f)] private float 탄환최대생존시간 = 5f;

    [FormerlySerializedAs("fireLogic")]
    [SerializeReference, KoreanLabel("발사 로직")] private GunFireLogic 발사로직 = new StraightProjectileFireLogic();

    public GameObject BulletPrefab => 총알프리팹;
    public Sprite BulletSprite => 총알스프라이트;
    public int BulletCount => 총알갯수;
    public float AttackSpeed => 공격속도;
    public float BulletFireSpeed => 총알발사속도;
    public float Damage => 데미지;
    public float ReloadSpeed => 장전속도;
    public GameObject ReloadAnimationPrefab => 장전애니메이션프리팹;
    public float ProjectileScale => 탄환크기;
    public Vector2 ProjectileVisualLocalOffset => 탄환시각위치보정;
    public int ProjectileSortingOrder => 탄환정렬순서;
    public float DestroyViewportPadding => 화면밖삭제여유;
    public float MaxProjectileLifetime => 탄환최대생존시간;
    public GunFireLogic FireLogic => 발사로직;

    public bool CanFire => 발사로직 != null && 발사로직.CanFire(this);
    public bool ReloadsSequentially => 발사로직 != null && 발사로직.ReloadsSequentially;
    public bool CanFireWhileReloading => 발사로직 != null && 발사로직.CanFireWhileReloading;
    public bool InterruptsReloadWhenFired => 발사로직 != null && 발사로직.InterruptsReloadWhenFired;
    public float SequentialReloadDelay => 발사로직 != null ? Mathf.Max(0f, 발사로직.SequentialReloadDelay) : 0f;

    public void Fire(GunFireContext context)
    {
        if (발사로직 == null)
        {
            return;
        }

        발사로직.Fire(this, context);
    }
}
