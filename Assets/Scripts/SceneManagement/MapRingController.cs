using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>Controls the nine rooms in one Unity scene. Room roots remain visible in Edit Mode.</summary>
public sealed class MapRingController : MonoBehaviour
{
    [System.Serializable]
    public sealed class Room
    {
        public string id;
        public GameObject root;
        public Transform spawn;
    }

    [SerializeField] private Room[] rooms;
    [SerializeField] private int startRoom;
    [SerializeField] private PlayerController player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Camera mapCamera;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.4f;
    [SerializeField, Min(0.01f)] private float sceneEntryFadeSeconds = 1f;

    private int currentRoom;
    private bool transitioning;
    private bool firstCycleComplete;
    private bool endingUnlocked;
    private MapPortal[] portals;

    public int CurrentRoom => currentRoom;
    public int RoomCount => rooms == null ? 0 : rooms.Length;
    public bool FirstCycleComplete => firstCycleComplete;
    public bool EndingUnlocked => endingUnlocked;

    private void Start()
    {
        if (rooms == null || rooms.Length == 0 || player == null || playerBody == null || mapCamera == null || fadeOverlay == null)
        {
            Debug.LogError("MapRingController has an unassigned scene reference.", this);
            enabled = false;
            return;
        }

        // A zero-scaled screen-space canvas makes the fade alpha animate invisibly.
        Canvas fadeCanvas = fadeOverlay.GetComponentInParent<Canvas>();
        if (fadeCanvas != null) fadeCanvas.transform.localScale = Vector3.one;

        currentRoom = Mathf.Clamp(startRoom, 0, rooms.Length - 1);
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].root == null || rooms[i].spawn == null)
            {
                Debug.LogError("MapRingController room " + i + " is incomplete.", this);
                enabled = false;
                return;
            }
            rooms[i].root.SetActive(i == currentRoom);
        }
        // Portals live under the room roots, not under this manager.
        portals = FindObjectsOfType<MapPortal>(true);
        RefreshPortals();

        PlacePlayerAndCamera(currentRoom);
        fadeOverlay.alpha = 1f;
        fadeOverlay.blocksRaycasts = true;
        StartCoroutine(InitialReveal());
    }

    private IEnumerator InitialReveal()
    {
        // Render the new scene fully black before starting its longer fade-in.
        yield return null;
        yield return Fade(0f, sceneEntryFadeSeconds);
        fadeOverlay.blocksRaycasts = false;
    }

    /// <summary>Called by the story once the ending condition has been met.</summary>
    public void UnlockEndingDoor()
    {
        if (!firstCycleComplete)
        {
            Debug.LogWarning("The ending door stays closed until r8 returns to r1.", this);
            return;
        }
        endingUnlocked = true;
        RefreshPortals();
    }

    public void EnterRoom(MapPortal portal)
    {
        if (!enabled || transitioning || portal == null || !portal.isActiveAndEnabled ||
            portal.OriginRoom != currentRoom || portal.DestinationRoom < 0 || portal.DestinationRoom >= RoomCount ||
            portal.DestinationSpawn == null || !portal.IsAvailable(firstCycleComplete, endingUnlocked)) return;
        StartCoroutine(Transition(portal));
    }

    private void RefreshPortals()
    {
        foreach (MapPortal portal in portals)
            if (portal != null) portal.gameObject.SetActive(portal.IsAvailable(firstCycleComplete, endingUnlocked));
    }

    private IEnumerator Transition(MapPortal portal)
    {
        transitioning = true;
        GameAudioDirector.PlayDoor();
        // The dialogue UI is shared by all room roots, so old hints outlive a room unless cleared.
        if (DialogueManager.hasInstance) DialogueManager.HideAllAlerts();
        player.SetCanMove(false);
        fadeOverlay.blocksRaycasts = true;
        yield return Fade(1f, fadeSeconds);

        int origin = currentRoom;
        rooms[currentRoom].root.SetActive(false);
        if (DialogueManager.hasInstance) DialogueManager.HideAllAlerts();
        currentRoom = portal.DestinationRoom;
        rooms[currentRoom].root.SetActive(true);
        PlacePlayerAndCamera(currentRoom, portal.DestinationSpawn);
        if (!firstCycleComplete && origin == 8 && currentRoom == 1)
        {
            firstCycleComplete = true;
            RefreshPortals();
        }
        Physics2D.SyncTransforms();
        yield return null;

        yield return Fade(0f, fadeSeconds);
        fadeOverlay.blocksRaycasts = false;
        player.SetCanMove(true);
        transitioning = false;
    }

    private void PlacePlayerAndCamera(int index, Transform entry = null)
    {
        Vector3 spawn = entry != null ? entry.position : rooms[index].spawn.position;
        playerBody.velocity = Vector2.zero;
        playerBody.position = new Vector2(spawn.x, spawn.y);
        player.transform.position = spawn;
        Vector3 center = rooms[index].root.transform.position;
        mapCamera.transform.position = new Vector3(center.x, center.y, mapCamera.transform.position.z);
    }

    private IEnumerator Fade(float target, float duration)
    {
        float initial = fadeOverlay.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            fadeOverlay.alpha = Mathf.Lerp(initial, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        fadeOverlay.alpha = target;
    }
}
