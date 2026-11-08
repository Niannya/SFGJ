using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Runs the bedroom memory: collect three doll pieces and return them to the brother.</summary>
public sealed class R6FlowController : MonoBehaviour
{
    private const string MotherConversation = "R6_母亲_训斥";
    private const string DoctorFirstConversation = "R6_医生_第一次";
    private const string DoctorSecondConversation = "R6_医生_再次";

    [SerializeField] private MapRingController ring;
    [SerializeField] private DialogueDatabase r6Database;
    [SerializeField] private PlayerController player;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private Transform mother;
    [SerializeField] private Transform brother;
    [SerializeField] private Transform doctor;
    [SerializeField] private Transform[] dollPieces;
    [SerializeField] private GameObject restoredDoll;
    [SerializeField] private BoxCollider2D exitPortalCollider;

    private readonly bool[] collected = new bool[3];
    private bool initialized;
    private bool entered;
    private bool motherFinished;
    private bool repaired;
    private int doctorTalks;
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
        if (initialized) return;
        if (exitPortalCollider == null)
        {
            Transform portal = transform.Find("ForwardPortal_to_r7_East");
            if (portal != null) exitPortalCollider = portal.GetComponent<BoxCollider2D>();
        }
        if (ring == null || r6Database == null || player == null || mapFadeOverlay == null ||
            mother == null || brother == null || doctor == null || restoredDoll == null ||
            exitPortalCollider == null || dollPieces == null || dollPieces.Length != 3 ||
            dollPieces[0] == null || dollPieces[1] == null || dollPieces[2] == null)
        {
            Debug.LogError("R6FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        initialized = true;
    }

    private void PrepareRoomEntry()
    {
        entered = true;
        ApplyVisuals();
        if (!motherFinished) QueueConversation(MotherConversation, mother, player.transform);
    }

    private void ApplyVisuals()
    {
        mother.gameObject.SetActive(!repaired);
        doctor.gameObject.SetActive(repaired);
        restoredDoll.SetActive(repaired);
        for (int i = 0; i < dollPieces.Length; i++)
            dollPieces[i].gameObject.SetActive(!repaired && !collected[i]);
        exitPortalCollider.enabled = repaired;
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
        if (!initialized || ring.CurrentRoom != 6) return;
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
        if (activeConversation != null)
        {
            ContinueDialogueInput();
            return;
        }
        if (!motherFinished || !player.CanMove || !Input.GetKeyDown(KeyCode.Z)) return;

        if (!repaired)
        {
            for (int i = 0; i < dollPieces.Length; i++)
            {
                if (!collected[i] && Vector2.Distance(player.transform.position, dollPieces[i].position) <= 0.9f)
                {
                    collected[i] = true;
                    dollPieces[i].gameObject.SetActive(false);
                    DialogueManager.ShowAlert($"娃娃碎片 {CollectedCount}/3", 2f);
                    return;
                }
            }
            if (Vector2.Distance(player.transform.position, brother.position) <= 1.6f)
            {
                if (CollectedCount == 3)
                {
                    repaired = true;
                    ApplyVisuals();
                    DialogueManager.ShowAlert("娃娃拼好并交给了萌兰。", 3f);
                }
                else DialogueManager.ShowAlert($"还需寻找 {3 - CollectedCount} 片娃娃碎片。", 2f);
            }
        }
        else if (doctorTalks < 2 && Vector2.Distance(player.transform.position, doctor.position) <= 1.6f)
        {
            string title = doctorTalks == 0 ? DoctorFirstConversation : DoctorSecondConversation;
            StartStoryConversation(title, player.transform, doctor);
        }
    }

    private int CollectedCount => (collected[0] ? 1 : 0) + (collected[1] ? 1 : 0) + (collected[2] ? 1 : 0);

    private bool StartStoryConversation(string title, Transform actor, Transform conversant)
    {
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive || activeConversation != null) return false;
        if (!EnsureConversationAvailable(title))
        {
            if (!reportedMissingConversation)
            {
                reportedMissingConversation = true;
                Debug.LogError($"R6 conversation has no usable dialogue: {title}", this);
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
            Debug.LogError($"R6 conversation did not start: {title}", this);
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
        Conversation source = r6Database.GetConversation(title);
        if (!HasLines(source)) return false;
        if (runtime == null) DialogueManager.AddDatabase(r6Database);
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
        if (ended == MotherConversation)
        {
            motherFinished = true;
            DialogueManager.ShowAlert("按 Z 拾起地上的三片娃娃碎片。", 4f);
        }
        else if (ended == DoctorFirstConversation) doctorTalks = 1;
        else if (ended == DoctorSecondConversation) doctorTalks = 2;
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
        if (DialogueManager.hasInstance) DialogueManager.instance.conversationEnded -= OnConversationEnded;
        activeConversation = null;
        pendingConversation = null;
        pendingActor = null;
        pendingConversant = null;
        entered = false;
    }
}
