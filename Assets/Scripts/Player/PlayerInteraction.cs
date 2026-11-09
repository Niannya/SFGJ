using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private LayerMask interactionLayers;

    private PlayerController playerController;
    [SerializeField] private InteractionUI interactionUI;

    public Transform InteractionOrigin => interactionOrigin;
    public float InteractionDistance => interactionDistance;
    public LayerMask InteractionLayers => interactionLayers;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (playerController == null) return;
        Vector2 direction = playerController.FacingDirection;
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (interactionUI != null && interactionUI.IsVisible)
            {
                interactionUI.Hide();
                playerController.SetCanMove(true);
                return;
            }
            if (!playerController.CanMove) return;

            // 关卡元素（钥匙/NPC）的碰撞体是 trigger，需临时开启 trigger 检测
            bool prevQueriesHitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;
            RaycastHit2D hit = Physics2D.Raycast(interactionOrigin.position, direction, interactionDistance, interactionLayers);
            Physics2D.queriesHitTriggers = prevQueriesHitTriggers;

            if (hit.collider != null)
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(interactionUI);
                    if (interactionUI != null && interactionUI.IsVisible) playerController.SetCanMove(false);
                }
            }
        }
    }
}
