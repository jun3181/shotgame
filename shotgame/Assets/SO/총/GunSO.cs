using UnityEngine;
using UnityEngine.Serialization;

public abstract class GunSO : ScriptableObject
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

    public GameObject BulletPrefab => 총알프리팹;
    public Sprite BulletSprite => 총알스프라이트;
    public int BulletCount => 총알갯수;
    public float AttackSpeed => 공격속도;
    public float BulletFireSpeed => 총알발사속도;
    public float Damage => 데미지;
    public float ReloadSpeed => 장전속도;
    public GameObject ReloadAnimationPrefab => 장전애니메이션프리팹;
}
