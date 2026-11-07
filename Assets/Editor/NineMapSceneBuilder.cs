using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-time builder. SampleScene is copied before any object is changed.</summary>
[InitializeOnLoad]
public static class NineMapSceneBuilder
{
    private const string SourceScene = "Assets/Scenes/SampleScene.unity";
    private const string TargetScene = "Assets/Scenes/NineMapScene.unity";
    private const string SpritePath = "Assets/Scenes/NineMapPlaceholder.png";
    private const string PreviousSceneSha256 = "6EB8B025709C0FA029B814090545F36072EBDF20424D7AE38C947F10F3516109";

    static NineMapSceneBuilder()
    {
        EditorApplication.delayCall += TryUpgradePreviousScene;
    }

    private static void TryUpgradePreviousScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryUpgradePreviousScene;
            return;
        }
        Debug.Log("NineMapTopologyUpgrade check: cwd=" + Directory.GetCurrentDirectory() +
            " exists=" + File.Exists(TargetScene) + " dirty=" + EditorSceneManager.GetActiveScene().isDirty);
        if (!File.Exists(TargetScene) || EditorSceneManager.GetActiveScene().isDirty) return;
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(TargetScene))
        {
            string current = System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            Debug.Log("NineMapTopologyUpgrade hash=" + current);
            if (current != PreviousSceneSha256) return;
        }
        UpdateTopology();
    }

    private static readonly string[] Names =
    {
        "诊疗室", "客厅", "教室", "游乐场", "教室", "客厅", "卧室", "游乐场", "卧室"
    };

    private static readonly Vector2[] Grid =
    {
        new Vector2(0, 0), new Vector2(22, 0), new Vector2(22, -14),
        new Vector2(0, -14), new Vector2(-22, -14), new Vector2(-22, 0),
        new Vector2(-22, 14), new Vector2(0, 14), new Vector2(22, 14)
    };

    [MenuItem("Tools/SFGJ/Build Nine Map Scene")]
    public static void Build()
    {
        if (!File.Exists(SourceScene))
        {
            Debug.LogError("Missing source scene: " + SourceScene);
            return;
        }
        if (File.Exists(TargetScene) && File.ReadAllText(TargetScene) != File.ReadAllText(SourceScene))
        {
            EditorUtility.DisplayDialog("Nine Map Scene", "NineMapScene already contains work. No existing work was overwritten.", "OK");
            return;
        }

        Sprite sprite = EnsurePlaceholderSprite();
        Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, TargetScene))
        {
            Debug.LogError("Could not copy SampleScene to " + TargetScene);
            return;
        }

        RemoveRoot("Test");
        RemoveRoot("DialogueTest");

        PlayerController player = Object.FindObjectOfType<PlayerController>();
        Camera mapCamera = Camera.main;
        if (player == null || mapCamera == null || player.GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogError("Copied scene must contain the existing Player, Rigidbody2D and Main Camera.");
            return;
        }

        PlayerSpawner oldSpawner = player.GetComponent<PlayerSpawner>();
        if (oldSpawner != null) oldSpawner.enabled = false;
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = 5.1f;
        mapCamera.transform.position = new Vector3(0, 0, mapCamera.transform.position.z);

        GameObject manager = new GameObject("MapRingController");
        MapRingController ring = manager.AddComponent<MapRingController>();
        GameObject mapContainer = new GameObject("NineMaps_3x3");
        GameObject[] roots = new GameObject[9];
        Transform[] spawns = new Transform[9];

        for (int i = 0; i < 9; i++)
        {
            GameObject root = new GameObject("r" + i + "_" + Names[i]);
            root.transform.SetParent(mapContainer.transform);
            root.transform.position = Grid[i];
            roots[i] = root;

            Color floorColor = RoomColor(i);
            AddSprite(root.transform, "PlaceholderImage_ReplaceWithArt", sprite, new Vector2(18, 10), floorColor, -10, Vector2.zero);
            GameObject spawn = new GameObject("Spawn_r" + i);
            spawn.transform.SetParent(root.transform);
            spawn.transform.localPosition = new Vector3(0, -1, 0);
            spawns[i] = spawn.transform;

            AddTitle(root.transform, "r" + i + "  " + Names[i]);
        }
        InstallTopology(roots, sprite, ring);

        CanvasGroup fade = AddFadeCanvas();
        SerializedObject serialized = new SerializedObject(ring);
        SerializedProperty rooms = serialized.FindProperty("rooms");
        rooms.arraySize = 9;
        for (int i = 0; i < 9; i++)
        {
            SerializedProperty room = rooms.GetArrayElementAtIndex(i);
            room.FindPropertyRelative("id").stringValue = "r" + i;
            room.FindPropertyRelative("root").objectReferenceValue = roots[i];
            room.FindPropertyRelative("spawn").objectReferenceValue = spawns[i];
        }
        serialized.FindProperty("startRoom").intValue = 0;
        serialized.FindProperty("player").objectReferenceValue = player;
        serialized.FindProperty("playerBody").objectReferenceValue = player.GetComponent<Rigidbody2D>();
        serialized.FindProperty("mapCamera").objectReferenceValue = mapCamera;
        serialized.FindProperty("fadeOverlay").objectReferenceValue = fade;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        player.transform.position = spawns[0].position;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Built NineMapScene: r0-r8 one-way first cycle, r8-r1 unlocks return doors, r1-r0 reserved for ending.");
    }

    [MenuItem("Tools/SFGJ/Update Nine Map Topology")]
    public static void UpdateTopology()
    {
        if (!File.Exists(TargetScene))
        {
            Debug.LogError("Missing scene: " + TargetScene);
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        GameObject container = GameObject.Find("NineMaps_3x3");
        MapRingController ring = Object.FindObjectOfType<MapRingController>();
        if (container == null || ring == null)
        {
            Debug.LogError("NineMapScene is missing NineMaps_3x3 or MapRingController.");
            return;
        }

        GameObject[] roots = new GameObject[9];
        for (int i = 0; i < 9; i++)
        {
            Transform room = container.transform.Find("r" + i + "_" + Names[i]);
            if (room == null)
            {
                Debug.LogError("Missing room r" + i + "; no topology was changed.");
                return;
            }
            roots[i] = room.gameObject;
        }

        Sprite sprite = EnsurePlaceholderSprite();
        InstallTopology(roots, sprite, ring);
        GameObject fadeCanvas = GameObject.Find("MapFadeCanvas");
        if (fadeCanvas != null && fadeCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            fadeCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Updated NineMapScene topology: first pass one-way, r8 to r1 unlocks return doors; ending door r1 to r0 remains locked until UnlockEndingDoor().");
    }

    private static Sprite EnsurePlaceholderSprite()
    {
        if (!File.Exists(SpritePath))
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath);
        }
        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null) throw new IOException("Cannot import placeholder sprite: " + SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static void RemoveRoot(string name)
    {
        GameObject found = GameObject.Find(name);
        if (found != null && found.transform.parent == null) Object.DestroyImmediate(found);
    }

    private static Color RoomColor(int index)
    {
        if (index == 0) return new Color(.27f, .45f, .51f, 1);
        if (index == 1 || index == 5) return new Color(.53f, .42f, .30f, 1);
        if (index == 2 || index == 4) return new Color(.39f, .34f, .52f, 1);
        if (index == 3 || index == 7) return new Color(.34f, .49f, .37f, 1);
        return new Color(.30f, .40f, .60f, 1);
    }

    private static GameObject AddSprite(Transform parent, string name, Sprite sprite, Vector2 size, Color color, int order, Vector2 localPosition)
    {
        GameObject result = new GameObject(name);
        result.transform.SetParent(parent);
        result.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0);
        result.transform.localScale = new Vector3(size.x, size.y, 1);
        SpriteRenderer renderer = result.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return result;
    }

    private static void AddWall(Transform parent, Sprite sprite, string name, Vector2 localPosition, Vector2 size)
    {
        GameObject wall = AddSprite(parent, name, sprite, size, new Color(.10f, .15f, .22f, 1f), -5, localPosition);
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
    }

    private static void AddTitle(Transform parent, string title)
    {
        GameObject label = new GameObject("RoomLabel");
        label.transform.SetParent(parent);
        label.transform.localPosition = new Vector3(0, 3.35f, -1f);
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = title;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 64;
        text.characterSize = .08f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = Color.white;
        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 5;
    }

    private static void InstallTopology(GameObject[] roots, Sprite sprite, MapRingController ring)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            Transform room = roots[i].transform;
            for (int childIndex = room.childCount - 1; childIndex >= 0; childIndex--)
            {
                GameObject child = room.GetChild(childIndex).gameObject;
                string name = child.name;
                if (name.StartsWith("Wall_") || name.StartsWith("NextPortal_") ||
                    name.StartsWith("PreviousPortal_") || name.StartsWith("ForwardPortal_") ||
                    name.StartsWith("BackPortal_") || name.StartsWith("EndingPortal_") ||
                    name.StartsWith("EntryFrom_"))
                    Object.DestroyImmediate(child);
            }
            AddWall(room, sprite, "Wall_North", new Vector2(0, 4.9f), new Vector2(18, .2f));
            AddWall(room, sprite, "Wall_South", new Vector2(0, -4.9f), new Vector2(18, .2f));
            AddWall(room, sprite, "Wall_West", new Vector2(-8.9f, 0), new Vector2(.2f, 10));
            AddWall(room, sprite, "Wall_East", new Vector2(8.9f, 0), new Vector2(.2f, 10));
        }

        // r0 -> r1 -> ... -> r8 -> r1 is the first, irreversible pass.
        for (int i = 0; i < 8; i++)
            AddConnection(roots, sprite, ring, i, i + 1, MapPortal.AccessRule.Forward);
        AddConnection(roots, sprite, ring, 8, 1, MapPortal.AccessRule.Forward);

        // These doors appear only after arriving at r1 from r8.
        for (int i = 1; i < 8; i++)
            AddConnection(roots, sprite, ring, i + 1, i, MapPortal.AccessRule.AfterFirstCycle);
        AddConnection(roots, sprite, ring, 1, 8, MapPortal.AccessRule.AfterFirstCycle);

        // No other room has a door to r0. This one is a story-controlled ending door.
        AddConnection(roots, sprite, ring, 1, 0, MapPortal.AccessRule.EndingOnly);
    }

    private static void AddConnection(GameObject[] roots, Sprite sprite, MapRingController ring,
        int origin, int destination, MapPortal.AccessRule access)
    {
        Vector2 delta = Grid[destination] - Grid[origin];
        Vector2 direction;
        if (Mathf.Abs(delta.x) > .01f && Mathf.Abs(delta.y) < .01f)
            direction = new Vector2(Mathf.Sign(delta.x), 0);
        else if (Mathf.Abs(delta.y) > .01f && Mathf.Abs(delta.x) < .01f)
            direction = new Vector2(0, Mathf.Sign(delta.y));
        else
            throw new System.InvalidOperationException("Rooms must share a row or column: r" + origin + " -> r" + destination);

        Vector2 portalPosition = new Vector2(direction.x * 8.3f, direction.y * 4.3f);
        Vector2 portalSize = direction.x == 0 ? new Vector2(1.7f, .8f) : new Vector2(.8f, 1.7f);
        Vector2 entryPosition = new Vector2(-direction.x * 6.7f, -direction.y * 2.8f);
        string prefix = access == MapPortal.AccessRule.Forward ? "ForwardPortal_" :
                        access == MapPortal.AccessRule.AfterFirstCycle ? "BackPortal_" : "EndingPortal_";
        string side = direction.x > 0 ? "East" : direction.x < 0 ? "West" : direction.y > 0 ? "North" : "South";
        Color color = access == MapPortal.AccessRule.Forward ? new Color(.20f, .85f, .85f, 1f) :
                      access == MapPortal.AccessRule.AfterFirstCycle ? new Color(1f, .70f, .24f, 1f) :
                      new Color(.95f, .35f, .72f, 1f);

        GameObject entry = new GameObject("EntryFrom_r" + origin + "_to_r" + destination);
        entry.transform.SetParent(roots[destination].transform);
        entry.transform.localPosition = new Vector3(entryPosition.x, entryPosition.y, 0);

        GameObject portal = AddSprite(roots[origin].transform,
            prefix + "to_r" + destination + "_" + side, sprite, portalSize, color, -3, portalPosition);
        BoxCollider2D collider = portal.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        MapPortal script = portal.AddComponent<MapPortal>();
        SerializedObject serialized = new SerializedObject(script);
        serialized.FindProperty("ring").objectReferenceValue = ring;
        serialized.FindProperty("originRoom").intValue = origin;
        serialized.FindProperty("destinationRoom").intValue = destination;
        serialized.FindProperty("destinationSpawn").objectReferenceValue = entry.transform;
        serialized.FindProperty("accessRule").enumValueIndex = (int)access;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CanvasGroup AddFadeCanvas()
    {
        GameObject canvasObject = new GameObject("MapFadeCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject overlay = new GameObject("BlackFadeOverlay", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
        overlay.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        UnityEngine.UI.Image image = overlay.GetComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.raycastTarget = true;
        CanvasGroup group = overlay.GetComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        return group;
    }
}
