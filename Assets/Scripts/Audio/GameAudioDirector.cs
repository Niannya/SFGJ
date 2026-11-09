using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Plays scene music and shared UI/gameplay sounds without scene-specific AudioSources.</summary>
public sealed class GameAudioDirector : MonoBehaviour
{
    private const float BaseMusicVolume = 0.18f;
    private const float RoomMusicVolume = 0.55f;
    private const float CrowdVolume = 0.16f;
    private const float FootstepInterval = 0.48f;

    private static GameAudioDirector instance;
    private GameAudioCatalog clips;
    private AudioSource baseMusic;
    private AudioSource roomMusic;
    private AudioSource crowd;
    private AudioSource effects;
    private AudioSource footsteps;
    private MapRingController ring;
    private R4FlowController r4;
    private PlayerController player;
    private int configuredSceneHandle = -1;
    private int activeRoom = -1;
    private Vector3 lastPlayerPosition;
    private float footstepTimer;
    private Coroutine roomFade;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateDirector()
    {
        if (instance != null) return;
        var audioObject = new GameObject("GameAudioDirector");
        DontDestroyOnLoad(audioObject);
        audioObject.AddComponent<GameAudioDirector>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureAlreadyLoadedScene()
    {
        if (instance != null) instance.EnsureActiveScene();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        clips = Resources.Load<GameAudioCatalog>("GameAudioCatalog");
        if (clips == null)
            Debug.LogError("GameAudioCatalog is missing from Assets/Resources.", this);

        baseMusic = AddSource(true, BaseMusicVolume);
        roomMusic = AddSource(true, 0f);
        crowd = AddSource(true, CrowdVolume);
        effects = AddSource(false, 1f);
        footsteps = AddSource(false, 0.28f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private AudioSource AddSource(bool loop, float volume)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        configuredSceneHandle = scene.handle;
        ring = null;
        r4 = null;
        player = null;
        activeRoom = -1;
        footstepTimer = 0f;

        if (scene.name == "NineMapScene")
        {
            ring = FindObjectOfType<MapRingController>();
            r4 = FindObjectOfType<R4FlowController>(true);
            player = FindObjectOfType<PlayerController>();
            if (player != null) lastPlayerPosition = player.transform.position;
            SetBaseMusic(clips == null ? null : clips.innerWorld);
            if (ring != null) SetRoom(ring.CurrentRoom);
        }
        else
        {
            if (roomFade != null) StopCoroutine(roomFade);
            roomMusic.Stop();
            crowd.Stop();
            AudioClip sceneClip = null;
            if (clips != null)
            {
                if (scene.name == "OpeningScene" || scene.name == "EndingScene")
                    sceneClip = clips.prologueEpilogue;
                else if (scene.name == "start" || scene.name == "StartScene" || scene.name == "MainMenuScene")
                    sceneClip = clips.mainMenu;
            }
            SetBaseMusic(sceneClip);
        }
        Debug.Log("GameAudioDirector: configured " + scene.name +
                  ", base=" + (baseMusic.clip == null ? "none" : baseMusic.clip.name) +
                  ", room=" + activeRoom, this);
    }

    private void EnsureActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded && scene.handle != configuredSceneHandle)
            OnSceneLoaded(scene, LoadSceneMode.Single);
    }

    private void SetBaseMusic(AudioClip clip)
    {
        if (baseMusic.clip == clip && baseMusic.isPlaying) return;
        baseMusic.Stop();
        baseMusic.clip = clip;
        if (clip != null) baseMusic.Play();
    }

    private void SetRoom(int index)
    {
        if (activeRoom == index) return;
        activeRoom = index;
        if (roomFade != null) StopCoroutine(roomFade);
        roomFade = StartCoroutine(CrossfadeRoom(clips == null ? null : clips.RoomClip(index)));

        bool inCrowd = index == 3 || index == 7;
        if (inCrowd && clips != null && clips.crowd != null)
        {
            if (!crowd.isPlaying)
            {
                crowd.clip = clips.crowd;
                crowd.Play();
            }
        }
        else crowd.Stop();
    }

    private IEnumerator CrossfadeRoom(AudioClip next)
    {
        float oldVolume = roomMusic.volume;
        for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            roomMusic.volume = Mathf.Lerp(oldVolume, 0f, t / 0.2f);
            yield return null;
        }
        roomMusic.Stop();
        roomMusic.clip = next;
        roomMusic.volume = 0f;
        if (next != null) roomMusic.Play();
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            roomMusic.volume = Mathf.Lerp(0f, RoomMusicVolume, t / 0.25f);
            yield return null;
        }
        roomMusic.volume = RoomMusicVolume;
        roomFade = null;
    }

    private void Update()
    {
        // With Enter Play Mode Options > Reload Scene disabled, sceneLoaded is not fired.
        EnsureActiveScene();
        if (ring != null && ring.CurrentRoom != activeRoom) SetRoom(ring.CurrentRoom);
        UpdateFootsteps();
        UpdateUiClick();
    }

    private void UpdateFootsteps()
    {
        if (player == null || clips == null || clips.footstep == null) return;
        Vector3 position = player.transform.position;
        // The supplied footstep effect is marked for the R4 maze, not every room.
        if (r4 == null || !r4.IsMazeActive)
        {
            lastPlayerPosition = position;
            footstepTimer = 0f;
            return;
        }
        bool walking = player.CanMove && player.MoveInput.sqrMagnitude > 0.01f &&
                       (position - lastPlayerPosition).sqrMagnitude > 0.00001f;
        lastPlayerPosition = position;
        if (!walking)
        {
            footstepTimer = 0f;
            return;
        }
        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f || footsteps.isPlaying) return;
        footsteps.clip = clips.footstep;
        footsteps.Play();
        footstepTimer = Mathf.Max(FootstepInterval, clips.footstep.length);
    }

    private void UpdateUiClick()
    {
        if (!Input.GetMouseButtonDown(0) || EventSystem.current == null || clips == null || clips.uiClick == null)
            return;
        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        if (uiHits.Count == 0) return;
        var button = uiHits[0].gameObject.GetComponentInParent<Button>();
        if (button != null && button.isActiveAndEnabled && button.interactable)
            effects.PlayOneShot(clips.uiClick, 0.5f);
    }

    public static void PlayDoor()
    {
        if (instance != null && instance.clips != null && instance.clips.whoosh != null)
            instance.effects.PlayOneShot(instance.clips.whoosh, 0.7f);
    }

    public static void PlayBalloon()
    {
        if (instance != null && instance.clips != null && instance.clips.balloon != null)
            instance.effects.PlayOneShot(instance.clips.balloon, 0.75f);
    }
}
