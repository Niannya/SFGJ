using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Runs the playground memory: return a child's balloon, then speak with the brother.</summary>
public sealed class R7FlowController : MonoBehaviour
{
    private static int playSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginPlaySession() => playSession++;

    private const string Intro = "R7_母亲_训斥";
    private const string Child = "R7_小孩_气球";
    private const string MotherReaction = "R7_母亲_气球";
    private const string DoctorHint = "R7_医生_提示";
    private const string DoctorBrother = "R7_医生_找哥哥";
    private const string Brother = "R7_哥哥_告别";
    private const string DoctorFinal = "R7_医生_结尾";

    [SerializeField] private MapRingController ring;
    [SerializeField] private DialogueDatabase r7Database;
    [SerializeField] private PlayerController player;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private Transform mother;
    [SerializeField] private Transform father;
    [SerializeField] private Transform child;
    [SerializeField] private Transform tree;
    [SerializeField] private Transform balloon;
    [SerializeField] private Transform brother;
    [SerializeField] private Transform doctor;
    [SerializeField] private BoxCollider2D exitPortalCollider;

    private SpriteRenderer motherRenderer;
    private SpriteRenderer fatherRenderer;
    private bool initialized;
    private int initializedSession = -1;
    private Vector3 balloonHomeLocalPosition;
    private bool entered;
    private bool introFinished;
    private bool childMet;
    private bool treeTouched;
    private bool parentsGone;
    private bool doctorBrotherHeard;
    private bool brotherTalked;
    private bool doctorFinished;
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
            Transform portal = transform.Find("ForwardPortal_to_r8_East");
            if (portal != null) exitPortalCollider = portal.GetComponent<BoxCollider2D>();
        }
        motherRenderer = mother == null ? null : mother.GetComponent<SpriteRenderer>();
        fatherRenderer = father == null ? null : father.GetComponent<SpriteRenderer>();
        if (ring == null || r7Database == null || player == null || mapFadeOverlay == null ||
            mother == null || father == null || child == null || tree == null || balloon == null ||
            brother == null || doctor == null || motherRenderer == null || fatherRenderer == null ||
            exitPortalCollider == null)
        {
            Debug.LogError("R7FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        if (!initialized) balloonHomeLocalPosition = balloon.localPosition;
        if (initializedSession != playSession)
        {
            introFinished = false;
            childMet = false;
            treeTouched = false;
            parentsGone = false;
            doctorBrotherHeard = false;
            brotherTalked = false;
            doctorFinished = false;
            reportedMissingConversation = false;
            activeConversation = null;
            pendingConversation = null;
            pendingActor = null;
            pendingConversant = null;
            entered = false;
            balloon.localPosition = balloonHomeLocalPosition;
        }
        initializedSession = playSession;
        initialized = true;
    }

    private void PrepareRoomEntry()
    {
        entered = true;
        ApplyVisuals();
        if (!introFinished) QueueConversation(Intro, mother, player.transform);
    }

    private void ApplyVisuals()
    {
        mother.gameObject.SetActive(!parentsGone);
        father.gameObject.SetActive(!parentsGone);
        SetAlpha(motherRenderer, parentsGone ? 0f : 1f);
        SetAlpha(fatherRenderer, parentsGone ? 0f : 1f);
        balloon.gameObject.SetActive(!treeTouched);
        exitPortalCollider.enabled = doctorFinished;
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
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
        if (!initialized || ring.CurrentRoom != 7) return;
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
        if (!introFinished || !player.CanMove || !Input.GetKeyDown(KeyCode.Z)) return;

        Vector2 at = player.transform.position;
        if (Vector2.Distance(at, doctor.position) <= 1.6f)
        {
            if (!parentsGone) StartStoryConversation(DoctorHint, player.transform, doctor);
            else if (!brotherTalked) StartStoryConversation(DoctorBrother, player.transform, doctor);
            else if (!doctorFinished) StartStoryConversation(DoctorFinal, player.transform, doctor);
            return;
        }
        if (!childMet && Vector2.Distance(at, child.position) <= 1.5f)
        {
            StartStoryConversation(Child, child, player.transform);
            return;
        }
        if (!treeTouched && Vector2.Distance(at, tree.position) <= 1.8f)
        {
            if (childMet)
            {
                treeTouched = true;
                player.SetCanMove(false);
                StartCoroutine(BalloonToMother());
            }
            else DialogueManager.ShowAlert("先问问哭泣的小孩。", 2f);
            return;
        }
        if (Vector2.Distance(at, brother.position) <= 1.6f && !brotherTalked)
        {
            if (!parentsGone) DialogueManager.ShowAlert("先帮小孩找回气球。", 2f);
            else if (!doctorBrotherHeard) DialogueManager.ShowAlert("先听听医生要说什么。", 2f);
            else StartStoryConversation(Brother, brother, player.transform);
        }
    }

    private IEnumerator BalloonToMother()
    {
        GameAudioDirector.PlayBalloon();
        Vector3 from = balloon.position;
        Vector3 to = mother.position + Vector3.up * 1.2f;
        float elapsed = 0f;
        const float duration = 0.75f;
        while (elapsed < duration)
        {
            balloon.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        balloon.position = to;
        balloon.gameObject.SetActive(false);
        if (isActiveAndEnabled && ring.CurrentRoom == 7)
            QueueConversation(MotherReaction, mother, player.transform);
    }

    private IEnumerator FadeParents()
    {
        float elapsed = 0f;
        const float duration = 0.8f;
        while (elapsed < duration)
        {
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            SetAlpha(motherRenderer, alpha);
            SetAlpha(fatherRenderer, alpha);
            yield return null;
            elapsed += Time.deltaTime;
        }
        parentsGone = true;
        mother.gameObject.SetActive(false);
        father.gameObject.SetActive(false);
        DialogueManager.ShowAlert("医生似乎有话要说。", 3f);
        player.SetCanMove(true);
    }

    private bool StartStoryConversation(string title, Transform actor, Transform conversant)
    {
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive || activeConversation != null) return false;
        if (!EnsureConversationAvailable(title))
        {
            if (!reportedMissingConversation)
            {
                reportedMissingConversation = true;
                Debug.LogError($"R7 conversation has no usable dialogue: {title}", this);
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
            Debug.LogError($"R7 conversation did not start: {title}", this);
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
        Conversation source = r7Database.GetConversation(title);
        if (!HasLines(source)) return false;
        if (runtime == null) DialogueManager.AddDatabase(r7Database);
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
        if (ended == Intro)
        {
            introFinished = true;
            DialogueManager.ShowAlert("按 Z 与哭泣的小孩交谈。", 4f);
        }
        else if (ended == Child)
        {
            childMet = true;
            DialogueManager.ShowAlert("气球挂在树上；靠近树按 Z。", 4f);
        }
        else if (ended == MotherReaction)
        {
            StartCoroutine(FadeParents());
            return;
        }
        else if (ended == DoctorBrother)
        {
            doctorBrotherHeard = true;
            DialogueManager.ShowAlert("去找哥哥说话。", 3f);
        }
        else if (ended == Brother)
        {
            brotherTalked = true;
            DialogueManager.ShowAlert("再和医生说话。", 3f);
        }
        else if (ended == DoctorFinal)
        {
            doctorFinished = true;
            exitPortalCollider.enabled = true;
            DialogueManager.ShowAlert("可以前往下一个房间了。", 3f);
        }
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
