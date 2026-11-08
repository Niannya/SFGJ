using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Two classroom story beats and the doll gate, inside the existing map room r2.</summary>
public sealed class R2FlowController : MonoBehaviour
{
    private const string FirstConversation = "R2_友情_初见";
    private const string DollConversation = "R2_友情_送娃娃";
    private const string DoctorWithDoll = "R2_医生_有娃娃";
    private const string DoctorWithoutDoll = "R2_医生_未有娃娃";

    [SerializeField] private MapRingController ring;
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Camera mapCamera;
    [SerializeField] private CanvasGroup mapFadeOverlay;
    [SerializeField] private Transform xiaoYe;
    [SerializeField] private Transform yingYing;
    [SerializeField] private Transform doctor;
    [SerializeField] private BoxCollider2D exitPortalCollider;

    private RigidbodyConstraints2D originalConstraints;
    private bool initialized;
    private bool firstComplete;
    private bool dollReceived;
    private bool doctorWithoutDollComplete;
    private bool doctorWithDollComplete;
    private string activeConversation;
    private int conversationStartFrame;

    public bool DollReceived => dollReceived;

    private void Awake()
    {
        if (exitPortalCollider == null)
        {
            Transform exit = transform.Find("ForwardPortal_to_r3_West");
            if (exit != null) exitPortalCollider = exit.GetComponent<BoxCollider2D>();
        }
        if (ring == null || player == null || playerBody == null || mapCamera == null ||
            mapFadeOverlay == null || xiaoYe == null || yingYing == null || doctor == null || exitPortalCollider == null)
        {
            Debug.LogError("R2FlowController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }
        originalConstraints = playerBody.constraints;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized) return;
        PrepareRoom();
        StartCoroutine(BeginAfterRoomEntry());
    }

    private void PrepareRoom()
    {
        // The topology builder may recreate these children. Keep its MapPortal wiring,
        // but present this long room's forward door at the right end.
        Transform exit = transform.Find("ForwardPortal_to_r3_West");
        if (exit != null)
        {
            exitPortalCollider = exit.GetComponent<BoxCollider2D>();
            exit.localPosition = new Vector3(26.3f, -3.2f, 0f);
            if (exitPortalCollider != null) exitPortalCollider.enabled = dollReceived;
        }
        Transform returnDoor = transform.Find("BackPortal_to_r1_North");
        if (returnDoor != null) returnDoor.localPosition = new Vector3(-8.2f, -3.2f, 0f);
        Transform eastWall = transform.Find("Wall_East");
        if (eastWall != null)
        {
            BoxCollider2D wallCollider = eastWall.GetComponent<BoxCollider2D>();
            if (wallCollider != null) wallCollider.enabled = false;
            SpriteRenderer wallRenderer = eastWall.GetComponent<SpriteRenderer>();
            if (wallRenderer != null) wallRenderer.enabled = false;
        }
    }

    private IEnumerator BeginAfterRoomEntry()
    {
        // MapRingController places the player after activating this room.
        yield return null;
        if (!isActiveAndEnabled || ring.CurrentRoom != 2) yield break;
        Vector3 local = transform.InverseTransformPoint(player.transform.position);
        if (!firstComplete) local.x = -6f;
        local.y = -3.2f;
        Vector3 world = transform.TransformPoint(local);
        playerBody.velocity = Vector2.zero;
        playerBody.position = world;
        player.transform.position = world;
        playerBody.constraints = originalConstraints | RigidbodyConstraints2D.FreezePositionY;
        Physics2D.SyncTransforms();

        while (isActiveAndEnabled && mapFadeOverlay.blocksRaycasts) yield return null;
        while (isActiveAndEnabled && !DialogueManager.hasInstance) yield return null;
        if (isActiveAndEnabled && !firstComplete)
            StartStoryConversation(FirstConversation, xiaoYe, yingYing);
    }

    private void Update()
    {
        if (ring == null || ring.CurrentRoom != 2) return;
        if (activeConversation != null)
        {
            ContinueDialogueInput();
            return;
        }
        if (!firstComplete || !player.CanMove || !Input.GetKeyDown(KeyCode.Z)) return;
        float x = transform.InverseTransformPoint(player.transform.position).x;
        if (!dollReceived && Mathf.Abs(x - 14.5f) <= 2f)
            StartStoryConversation(DollConversation, xiaoYe, yingYing);
        else if (Mathf.Abs(x - 23.5f) <= 2f)
        {
            if (dollReceived && !doctorWithDollComplete)
                StartStoryConversation(DoctorWithDoll, player.transform, doctor);
            else if (!dollReceived && !doctorWithoutDollComplete)
                StartStoryConversation(DoctorWithoutDoll, player.transform, doctor);
        }
    }

    private void LateUpdate()
    {
        if (ring == null || ring.CurrentRoom != 2 || mapCamera == null) return;
        float x = transform.InverseTransformPoint(player.transform.position).x;
        Vector3 center = transform.position;
        mapCamera.transform.position = new Vector3(center.x + Mathf.Clamp(x, 0f, 18f), center.y,
            mapCamera.transform.position.z);
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
        if (ended == FirstConversation) firstComplete = true;
        else if (ended == DollConversation)
        {
            dollReceived = true;
            if (exitPortalCollider != null) exitPortalCollider.enabled = true;
        }
        else if (ended == DoctorWithDoll) doctorWithDollComplete = true;
        else if (ended == DoctorWithoutDoll) doctorWithoutDollComplete = true;
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
        if (initialized && playerBody != null) playerBody.constraints = originalConstraints;
        activeConversation = null;
    }
}
