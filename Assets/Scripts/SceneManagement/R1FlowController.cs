using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Stages the first living-room visit and the changed response after R8.</summary>
public sealed class R1FlowController : MonoBehaviour
{
    private const string FirstIntro = "R1_初次_开场";
    private const string EndingConversation = "R1_结束";
    private const string EndingSceneName = "EndingScene";
    private const string FatherFamily = "R1_父亲与妹妹";
    private const string MotherFamily = "R1_母亲与哥哥";
    private const string DoctorIntro = "R1_医生_初次";
    private const string DoctorAnswerOne = "R1_医生_精神世界";
    private const string DoctorAnswerTwo = "R1_医生_心灵投影";
    private const string DoctorFollowUp = "R1_医生_继续探索";

    private static int playSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginPlaySession() => playSession++;

    [SerializeField] private MapRingController ring;
    [SerializeField] private DialogueDatabase r1Database;
    [SerializeField] private PlayerController player;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private Transform father;
    [SerializeField] private Transform sister;
    [SerializeField] private Transform mother;
    [SerializeField] private Transform brother;
    [SerializeField] private Transform doctor;
    [SerializeField] private BoxCollider2D exitPortalCollider;

    private bool initialized;
    private int initializedSession = -1;
    private bool entered;
    private bool firstIntroSeen;
    private bool endingTransitioning;
    private bool fatherSeen;
    private bool motherSeen;
    private bool doctorIntroSeen;
    private bool answerOneSeen;
    private bool answerTwoSeen;
    private bool storyComplete;
    private bool waitingChoice;
    private string pendingConversation;
    private Transform pendingActor;
    private Transform pendingConversant;
    private string activeConversation;
    private int conversationStartFrame;
    private bool reportedMissingConversation;

    private void Awake() => Initialize();
    private void OnEnable()
    {
        Initialize();
        // Never carry the first visit's unlocked exit into the return from R8.
        if (exitPortalCollider != null) exitPortalCollider.enabled = false;
    }

    private void Initialize()
    {
        if (initialized && initializedSession == playSession) return;
        if (exitPortalCollider == null)
        {
            Transform portal = transform.Find("ForwardPortal_to_r2_South");
            if (portal != null) exitPortalCollider = portal.GetComponent<BoxCollider2D>();
        }
        if (ring == null || r1Database == null || player == null || mapFadeOverlay == null ||
            father == null || sister == null || mother == null || brother == null || doctor == null ||
            exitPortalCollider == null)
        {
            Debug.LogError("R1FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        if (initializedSession != playSession)
        {
            entered = false;
            firstIntroSeen = false;
            endingTransitioning = false;
            fatherSeen = false;
            motherSeen = false;
            doctorIntroSeen = false;
            answerOneSeen = false;
            answerTwoSeen = false;
            storyComplete = false;
            waitingChoice = false;
            activeConversation = null;
            pendingConversation = null;
            pendingActor = null;
            pendingConversant = null;
            reportedMissingConversation = false;
        }
        initializedSession = playSession;
        initialized = true;
    }

    private void PrepareRoomEntry()
    {
        entered = true;
        exitPortalCollider.enabled = !ring.FirstCycleComplete && storyComplete;
        Debug.Log($"R1 room entered: secondCycle={ring.FirstCycleComplete}, storyComplete={storyComplete}", this);
        if (!ring.FirstCycleComplete && !firstIntroSeen)
            QueueConversation(FirstIntro, mother, player.transform);
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
        if (!initialized || ring.CurrentRoom != 1) return;
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
        if (ring.FirstCycleComplete)
        {
            if (!endingTransitioning && player.CanMove && Input.GetKeyDown(KeyCode.Z) &&
                Vector2.Distance(player.transform.position, doctor.position) <= 1.7f)
                StartStoryConversation(EndingConversation, player.transform, doctor);
            return;
        }
        if (waitingChoice)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                if (StartStoryConversation(DoctorAnswerOne, player.transform, doctor)) waitingChoice = false;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                if (StartStoryConversation(DoctorAnswerTwo, player.transform, doctor)) waitingChoice = false;
            }
            else if (Input.GetKeyDown(KeyCode.Z)) ShowChoicePrompt();
            return;
        }
        if (!player.CanMove || !Input.GetKeyDown(KeyCode.Z)) return;
        Vector2 at = player.transform.position;
        if (Vector2.Distance(at, father.position) <= 1.7f ||
            Vector2.Distance(at, sister.position) <= 1.7f)
        {
            if (!fatherSeen) StartStoryConversation(FatherFamily, father, sister);
            return;
        }
        if (Vector2.Distance(at, mother.position) <= 1.7f ||
            Vector2.Distance(at, brother.position) <= 1.7f)
        {
            if (!motherSeen) StartStoryConversation(MotherFamily, mother, brother);
            return;
        }
        if (Vector2.Distance(at, doctor.position) <= 1.7f && !storyComplete)
        {
            if (!doctorIntroSeen) StartStoryConversation(DoctorIntro, player.transform, doctor);
            else if (!answerOneSeen || !answerTwoSeen)
            {
                waitingChoice = true;
                player.SetCanMove(false);
                ShowChoicePrompt();
            }
            else if (fatherSeen && motherSeen)
                StartStoryConversation(DoctorFollowUp, player.transform, doctor);
            else DialogueManager.ShowAlert("先听听家人的对话。", 3f);
        }
    }

    private void ShowChoicePrompt()
    {
        DialogueManager.ShowAlert("按 1：这里是哪里？  按 2：现在是什么情况？", 20f);
    }

    private bool StartStoryConversation(string title, Transform actor, Transform conversant)
    {
        if (!DialogueManager.hasInstance || DialogueManager.isConversationActive || activeConversation != null) return false;
        if (!EnsureConversationAvailable(title))
        {
            if (!reportedMissingConversation)
            {
                reportedMissingConversation = true;
                Debug.LogError($"R1 conversation has no usable dialogue: {title}", this);
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
            Debug.LogError($"R1 conversation did not start: {title}", this);
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
        Conversation source = r1Database.GetConversation(title);
        if (!HasLines(source)) return false;
        if (runtime == null) DialogueManager.AddDatabase(r1Database);
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
        if (ended == FirstIntro)
        {
            firstIntroSeen = true;
            DialogueManager.ShowAlert("靠近家人或医生，按 Z 交谈。", 4f);
        }
        else if (ended == EndingConversation)
        {
            endingTransitioning = true;
            StartCoroutine(FadeToEndingScene());
            return;
        }
        else if (ended == FatherFamily) fatherSeen = true;
        else if (ended == MotherFamily) motherSeen = true;
        else if (ended == DoctorIntro)
        {
            doctorIntroSeen = true;
            waitingChoice = true;
            ShowChoicePrompt();
            return;
        }
        else if (ended == DoctorAnswerOne)
        {
            answerOneSeen = true;
            if (!answerTwoSeen) DialogueManager.ShowAlert("再次与医生交谈，按 2 听另一种解释。", 5f);
        }
        else if (ended == DoctorAnswerTwo)
        {
            answerTwoSeen = true;
            if (!answerOneSeen) DialogueManager.ShowAlert("再次与医生交谈，按 1 听另一种解释。", 5f);
        }
        else if (ended == DoctorFollowUp)
        {
            storyComplete = true;
            exitPortalCollider.enabled = true;
            DialogueManager.ShowAlert("可从南侧前往 R2。", 4f);
        }
        player.SetCanMove(true);
    }

    private System.Collections.IEnumerator FadeToEndingScene()
    {
        player.SetCanMove(false);
        DialogueManager.HideAllAlerts();
        mapFadeOverlay.blocksRaycasts = true;
        float startAlpha = mapFadeOverlay.alpha;
        float elapsed = 0f;
        const float duration = 0.4f;
        while (elapsed < duration)
        {
            mapFadeOverlay.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        mapFadeOverlay.alpha = 1f;
        SceneManager.LoadScene(EndingSceneName);
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
        waitingChoice = false;
        entered = false;
    }
}
