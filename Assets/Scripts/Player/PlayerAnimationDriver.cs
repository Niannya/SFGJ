using UnityEngine;

/// <summary>Feeds Player movement to the shared visual's Animator Controller.</summary>
public sealed class PlayerAnimationDriver : MonoBehaviour
{
    private static readonly int Direction = Animator.StringToHash("Direction");
    private static readonly int Moving = Animator.StringToHash("Moving");

    [SerializeField] private PlayerController player;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (player == null || visual == null || animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("PlayerAnimationDriver has an unassigned reference.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        Vector2 facing = player.FacingDirection;
        bool moving = player.CanMove && player.MoveInput.sqrMagnitude > 0.001f;
        int direction;

        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y))
        {
            visual.flipX = facing.x < 0f;
            direction = 2;
        }
        else
        {
            visual.flipX = false;
            direction = facing.y > 0f ? 1 : 0;
        }

        animator.SetInteger(Direction, direction);
        animator.SetBool(Moving, moving);
    }
}
