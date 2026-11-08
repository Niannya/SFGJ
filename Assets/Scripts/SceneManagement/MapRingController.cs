using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField, Min(0.01f)] private float fadeSeconds = 1f;
    [SerializeField, Min(0.01f)] private float sceneEntryFadeSeconds = 1f;

    [Header("Minimap（九宫格，手动配置）")]
    [Tooltip("九宫格根节点（用于整体显示/隐藏）")]
    [SerializeField] private Transform minimapRoot;

    [Tooltip("9 个房间图的根 GameObject，索引 = 房间号（[0]=r0 诊疗室、[1]=r1 客厅、…、[8]=r8 卧室）")]
    [SerializeField] private GameObject[] minimapTiles;

    [Tooltip("九宫格在黑屏中显示的时长（秒）")]
    [SerializeField, Min(0.1f)] private float minimapDisplaySeconds = 6f;

    private int currentRoom;
    private bool transitioning;
    private bool firstCycleComplete;
    private bool endingUnlocked;
    private MapPortal[] portals;

    // 小地图
    private readonly HashSet<int> _visitedRooms = new HashSet<int>();

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

        currentRoom = Mathf.Clamp(startRoom, 0, rooms.Length - 1);
        _visitedRooms.Add(currentRoom);
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
        HideMinimap();

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
        player.SetCanMove(false);
        fadeOverlay.blocksRaycasts = true;

        // 1. 淡出到黑屏
        yield return Fade(1f, fadeSeconds);

        // 2. 换房间（在黑屏中完成）
        int origin = currentRoom;
        rooms[currentRoom].root.SetActive(false);
        currentRoom = portal.DestinationRoom;
        _visitedRooms.Add(currentRoom);
        rooms[currentRoom].root.SetActive(true);
        PlacePlayerAndCamera(currentRoom, portal.DestinationSpawn);
        if (!firstCycleComplete && origin == 8 && currentRoom == 1)
        {
            firstCycleComplete = true;
            RefreshPortals();
        }
        Physics2D.SyncTransforms();
        yield return null;

        // 3. 黑屏中显示九宫格（标记新房间与可到达房间）
        ShowMinimap();
        yield return new WaitForSecondsRealtime(minimapDisplaySeconds);
        HideMinimap();

        // 4. 淡入
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

    // ================= 九宫格 =================

    private void ShowMinimap()
    {
        if (minimapRoot == null) return;
        UpdateMinimapVisibility();
        minimapRoot.gameObject.SetActive(true);
    }

    private void HideMinimap()
    {
        if (minimapRoot != null) minimapRoot.gameObject.SetActive(false);
    }

    /// <summary>刷新九宫格显隐：当前房间与走过的房间显示，其余整个 GameObject 隐藏（含子图片）。索引即房间号。</summary>
    private void UpdateMinimapVisibility()
    {
        if (minimapTiles == null || minimapTiles.Length == 0) return;

        int roomCount = rooms == null ? 0 : rooms.Length;
        int n = Mathf.Min(9, Mathf.Min(roomCount, minimapTiles.Length));
        for (int r = 0; r < n; r++)
        {
            if (minimapTiles[r] == null) continue;
            bool visible = (r == currentRoom) || _visitedRooms.Contains(r);
            minimapTiles[r].SetActive(visible);
        }
    }
}
