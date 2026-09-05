using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ArisaStanceCollider : MonoBehaviour
{
    [SerializeField] private ArisaHorizontalMovement movement;
    [SerializeField] private ArisaMouseAim aim;
    [SerializeField] private BoxCollider2D targetCollider;
    [SerializeField] private Vector2 standingSize = new Vector2(0.9f, 2.65f);
    [SerializeField] private Vector2 standingOffset = new Vector2(-0.15f, -1.05f);
    [SerializeField] private Vector2 crouchFrame1Size = new Vector2(1.15f, 1.55f);
    [SerializeField] private Vector2 crouchFrame1Offset = new Vector2(-0.05f, -1.35f);
    [SerializeField] private Vector2 leftCrouchFrame1OffsetAdjustment;
    [SerializeField] private Vector2 crouchFrame2Size = new Vector2(1.35f, 1.15f);
    [SerializeField] private Vector2 crouchFrame2Offset = new Vector2(0.05f, -1.5f);
    [SerializeField] private Vector2 leftCrouchFrame2OffsetAdjustment;

    private void Reset()
    {
        FindReferences();
        ApplyCurrentStance();
    }

    private void Awake()
    {
        FindReferences();
        ApplyCurrentStance();
    }

    private void LateUpdate()
    {
        ApplyCurrentStance();
    }

    private void OnValidate()
    {
        FindReferences();
        ApplyCurrentStance();
    }

    private void FindReferences()
    {
        if (movement == null)
        {
            movement = GetComponent<ArisaHorizontalMovement>();
        }

        if (targetCollider == null)
        {
            targetCollider = GetComponent<BoxCollider2D>();
        }

        if (aim == null)
        {
            aim = GetComponent<ArisaMouseAim>();
        }
    }

    public void ApplyCurrentStance()
    {
        if (targetCollider == null)
        {
            return;
        }

        if (movement == null || !movement.IsCrouching)
        {
            ApplyCollider(standingSize, standingOffset);
            return;
        }

        if (movement.CrouchFrameIndex <= 0)
        {
            ApplyCollider(crouchFrame1Size, GetCrouchOffset(crouchFrame1Offset, leftCrouchFrame1OffsetAdjustment));
            return;
        }

        ApplyCollider(crouchFrame2Size, GetCrouchOffset(crouchFrame2Offset, leftCrouchFrame2OffsetAdjustment));
    }

    private void ApplyCollider(Vector2 size, Vector2 offset)
    {
        targetCollider.size = size;
        targetCollider.offset = offset;
    }

    private Vector2 GetCrouchOffset(Vector2 rightOffset, Vector2 leftAdjustment)
    {
        if (aim == null || !aim.IsFacingLeft)
        {
            return rightOffset;
        }

        return new Vector2(aim.MirrorLocalX(rightOffset.x), rightOffset.y) + leftAdjustment;
    }
}
