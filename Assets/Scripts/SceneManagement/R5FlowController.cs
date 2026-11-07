using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Stages the r5 memory, blackout, return to adulthood, and doctor's explanation.</summary>
public sealed class R5FlowController : MonoBehaviour
{
    private const string ParentsConversation = "R5_父母_离婚";
    private const string SiblingsConversation = "R5_兄妹_安慰";
    private const string DoctorConversation = "R5_医生_说明";

    [SerializeField] private MapRingController ring;
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private SpriteRenderer adultPlayerRenderer;
    [SerializeField] private SpriteRenderer playerVisualRenderer;
    [SerializeField] private Camera mapCamera;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private Transform youngHua;
    [SerializeField] private SpriteRenderer fatherRenderer;
    [SerializeField] private SpriteRenderer motherRenderer;
    [SerializeField] private SpriteRenderer sisterRenderer;
    [SerializeField] private SpriteRenderer brotherRenderer;
    [SerializeField] private Transform doctor;
    [SerializeField] private SpriteRenderer blackoutRenderer;
    [SerializeField] private BoxCollider2D exitPortalCollider;

    private bool initialized;
    private bool entered;
    private bool storyComplete;
    private string activeConversation;
    private int conversationStartFrame;
    private float initialCameraSize;
    private Vector3 youngBaseLocalPosition;
    private Coroutine zoomRoutine;
    private Coroutine trembleRoutine;

    private void Awake()
    {
        if (exitPortalCollider == null)
        {
            Transform exit = transform.Find("ForwardPortal_to_r6_North");
            if (exit != null) exitPortalCollider = exit.GetComponent<BoxCollider2D>();
        }
        if (ring == null || player == null || playerBody == null || adultPlayerRenderer == null || playerVisualRenderer == null ||
            mapCamera == null || mapFadeOverlay == null || youngHua == null ||
            fatherRenderer == null || motherRenderer == null || sisterRenderer == null ||
            brotherRenderer == null || doctor == null || blackoutRenderer == null || exitPortalCollider == null)
        {
            Debug.LogError("R5FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        youngBaseLocalPosition = youngHua.localPosition;
        initialized = true;
    }

    private void OnEnable()
    {
        if (initialized) StartCoroutine(CheckRoomAfterMapInitialization());
    }

    private IEnumerator CheckRoomAfterMapInitialization()
    {
        yield return null;
        if (!isActiveAndEnabled || ring.CurrentRoom != 5) yield break;
        entered = true;
        initialCameraSize = mapCamera.orthographicSize;
        Transform exit = transform.Find("ForwardPortal_to_r6_North");
        if (exit != null) exitPortalCollider = exit.GetComponent<BoxCollider2D>();
        if (exitPortalCollider != null) exitPortalCollider.enabled = storyComplete;

        if (storyComplete)
        {
            ShowFinishedRoom();
            yield break;
        }
        PrepareMemoryScene();
        StartCoroutine(BeginAfterMapFade());
    }

    private void PrepareMemoryScene()
    {
        player.SetCanMove(false);
        Vector3 childWorld = youngHua.position;
        playerBody.velocity = Vector2.zero;
        playerBody.position = childWorld;
        player.transform.position = childWorld;
        Physics2D.SyncTransforms();
        adultPlayerRenderer.enabled = false;
        playerVisualRenderer.enabled = false;
        youngHua.gameObject.SetActive(true);
        youngHua.localPosition = youngBaseLocalPosition;
        fatherRenderer.gameObject.SetActive(true);
        motherRenderer.gameObject.SetActive(true);
        SetAlpha(fatherRenderer, 1f);
        SetAlpha(motherRenderer, 1f);
        sisterRenderer.gameObject.SetActive(false);
        brotherRenderer.gameObject.SetActive(false);
        doctor.gameObject.SetActive(false);
        SetAlpha(blackoutRenderer, 0f);
    }

    private IEnumerator BeginAfterMapFade()
    {
        yield return null;
        while (isActiveAndEnabled && mapFadeOverlay.blocksRaycasts) yield return null;
        while (isActiveAndEnabled && !DialogueManager.hasInstance) yield return null;
        if (isActiveAndEnabled && ring.CurrentRoom == 5 && StartStoryConversation(ParentsConversation, player.transform, fatherRenderer.transform))
        {
            zoomRoutine = StartCoroutine(ZoomDuringParents());
            trembleRoutine = StartCoroutine(TrembleYoungHua());
        }
    }

    private void Update()
    {
        if (ring.CurrentRoom == 5 && activeConversation != null)
            ContinueDialogueInput();
    }

    private bool StartStoryConversation(string title, Transform actor, Transform conversant)
    {
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive || activeConversation != null) return false;
        activeConversation = title;
        conversationStartFrame = Time.frameCount;
        player.SetCanMove(false);
        DialogueManager.instance.conversationEnded += OnConversationEnded;
        DialogueManager.StartConversation(title, actor, conversant);
        return true;
    }

    private void OnConversationEnded(Transform actor)
    {
        if (activeConversation == null || DialogueManager.lastConversationStarted != activeConversation) return;
        string ended = activeConversation;
        activeConversation = null;
        DialogueManager.instance.conversationEnded -= OnConversationEnded;
        if (ended == ParentsConversation)
        {
            if (zoomRoutine != null) StopCoroutine(zoomRoutine);
            if (trembleRoutine != null) StopCoroutine(trembleRoutine);
            youngHua.localPosition = youngBaseLocalPosition;
            StartCoroutine(AfterParents());
        }
        else if (ended == SiblingsConversation) StartCoroutine(AfterSiblings());
        else if (ended == DoctorConversation)
        {
            storyComplete = true;
            if (exitPortalCollider != null) exitPortalCollider.enabled = true;
            player.SetCanMove(true);
        }
    }

    private IEnumerator ZoomDuringParents()
    {
        Vector3 from = mapCamera.transform.position;
        Vector3 to = new Vector3(youngHua.position.x, youngHua.position.y, from.z);
        float elapsed = 0f;
        const float duration = 18f;
        while (activeConversation == ParentsConversation)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            mapCamera.orthographicSize = Mathf.Lerp(initialCameraSize, 2.8f, t);
            mapCamera.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
    }

    private IEnumerator TrembleYoungHua()
    {
        while (activeConversation == ParentsConversation)
        {
            youngHua.localPosition = youngBaseLocalPosition +
                new Vector3(Mathf.Sin(Time.unscaledTime * 26f) * 0.035f, 0f, 0f);
            yield return null;
        }
    }

    private IEnumerator AfterParents()
    {
        yield return MoveCameraToChild(0.7f);
        yield return FadeBlack(1f, 1.2f);
        youngHua.gameObject.SetActive(false);
        playerVisualRenderer.enabled = true;
        sisterRenderer.gameObject.SetActive(true);
        brotherRenderer.gameObject.SetActive(true);
        SetAlpha(sisterRenderer, 1f);
        SetAlpha(brotherRenderer, 1f);
        mapCamera.orthographicSize = initialCameraSize;
        mapCamera.transform.position = new Vector3(transform.position.x, transform.position.y,
            mapCamera.transform.position.z);
        StartStoryConversation(SiblingsConversation, player.transform, brotherRenderer.transform);
    }

    private IEnumerator AfterSiblings()
    {
        yield return FadeBlack(0f, 1.2f);
        yield return FadeFourActors(2f);
        fatherRenderer.gameObject.SetActive(false);
        motherRenderer.gameObject.SetActive(false);
        sisterRenderer.gameObject.SetActive(false);
        brotherRenderer.gameObject.SetActive(false);
        doctor.gameObject.SetActive(true);
        StartStoryConversation(DoctorConversation, player.transform, doctor);
    }

    private IEnumerator MoveCameraToChild(float duration)
    {
        Vector3 from = mapCamera.transform.position;
        Vector3 to = new Vector3(youngHua.position.x, youngHua.position.y, from.z);
        float sizeFrom = mapCamera.orthographicSize;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            mapCamera.transform.position = Vector3.Lerp(from, to, t);
            mapCamera.orthographicSize = Mathf.Lerp(sizeFrom, 2.8f, t);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        mapCamera.transform.position = to;
        mapCamera.orthographicSize = 2.8f;
    }

    private IEnumerator FadeBlack(float target, float duration)
    {
        float from = blackoutRenderer.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetAlpha(blackoutRenderer, Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / duration)));
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        SetAlpha(blackoutRenderer, target);
    }

    private IEnumerator FadeFourActors(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            SetAlpha(fatherRenderer, alpha);
            SetAlpha(motherRenderer, alpha);
            SetAlpha(sisterRenderer, alpha);
            SetAlpha(brotherRenderer, alpha);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        SetAlpha(fatherRenderer, 0f);
        SetAlpha(motherRenderer, 0f);
        SetAlpha(sisterRenderer, 0f);
        SetAlpha(brotherRenderer, 0f);
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    private void ShowFinishedRoom()
    {
        adultPlayerRenderer.enabled = false;
        playerVisualRenderer.enabled = true;
        youngHua.gameObject.SetActive(false);
        fatherRenderer.gameObject.SetActive(false);
        motherRenderer.gameObject.SetActive(false);
        sisterRenderer.gameObject.SetActive(false);
        brotherRenderer.gameObject.SetActive(false);
        doctor.gameObject.SetActive(true);
        SetAlpha(blackoutRenderer, 0f);
    }

    private void ContinueDialogueInput()
    {
        if (Time.frameCount == conversationStartFrame || !DialogueManager.isConversationActive ||
            DialogueManager.lastConversationStarted != activeConversation ||
            DialogueManager.conversationView == null ||
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

    private void OnDisable()
    {
        if (DialogueManager.hasInstance)
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
        activeConversation = null;
        if (!entered) return;
        mapCamera.orthographicSize = initialCameraSize;
        adultPlayerRenderer.enabled = false;
        playerVisualRenderer.enabled = true;
        SetAlpha(blackoutRenderer, 0f);
        entered = false;
    }
}
