using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Advances the opening dialogue and fades to the first map.</summary>
public sealed class OpeningFlowController : MonoBehaviour
{
    [SerializeField] private string conversationTitle = "Opening";
    [SerializeField] private string nextSceneName = "NineMapScene";

    private SceneFader sceneFader;
    private bool transitioning;

    private void Awake()
    {
        var canvasObject = new GameObject("OpeningFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var overlayObject = new GameObject("BlackFadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        var rect = overlayObject.GetComponent<RectTransform>();
        rect.SetParent(canvasObject.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = overlayObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        var overlay = overlayObject.GetComponent<CanvasGroup>();
        overlay.blocksRaycasts = false;
        sceneFader = canvasObject.AddComponent<SceneFader>();
        sceneFader.Configure(overlay);
    }

    private void OnEnable()
    {
        // The actor of the Opening conversation is OpeningFlow, so it receives
        // OnConversationEnd. The event also handles a later change of actor.
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (enabled && !DialogueManager.hasInstance) yield return null;
        if (enabled && DialogueManager.hasInstance)
            DialogueManager.instance.conversationEnded += OnConversationEnded;
    }

    private void OnDisable()
    {
        if (DialogueManager.hasInstance)
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
    }

    private void Update()
    {
        if (transitioning || sceneFader == null || sceneFader.IsFadingIn ||
            !DialogueManager.isConversationActive || DialogueManager.conversationView == null ||
            !DialogueManager.conversationView.isWaitingForContinue) return;

        bool zPressed = Input.GetKeyDown(KeyCode.Z);
        bool mousePressed = Input.GetMouseButtonDown(0);
        if (!zPressed && !mousePressed) return;

        var state = DialogueManager.currentConversationState;
        var ui = DialogueManager.dialogueUI as StandardDialogueUI;
        if (state == null || state.subtitle == null || ui == null) return;

        DialogueActor speaker;
        var panel = ui.conversationUIElements.standardSubtitleControls.GetPanel(state.subtitle, out speaker);
        if (panel == null || panel.continueButton == null || !panel.continueButton.interactable) return;

        // A click on an actual UI Button is handled on mouse-up by the EventSystem.
        // Sending OnFastForward here as well would advance two lines per click.
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

    // Also called as a message on the conversation actor by Dialogue System.
    private void OnConversationEnd(Transform actor) => OnConversationEnded(actor);

    private void OnConversationEnded(Transform actor)
    {
        if (transitioning || DialogueManager.lastConversationStarted != conversationTitle) return;
        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        transitioning = true;
        if (sceneFader != null) yield return sceneFader.FadeOut();
        SceneManager.LoadScene(nextSceneName);
    }
}
