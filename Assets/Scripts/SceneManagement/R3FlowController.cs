using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Plays the playground entrance memory and opens the existing r3 to r4 portal.</summary>
public sealed class R3FlowController : MonoBehaviour
{
    private const string FamilyConversation = "R3_游乐场_入园";
    private const string DoctorConversation = "R3_医生_游乐场";

    [SerializeField] private MapRingController ring;
    [SerializeField] private PlayerController player;
    [SerializeField] private Camera mapCamera;
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private Transform sister;
    [SerializeField] private Transform father;
    [SerializeField] private Transform doctor;
    [SerializeField] private BoxCollider2D exitPortalCollider;

    private bool initialized;
    private bool familyComplete;
    private bool doctorComplete;
    private string activeConversation;
    private int conversationStartFrame;

    private void Awake()
    {
        if (mapCamera == null) mapCamera = Camera.main;
        if (background == null)
        {
            Transform image = transform.Find("R3_PlaygroundEntrance_Background");
            if (image != null) background = image.GetComponent<SpriteRenderer>();
        }
        if (exitPortalCollider == null)
        {
            Transform exit = transform.Find("ForwardPortal_to_r4_West");
            if (exit != null) exitPortalCollider = exit.GetComponent<BoxCollider2D>();
        }
        if (ring == null || player == null || mapCamera == null || background == null ||
            mapFadeOverlay == null || sister == null ||
            father == null || doctor == null || exitPortalCollider == null)
        {
            Debug.LogError("R3FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        initialized = true;
    }

    private void OnEnable()
    {
        if (initialized) StartCoroutine(CheckRoomAfterMapInitialization());
    }

    private IEnumerator CheckRoomAfterMapInitialization()
    {
        // All room roots start active in the editor. Wait for MapRingController's Start
        // (or its room transition) before deciding whether r3 is the current room.
        yield return null;
        if (!isActiveAndEnabled || ring.CurrentRoom != 3) yield break;
        PrepareExit();
        if (!familyComplete) player.SetCanMove(false);
        StartCoroutine(BeginAfterMapFade());
    }

    private void PrepareExit()
    {
        // Preserve the exit's scene-authored position for the current background.
        // Only its availability changes when the family conversation completes.
        Transform exit = transform.Find("ForwardPortal_to_r4_West");
        if (exit == null) return;
        exitPortalCollider = exit.GetComponent<BoxCollider2D>();
        if (exitPortalCollider != null) exitPortalCollider.enabled = familyComplete;
    }

    private IEnumerator BeginAfterMapFade()
    {
        yield return null;
        while (isActiveAndEnabled && mapFadeOverlay.blocksRaycasts) yield return null;
        while (isActiveAndEnabled && !DialogueManager.hasInstance) yield return null;
        if (isActiveAndEnabled && ring.CurrentRoom == 3 && !familyComplete)
            StartStoryConversation(FamilyConversation, sister, father);
    }

    private void Update()
    {
        if (!initialized || ring.CurrentRoom != 3) return;
        if (activeConversation != null)
        {
            ContinueDialogueInput();
            return;
        }
        if (!familyComplete || doctorComplete || !player.CanMove || !Input.GetKeyDown(KeyCode.Z)) return;
        if (Vector2.Distance(player.transform.position, doctor.position) <= 1.6f)
            StartStoryConversation(DoctorConversation, player.transform, doctor);
    }

    private void LateUpdate()
    {
        if (!initialized || ring.CurrentRoom != 3) return;
        Bounds imageBounds = background.bounds;
        float cameraHalfWidth = mapCamera.orthographicSize * mapCamera.aspect;
        float minX = imageBounds.min.x + cameraHalfWidth;
        float maxX = imageBounds.max.x - cameraHalfWidth;
        float cameraX = minX <= maxX
            ? Mathf.Clamp(player.transform.position.x, minX, maxX)
            : imageBounds.center.x;
        Vector3 current = mapCamera.transform.position;
        mapCamera.transform.position = new Vector3(cameraX, transform.position.y, current.z);
    }

    private void StartStoryConversation(string title, Transform actor, Transform conversant)
    {
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive || activeConversation != null) return;
        activeConversation = title;
        conversationStartFrame = Time.frameCount;
        player.SetCanMove(false);
        DialogueManager.instance.conversationEnded += OnConversationEnded;
        DialogueManager.StartConversation(title, actor, conversant);
    }

    private void OnConversationEnded(Transform actor)
    {
        if (activeConversation == null || DialogueManager.lastConversationStarted != activeConversation) return;
        string ended = activeConversation;
        activeConversation = null;
        DialogueManager.instance.conversationEnded -= OnConversationEnded;
        if (ended == FamilyConversation)
        {
            familyComplete = true;
            if (exitPortalCollider != null) exitPortalCollider.enabled = true;
        }
        else if (ended == DoctorConversation) doctorComplete = true;
        player.SetCanMove(true);
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
    }
}
