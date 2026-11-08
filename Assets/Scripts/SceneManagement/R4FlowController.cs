using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Runs the brother's classroom memory, maze search, reunion, and doctor epilogue.</summary>
public sealed class R4FlowController : MonoBehaviour
{
    private const string Accusation = "R4_萌兰_被围";
    private const string MazePlea = "R4_萌兰_迷宫";
    private const string FoundItem = "R4_萌兰_找回";
    private const string Handoff = "R4_萌兰_交还";
    private const string DoctorFirst = "R4_医生_第一次";
    private const string DoctorSecond = "R4_医生_再次";

    private enum Stage
    {
        ClassroomAccusation,
        MazePlea,
        MazeExplore,
        MazeReturn,
        ClassroomHandoff,
        DoctorFirst,
        DoctorSecond,
        Complete
    }

    [SerializeField] private MapRingController ring;
    [SerializeField] private DialogueDatabase r4Database;
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private GameObject classroomBackground;
    [SerializeField] private GameObject roomLabel;
    [SerializeField] private GameObject classroomSet;
    [SerializeField] private GameObject facelessCrowd;
    [SerializeField] private Transform classroomBrother;
    [SerializeField] private Transform yingYing;
    [SerializeField] private Transform doctor;
    [SerializeField] private GameObject mazeSet;
    [SerializeField] private Transform mazeBrother;
    [SerializeField] private Transform lostItem;
    [SerializeField] private SpriteRenderer blackoutRenderer;
    [SerializeField] private BoxCollider2D exitPortalCollider;
    [SerializeField] private BoxCollider2D backPortalCollider;

    private Stage stage;
    private bool initialized;
    private bool entered;
    private string activeConversation;
    private string pendingConversation;
    private Transform pendingActor;
    private Transform pendingConversant;
    private float pendingSince;
    private bool pendingWarningLogged;
    private bool missingConversationLogged;
    private int conversationStartFrame;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // With Scene Reload disabled, an existing scene object can enter Play Mode without Awake.
        Initialize();
        if (initialized) EnsureBlackoutVisual();
        if (Application.isPlaying)
            Debug.Log($"R4 controller enabled: initialized={initialized}, room={(ring == null ? -1 : ring.CurrentRoom)}", this);
    }

    private void Initialize()
    {
        if (initialized) return;
        if (exitPortalCollider == null)
        {
            Transform exit = transform.Find("ForwardPortal_to_r5_North");
            if (exit != null) exitPortalCollider = exit.GetComponent<BoxCollider2D>();
        }
        if (ring == null || r4Database == null || player == null || playerBody == null || mapFadeOverlay == null ||
            classroomBackground == null || roomLabel == null || classroomSet == null || facelessCrowd == null ||
            classroomBrother == null || yingYing == null || doctor == null || mazeSet == null ||
            mazeBrother == null || lostItem == null || blackoutRenderer == null ||
            exitPortalCollider == null || backPortalCollider == null)
        {
            Debug.LogError("R4FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        EnsureBlackoutVisual();
        SetBlackAlpha(0f);
        initialized = true;
    }

    private void EnsureBlackoutVisual()
    {
        if (blackoutRenderer.sprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            blackoutRenderer.sprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }
        blackoutRenderer.transform.localScale = new Vector3(100f, 100f, 1f);
        Color color = blackoutRenderer.color;
        color.r = color.g = color.b = 0f;
        blackoutRenderer.color = color;
    }

    private void PrepareRoomEntry()
    {
        entered = true;
        Debug.Log($"R4 room entered: stage={stage}", this);
        ApplyStageVisuals();
        if (stage == Stage.ClassroomAccusation)
            QueueConversation(Accusation, classroomBrother, player.transform);
        else if (stage == Stage.MazePlea)
            QueueConversation(MazePlea, mazeBrother, player.transform);
        else if (stage == Stage.ClassroomHandoff)
            QueueConversation(Handoff, classroomBrother, yingYing);
        else player.SetCanMove(true);
    }

    private void QueueConversation(string title, Transform actor, Transform conversant)
    {
        pendingConversation = title;
        pendingActor = actor;
        pendingConversant = conversant;
        pendingSince = Time.unscaledTime;
        pendingWarningLogged = false;
        missingConversationLogged = false;
        player.SetCanMove(false);
    }

    private void Update()
    {
        if (!initialized) Initialize();
        if (!initialized || ring.CurrentRoom != 4) return;
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
            else if (!pendingWarningLogged && Time.unscaledTime - pendingSince > 3f)
            {
                pendingWarningLogged = true;
                Debug.LogWarning($"R4 conversation waiting: room={ring.CurrentRoom}, fade={mapFadeOverlay.blocksRaycasts}, dialogueManager={DialogueManager.hasInstance}, conversationActive={(DialogueManager.hasInstance && DialogueManager.isConversationActive)}", this);
            }
            return;
        }
        if (activeConversation != null)
        {
            ContinueDialogueInput();
            return;
        }

        if (stage == Stage.MazeExplore &&
            Vector2.Distance(player.transform.position, lostItem.position) <= 0.9f)
        {
            stage = Stage.MazeReturn;
            lostItem.gameObject.SetActive(false);
            return;
        }
        if (!player.CanMove || !Input.GetKeyDown(KeyCode.Z)) return;
        if (stage == Stage.MazeReturn &&
            Vector2.Distance(player.transform.position, mazeBrother.position) <= 1.5f)
            StartStoryConversation(FoundItem, player.transform, mazeBrother);
        else if (stage == Stage.DoctorFirst &&
            Vector2.Distance(player.transform.position, doctor.position) <= 1.6f)
            StartStoryConversation(DoctorFirst, player.transform, doctor);
        else if (stage == Stage.DoctorSecond &&
            Vector2.Distance(player.transform.position, doctor.position) <= 1.6f)
            StartStoryConversation(DoctorSecond, player.transform, doctor);
    }

    private bool StartStoryConversation(string title, Transform actor, Transform conversant)
    {
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive || activeConversation != null)
            return false;
        if (!EnsureConversationAvailable(title))
        {
            if (!missingConversationLogged)
            {
                missingConversationLogged = true;
                var source = r4Database.GetConversation(title);
                Debug.LogError($"R4 conversation has no usable dialogue: {title}. Source nodes={source?.dialogueEntries?.Count ?? 0}", this);
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
            if (!missingConversationLogged)
            {
                missingConversationLogged = true;
                Debug.LogError($"R4 conversation did not start: {title}", this);
            }
            return false;
        }
        Debug.Log($"R4 conversation started: {title}", this);
        return true;
    }

    private bool EnsureConversationAvailable(string title)
    {
        DialogueDatabase runtimeDatabase = DialogueManager.masterDatabase;
        if (runtimeDatabase == null) return false;
        Conversation runtime = runtimeDatabase.GetConversation(title);
        if (HasDialogueLines(runtime)) return true;
        Conversation source = r4Database.GetConversation(title);
        if (!HasDialogueLines(source)) return false;
        if (runtime == null)
        {
            DialogueManager.AddDatabase(r4Database);
        }
        else
        {
            int index = runtimeDatabase.conversations.IndexOf(runtime);
            if (index < 0) return false;
            runtimeDatabase.conversations[index] = new Conversation(source);
            runtimeDatabase.ResetCache();
        }
        bool available = HasDialogueLines(DialogueManager.masterDatabase.GetConversation(title));
        if (available) Debug.Log($"R4 dialogue restored in runtime database: {title}", this);
        return available;
    }

    private static bool HasDialogueLines(Conversation conversation)
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
        if (ended == Accusation) StartCoroutine(EnterMaze());
        else if (ended == MazePlea)
        {
            stage = Stage.MazeExplore;
            lostItem.gameObject.SetActive(true);
            player.SetCanMove(true);
        }
        else if (ended == FoundItem) StartCoroutine(ReturnToClassroom());
        else if (ended == Handoff)
        {
            stage = Stage.DoctorFirst;
            ApplyStageVisuals();
            player.SetCanMove(true);
        }
        else if (ended == DoctorFirst)
        {
            stage = Stage.DoctorSecond;
            player.SetCanMove(true);
        }
        else if (ended == DoctorSecond)
        {
            stage = Stage.Complete;
            exitPortalCollider.enabled = true;
            player.SetCanMove(true);
        }
    }

    private IEnumerator EnterMaze()
    {
        yield return FadeBlack(1f, 0.75f);
        stage = Stage.MazePlea;
        ApplyStageVisuals();
        PlacePlayer(new Vector2(-6.4f, -3.2f));
        yield return FadeBlack(0f, 0.75f);
        QueueConversation(MazePlea, mazeBrother, player.transform);
    }

    private IEnumerator ReturnToClassroom()
    {
        yield return FadeBlack(1f, 0.75f);
        stage = Stage.ClassroomHandoff;
        ApplyStageVisuals();
        PlacePlayer(new Vector2(-1.8f, -2.1f));
        yield return FadeBlack(0f, 0.75f);
        QueueConversation(Handoff, classroomBrother, yingYing);
    }

    private void ApplyStageVisuals()
    {
        bool maze = stage == Stage.MazePlea || stage == Stage.MazeExplore || stage == Stage.MazeReturn;
        classroomBackground.SetActive(!maze);
        roomLabel.SetActive(!maze);
        classroomSet.SetActive(!maze);
        mazeSet.SetActive(maze);
        facelessCrowd.SetActive(stage == Stage.ClassroomAccusation);
        doctor.gameObject.SetActive(stage == Stage.DoctorFirst || stage == Stage.DoctorSecond ||
                                    stage == Stage.Complete);
        lostItem.gameObject.SetActive(stage == Stage.MazeExplore);
        exitPortalCollider.enabled = stage == Stage.Complete;
        backPortalCollider.enabled = stage == Stage.Complete;
    }

    private void PlacePlayer(Vector2 localPosition)
    {
        Vector3 world = transform.TransformPoint(localPosition);
        playerBody.velocity = Vector2.zero;
        playerBody.position = world;
        player.transform.position = world;
        Physics2D.SyncTransforms();
    }

    private IEnumerator FadeBlack(float target, float duration)
    {
        float from = blackoutRenderer.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetBlackAlpha(Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / duration)));
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        SetBlackAlpha(target);
    }

    private void SetBlackAlpha(float alpha)
    {
        Color color = blackoutRenderer.color;
        color.a = alpha;
        blackoutRenderer.color = color;
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
        pendingConversation = null;
        pendingActor = null;
        pendingConversant = null;
        if (!entered) return;
        SetBlackAlpha(0f);
        entered = false;
    }
}
