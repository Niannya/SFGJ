using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>Runs the first-visit clinic dialogue, then opens the existing r0 to r1 portal.</summary>
public sealed class R0FlowController : MonoBehaviour
{
    [SerializeField] private string conversationTitle = "R0_诊疗室";
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform doctor;
    [SerializeField] private BoxCollider2D exitPortalCollider;
    [SerializeField] private CanvasGroup mapFadeOverlay;

    private bool complete;
    private bool started;

    private void Awake()
    {
        // The map topology editor tool recreates portal children; recover its replacement by name.
        if (exitPortalCollider == null)
        {
            Transform exit = transform.Find("ForwardPortal_to_r1_East");
            if (exit != null) exitPortalCollider = exit.GetComponent<BoxCollider2D>();
        }
        if (player == null || doctor == null || exitPortalCollider == null || mapFadeOverlay == null)
        {
            Debug.LogError("R0FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        player.SetCanMove(false);
        exitPortalCollider.enabled = false;
    }

    private void OnEnable()
    {
        if (!complete && player != null && exitPortalCollider != null && mapFadeOverlay != null)
            StartCoroutine(BeginAfterReveal());
    }

    private IEnumerator BeginAfterReveal()
    {
        // MapRingController owns the scene-entry fade. Begin after it releases the overlay.
        yield return null;
        while (isActiveAndEnabled && mapFadeOverlay.blocksRaycasts) yield return null;
        while (isActiveAndEnabled && !DialogueManager.hasInstance) yield return null;
        if (!isActiveAndEnabled || complete || started) yield break;

        started = true;
        DialogueManager.instance.conversationEnded += OnConversationEnded;
        DialogueManager.StartConversation(conversationTitle, player.transform, doctor);
    }

    private void Update()
    {
        if (!started || complete || !DialogueManager.isConversationActive ||
            DialogueManager.conversationView == null ||
            !DialogueManager.conversationView.isWaitingForContinue ||
            (DialogueManager.lastConversationStarted != conversationTitle)) return;

        bool zPressed = Input.GetKeyDown(KeyCode.Z);
        bool mousePressed = Input.GetMouseButtonDown(0);
        if (!zPressed && !mousePressed) return;

        var state = DialogueManager.currentConversationState;
        var ui = DialogueManager.dialogueUI as StandardDialogueUI;
        if (state == null || state.subtitle == null || ui == null) return;

        DialogueActor speaker;
        var panel = ui.conversationUIElements.standardSubtitleControls.GetPanel(state.subtitle, out speaker);
        if (panel == null || panel.continueButton == null || !panel.continueButton.interactable) return;
        if (mousePressed && IsClickOnButton()) return;

        var fastForward = panel.continueButton.GetComponent<StandardUIContinueButtonFastForward>();
        if (fastForward != null) fastForward.OnFastForward();
        else panel.OnContinue();
    }

    private static bool IsClickOnButton()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;
        var pointer = new PointerEventData(eventSystem) { position = Input.mousePosition };
        var hits = new List<RaycastResult>();
        eventSystem.RaycastAll(pointer, hits);
        if (hits.Count == 0) return false;
        var button = hits[0].gameObject.GetComponentInParent<Button>();
        return button != null && button.isActiveAndEnabled && button.interactable;
    }

    private void OnConversationEnded(Transform actor)
    {
        if (complete || DialogueManager.lastConversationStarted != conversationTitle) return;
        complete = true;
        DialogueManager.instance.conversationEnded -= OnConversationEnded;
        exitPortalCollider.enabled = true;
        player.SetCanMove(true);
    }

    private void OnDisable()
    {
        if (DialogueManager.hasInstance)
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
    }
}
