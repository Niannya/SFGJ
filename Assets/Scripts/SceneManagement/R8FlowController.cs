using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Plays HeYe's bedroom memory and opens the second-cycle route to R1.</summary>
public sealed class R8FlowController : MonoBehaviour
{
    private const string FamilyConversation = "R8_妹妹与父亲_故事";
    private const string DoctorConversation = "R8_医生_二周目";

    private static int playSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginPlaySession() => playSession++;

    [SerializeField] private MapRingController ring;
    [SerializeField] private DialogueDatabase r8Database;
    [SerializeField] private PlayerController player;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private Transform sister;
    [SerializeField] private Transform father;
    [SerializeField] private Transform doctor;
    [SerializeField] private GameObject drawingSymbols;
    [SerializeField] private BoxCollider2D exitPortalCollider;

    private SpriteRenderer doctorRenderer;
    private bool initialized;
    private int initializedSession = -1;
    private bool entered;
    private bool familyFinished;
    private bool doctorFinished;
    private bool exitUnlocked;
    private string pendingConversation;
    private Transform pendingActor;
    private Transform pendingConversant;
    private string activeConversation;
    private int conversationStartFrame;
    private bool reportedMissingConversation;

    private void Awake() => Initialize();
    private void OnEnable() => Initialize();

    private void Initialize()
    {
        if (initialized && initializedSession == playSession) return;
        if (exitPortalCollider == null)
        {
            Transform portal = transform.Find("ForwardPortal_to_r1_South");
            if (portal != null) exitPortalCollider = portal.GetComponent<BoxCollider2D>();
        }
        doctorRenderer = doctor == null ? null : doctor.GetComponent<SpriteRenderer>();
        if (ring == null || r8Database == null || player == null || mapFadeOverlay == null ||
            sister == null || father == null || doctor == null || drawingSymbols == null ||
            doctorRenderer == null || exitPortalCollider == null)
        {
            Debug.LogError("R8FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        if (initializedSession != playSession)
        {
            entered = false;
            familyFinished = false;
            doctorFinished = false;
            exitUnlocked = false;
            reportedMissingConversation = false;
            activeConversation = null;
            pendingConversation = null;
            pendingActor = null;
            pendingConversant = null;
        }
        initializedSession = playSession;
        initialized = true;
    }

    private void PrepareRoomEntry()
    {
        entered = true;
        ApplyVisuals();
        if (!familyFinished) QueueConversation(FamilyConversation, sister, father);
        else if (!doctorFinished) QueueConversation(DoctorConversation, player.transform, doctor);
    }

    private void ApplyVisuals()
    {
        drawingSymbols.SetActive(familyFinished);
        doctor.gameObject.SetActive(familyFinished && !doctorFinished);
        SetDoctorAlpha(1f);
        exitPortalCollider.enabled = exitUnlocked;
    }

    private void SetDoctorAlpha(float alpha)
    {
        Color color = doctorRenderer.color;
        color.a = alpha;
        doctorRenderer.color = color;
    }

    private void QueueConversation(string title, Transform actor, Transform conversant)
    {
        pendingConversation = title;
        pendingActor = actor;
        pendingConversant = conversant;
        reportedMissingConversation = false;
        player.SetCanMove(false);
    }

    private void Update()
    {
        if (!initialized) Initialize();
        if (!initialized || ring.CurrentRoom != 8) return;
        if (!entered) PrepareRoomEntry();

        if (pendingConversation != null)
        {
            if (!mapFadeOverlay.blocksRaycasts && DialogueManager.hasInstance &&
                !DialogueManager.isConversationActive &&
                StartStoryConversation(pendingConversation, pendingActor, pendingConversant))
            {
                pendingConversation = null;
                pendingActor = null;
                pendingConversant = null;
            }
            return;
        }
        if (activeConversation != null) ContinueDialogueInput();
    }

    private bool StartStoryConversation(string title, Transform actor, Transform conversant)
    {
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive || activeConversation != null) return false;
        if (!EnsureConversationAvailable(title))
        {
            if (!reportedMissingConversation)
            {
                reportedMissingConversation = true;
                Debug.LogError($"R8 conversation has no usable dialogue: {title}", this);
            }
            player.SetCanMove(true);
            return false;
        }
        activeConversation = title;
        conversationStartFrame = Time.frameCount;
        player.SetCanMove(false);
        DialogueManager.instance.conversationEnded += OnConversationEnded;
        DialogueManager.StartConversation(title, actor, conversant);
        if (!DialogueManager.isConversationActive || DialogueManager.lastConversationStarted != title)
        {
            DialogueManager.instance.conversationEnded -= OnConversationEnded;
            activeConversation = null;
            player.SetCanMove(true);
            Debug.LogError($"R8 conversation did not start: {title}", this);
            return false;
        }
        return true;
    }

    private bool EnsureConversationAvailable(string title)
    {
        DialogueDatabase runtimeDatabase = DialogueManager.masterDatabase;
        if (runtimeDatabase == null) return false;
        Conversation runtime = runtimeDatabase.GetConversation(title);
        if (HasLines(runtime)) return true;
        Conversation source = r8Database.GetConversation(title);
        if (!HasLines(source)) return false;
        if (runtime == null) DialogueManager.AddDatabase(r8Database);
        else
        {
            int index = runtimeDatabase.conversations.IndexOf(runtime);
            if (index < 0) return false;
            runtimeDatabase.conversations[index] = new Conversation(source);
            runtimeDatabase.ResetCache();
        }
        return HasLines(DialogueManager.masterDatabase.GetConversation(title));
    }

    private static bool HasLines(Conversation conversation)
    {
        return conversation != null && conversation.dialogueEntries != null &&
               conversation.dialogueEntries.Count > 1;
    }

    private void OnConversationEnded(Transform actor)
    {
        if (activeConversation == null || DialogueManager.lastConversationStarted != activeConversation) return;
        string ended = activeConversation;
        activeConversation = null;
        DialogueManager.instance.conversationEnded -= OnConversationEnded;
        if (ended == FamilyConversation)
        {
            familyFinished = true;
            drawingSymbols.SetActive(true);
            doctor.gameObject.SetActive(true);
            QueueConversation(DoctorConversation, player.transform, doctor);
        }
        else if (ended == DoctorConversation)
        {
            doctorFinished = true;
            StartCoroutine(FadeDoctorAndOpenExit());
        }
    }

    private IEnumerator FadeDoctorAndOpenExit()
    {
        float elapsed = 0f;
        const float duration = 0.8f;
        while (elapsed < duration)
        {
            SetDoctorAlpha(1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        SetDoctorAlpha(0f);
        doctor.gameObject.SetActive(false);
        exitUnlocked = true;
        exitPortalCollider.enabled = true;
        player.SetCanMove(true);
        DialogueManager.ShowAlert("可从南侧前往 R1。", 4f);
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
        if (DialogueManager.hasInstance) DialogueManager.instance.conversationEnded -= OnConversationEnded;
        activeConversation = null;
        pendingConversation = null;
        pendingActor = null;
        pendingConversant = null;
        entered = false;
    }
}
