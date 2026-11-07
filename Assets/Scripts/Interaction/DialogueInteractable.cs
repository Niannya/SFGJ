using PixelCrushers.DialogueSystem;
using System.Collections;
using UnityEngine;

public sealed class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string conversationTitle;
    [SerializeField] private PlayerController playerController;

    public void Interact(InteractionUI interactionUI)
    {
        if (playerController == null || string.IsNullOrWhiteSpace(conversationTitle))
        {
            Debug.LogWarning("DialogueInteractable needs a player and conversation title.", this);
            return;
        }
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive) return;

        playerController.SetCanMove(false);
        DialogueManager.StartConversation(conversationTitle, playerController.transform, transform);
        if (!DialogueManager.isConversationActive) playerController.SetCanMove(true);
    }

    private void OnConversationEnd(Transform actor)
    {
        if (playerController != null)
            StartCoroutine(UnlockAfterInteractionKeyReleased());
    }

    private IEnumerator UnlockAfterInteractionKeyReleased()
    {
        yield return null;
        while (Input.GetKey(KeyCode.Z))
            yield return null;

        if (playerController != null && !DialogueManager.isConversationActive)
            playerController.SetCanMove(true);
    }
}
