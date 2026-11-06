using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using SFGJ.Level;

namespace SFGJ.LevelEditor
{
    /// <summary>
    /// 关卡编辑器主窗口。直接在场景中创建 / 删除 / 编辑关卡元素对象，
    /// 关卡即场景本身（无需运行时再生成）。
    ///
    /// 操作方式：
    ///   - 左键点击空格：放置当前画笔元素（1x1）
    ///   - 按住左键拖拽：框选放置大物体（如大触发器）
    ///   - 左键点击已有元素：选中（在 Inspector 中编辑属性）
    ///   - Shift + 左键 或 右键：删除该格元素
    ///   - 选择"橡皮擦"画笔：左键点击直接删除
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        private const string WhiteSpritePath = "Assets/SFGJ/Textures/WhiteBlock.png";
        private const float SingleBlockGap = 0.06f;

        // ---------------- 菜单入口 ----------------
        [MenuItem("SFGJ/关卡编辑器", priority = 1)]
        public static void Open()
        {
            var win = GetWindow<LevelEditorWindow>("关卡编辑器");
            win.minSize = new Vector2(340, 520);
            win.Show();
        }

        // ---------------- 状态 ----------------
        public static LevelEditorWindow instance;

        private LevelSettings settings;

        /// <summary>当前画笔索引：0 = 橡皮擦，1..N 对应 BrushTypes。</summary>
        private int brushIndex = 1;

        private bool isDragging = false;
        private Vector2Int? dragStartGrid = null;

        private Vector2 scrollPos;
        private GUIStyle _labelStyle;

        private static readonly LevelElementType[] BrushTypes =
            (LevelElementType[])Enum.GetValues(typeof(LevelElementType));

        // ---------------- 生命周期 ----------------
        private void OnEnable()
        {
            instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
            EnsureSettings();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (instance == this) instance = null;
        }

        private void EnsureSettings()
        {
            if (settings == null)
                settings = FindObjectOfType<LevelSettings>();
            if (settings == null)
            {
                var go = new GameObject("LevelSettings");
                settings = go.AddComponent<LevelSettings>();
                Undo.RegisterCreatedObjectUndo(go, "创建关卡设置");
                Selection.activeGameObject = go;
            }
        }

        // ---------------- 窗口 UI ----------------
        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            // 关卡设置对象
            EditorGUILayout.LabelField("关卡设置", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings = (LevelSettings)EditorGUILayout.ObjectField("设置对象", settings, typeof(LevelSettings), true);
            if (EditorGUI.EndChangeCheck()) return;
            if (GUILayout.Button("在场景中创建/查找设置对象")) { settings = null; EnsureSettings(); }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("点击上方按钮在场景中创建关卡设置对象。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();
            settings.gridWidth = EditorGUILayout.IntField("宽度 (格)", settings.gridWidth);
            settings.gridHeight = EditorGUILayout.IntField("高度 (格)", settings.gridHeight);
            settings.cellSize = EditorGUILayout.FloatField("格子大小", settings.cellSize);
            if (EditorGUI.EndChangeCheck())
            {
                settings.gridWidth = Mathf.Max(1, settings.gridWidth);
                settings.gridHeight = Mathf.Max(1, settings.gridHeight);
                settings.cellSize = Mathf.Max(0.1f, settings.cellSize);
                EditorUtility.SetDirty(settings);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(6);

            // 画笔
            EditorGUILayout.LabelField("画笔（左键放置 · 拖拽框选 · Shift+左键/右键删除）", EditorStyles.boldLabel);
            DrawBrushBar();

            EditorGUILayout.Space(6);

            // 元素列表
            var all = new List<LevelElementBehaviour>(FindObjectsOfType<LevelElementBehaviour>());
            all.Sort((a, b) => (a.gridPos.y * 10000 + a.gridPos.x).CompareTo(b.gridPos.y * 10000 + b.gridPos.x));
            EditorGUILayout.LabelField($"场景元素（{all.Count}）", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(160));
            for (int i = 0; i < all.Count; i++)
            {
                var el = all[i];
                bool selected = (Selection.activeGameObject == el.gameObject);
                string sizeText = (el.gridSize != Vector2Int.one) ? $"  {el.gridSize.x}x{el.gridSize.y}" : "";
                string line = $"{LevelPalette.GetLabel(el.type)}  {el.gridPos}{sizeText}  {el.gameObject.name}";
                bool toggle = GUILayout.Toggle(selected, line, EditorStyles.label);
                if (toggle != selected)
                    Selection.activeGameObject = toggle ? el.gameObject : null;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("自动加一圈围墙")) AutoWalls();
                if (GUILayout.Button("清空所有元素")) ClearAll();
            }

            EditorGUILayout.HelpBox("在 Scene 视图（2D）中：左键点击放置 1x1 元素；按住左键拖拽框选放置大物体；Shift+左键或右键删除；点击已有元素选中后在 Inspector 里编辑。", MessageType.Info);
        }

        private void DrawBrushBar()
        {
            DrawBrushButton(0, "橡皮擦", Color.white);
            for (int i = 0; i < BrushTypes.Length; i++)
            {
                var t = BrushTypes[i];
                DrawBrushButton(i + 1, LevelPalette.GetLabel(t), LevelPalette.GetColor(t));
            }
        }

        private void DrawBrushButton(int index, string label, Color color)
        {
            bool active = (brushIndex == index);
            Color oldBg = GUI.backgroundColor;
            if (active) GUI.backgroundColor = new Color(0.28f, 0.58f, 1f);

            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = color;
            style.hover.textColor = color;

            if (GUILayout.Button(label, style, GUILayout.Height(24), GUILayout.MinWidth(54)))
                brushIndex = index;

            GUI.backgroundColor = oldBg;
        }

        // ---------------- 场景视图 ----------------
        private void OnSceneGUI(SceneView sceneView)
        {
            if (settings == null) return;
            if (Event.current == null) return;

            DrawGrid();
            DrawSelectionHighlight();
            DrawHoverAndInput(sceneView);
        }

        private void DrawGrid()
        {
            float cs = settings.cellSize;
            Vector2 o = settings.GridOrigin;
            float w = settings.gridWidth * cs;
            float h = settings.gridHeight * cs;

            Handles.color = new Color(0.55f, 0.55f, 0.6f, 0.45f);
            for (int x = 0; x <= settings.gridWidth; x++)
            {
                float px = o.x + x * cs;
                Handles.DrawLine(new Vector3(px, o.y, 0), new Vector3(px, o.y + h, 0));
            }
            for (int y = 0; y <= settings.gridHeight; y++)
            {
                float py = o.y + y * cs;
                Handles.DrawLine(new Vector3(o.x, py, 0), new Vector3(o.x + w, py, 0));
            }

            Handles.color = new Color(1f, 1f, 1f, 0.7f);
            Handles.DrawLine(new Vector3(o.x, o.y, 0), new Vector3(o.x + w, o.y, 0));
            Handles.DrawLine(new Vector3(o.x + w, o.y, 0), new Vector3(o.x + w, o.y + h, 0));
            Handles.DrawLine(new Vector3(o.x + w, o.y + h, 0), new Vector3(o.x, o.y + h, 0));
            Handles.DrawLine(new Vector3(o.x, o.y + h, 0), new Vector3(o.x, o.y, 0));
        }

        private void DrawSelectionHighlight()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var beh = go.GetComponent<LevelElementBehaviour>();
            if (beh == null) return;

            Rect wr = settings.ElementWorldRect(beh.gridPos, beh.gridSize);
            Vector3 c = settings.ElementCenter(beh.gridPos, beh.gridSize);
            Handles.color = Color.yellow;
            Handles.DrawWireCube(c, new Vector3(wr.width, wr.height, 0.01f));
        }

        private void DrawHoverAndInput(SceneView sceneView)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out float dist)) return;

            Vector3 world = ray.GetPoint(dist);
            Vector2Int grid = settings.WorldToGrid(world);
            bool inBounds = settings.InBounds(grid);

            // 接管鼠标控制（阻止 Scene 视图默认框选/选中）
            if (inBounds || isDragging)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // 预览绘制（Repaint 时）
            if (e.type == EventType.Repaint)
            {
                if (isDragging && dragStartGrid.HasValue)
                    DrawDragPreview(dragStartGrid.Value, grid);
                else if (inBounds)
                    DrawHoverHighlight(grid);
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (inBounds && e.button == 0 && !e.shift)
                    {
                        if (brushIndex == 0)
                        {
                            RemoveElementAt(grid);
                        }
                        else
                        {
                            isDragging = true;
                            dragStartGrid = grid;
                        }
                        e.Use();
                        Repaint();
                    }
                    else if (inBounds && (e.button == 1 || (e.button == 0 && e.shift)))
                    {
                        RemoveElementAt(grid);
                        e.Use();
                        Repaint();
                    }
                    break;

                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        e.Use();
                        Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (isDragging && e.button == 0 && dragStartGrid.HasValue)
                    {
                        PlaceRect(dragStartGrid.Value, grid);
                        isDragging = false;
                        dragStartGrid = null;
                        e.Use();
                        Repaint();
                    }
                    break;
            }
        }

        private void DrawDragPreview(Vector2Int a, Vector2Int b)
        {
            Vector2Int min = new Vector2Int(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            Vector2Int max = new Vector2Int(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            min.x = Mathf.Clamp(min.x, 0, settings.gridWidth - 1);
            min.y = Mathf.Clamp(min.y, 0, settings.gridHeight - 1);
            max.x = Mathf.Clamp(max.x, 0, settings.gridWidth - 1);
            max.y = Mathf.Clamp(max.y, 0, settings.gridHeight - 1);

            float cs = settings.cellSize;
            Vector2 o = settings.GridOrigin;
            Vector2 wMin = o + new Vector2(min.x * cs, min.y * cs);
            Vector2 wMax = o + new Vector2((max.x + 1) * cs, (max.y + 1) * cs);

            Vector3[] verts =
            {
                new Vector3(wMin.x, wMin.y, 0),
                new Vector3(wMax.x, wMin.y, 0),
                new Vector3(wMax.x, wMax.y, 0),
                new Vector3(wMin.x, wMax.y, 0),
            };
            Handles.DrawSolidRectangleWithOutline(verts, new Color(1f, 1f, 1f, 0.16f), Color.white);

            Vector2Int size = max - min + Vector2Int.one;
            Vector3 center = new Vector3((wMin.x + wMax.x) / 2f, (wMin.y + wMax.y) / 2f, 0);
            Handles.Label(center, $"{size.x} x {size.y}", GetLabelStyle());
        }

        private void DrawHoverHighlight(Vector2Int grid)
        {
            float cs = settings.cellSize;
            Vector3 c = settings.GridToWorld(grid);
            float half = cs * 0.48f;
            Vector3[] verts =
            {
                new Vector3(c.x - half, c.y - half, 0),
                new Vector3(c.x + half, c.y - half, 0),
                new Vector3(c.x + half, c.y + half, 0),
                new Vector3(c.x - half, c.y + half, 0),
            };
            Color hoverCol = (brushIndex == 0)
                ? new Color(1f, 0.3f, 0.3f, 0.35f)
                : new Color(1f, 1f, 1f, 0.22f);
            Handles.DrawSolidRectangleWithOutline(verts, hoverCol, Color.white);
        }

        private GUIStyle GetLabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.label);
                _labelStyle.alignment = TextAnchor.MiddleCenter;
                _labelStyle.normal.textColor = Color.white;
                _labelStyle.fontStyle = FontStyle.Bold;
                _labelStyle.fontSize = 11;
            }
            return _labelStyle;
        }

        // ---------------- 场景对象操作 ----------------
        private void PlaceRect(Vector2Int a, Vector2Int b)
        {
            Vector2Int min = new Vector2Int(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            Vector2Int max = new Vector2Int(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            min.x = Mathf.Clamp(min.x, 0, settings.gridWidth - 1);
            min.y = Mathf.Clamp(min.y, 0, settings.gridHeight - 1);
            max.x = Mathf.Clamp(max.x, 0, settings.gridWidth - 1);
            max.y = Mathf.Clamp(max.y, 0, settings.gridHeight - 1);
            Vector2Int size = max - min + Vector2Int.one;

            if (size == Vector2Int.one)
            {
                var existing = GetElementAt(min);
                if (existing != null)
                {
                    Selection.activeGameObject = existing.gameObject;
                    return;
                }
                PlaceElement(BrushTypes[brushIndex - 1], min, Vector2Int.one);
            }
            else
            {
                ClearRect(min, size);
                PlaceElement(BrushTypes[brushIndex - 1], min, size);
            }
        }

        private LevelElementBehaviour GetElementAt(Vector2Int grid)
        {
            var all = FindObjectsOfType<LevelElementBehaviour>();
            for (int i = all.Length - 1; i >= 0; i--)
            {
                if (settings.ElementRect(all[i]).Contains(grid)) return all[i];
            }
            return null;
        }

        private void ClearRect(Vector2Int min, Vector2Int size)
        {
            var rect = new RectInt(min.x, min.y, size.x, size.y);
            var toRemove = new List<GameObject>();
            foreach (var beh in FindObjectsOfType<LevelElementBehaviour>())
            {
                if (settings.ElementRect(beh).Overlaps(rect))
                    toRemove.Add(beh.gameObject);
            }
            foreach (var go in toRemove)
                Undo.DestroyObjectImmediate(go);
        }

        private void RemoveElementAt(Vector2Int grid)
        {
            var toRemove = new List<GameObject>();
            foreach (var beh in FindObjectsOfType<LevelElementBehaviour>())
            {
                if (settings.ElementRect(beh).Contains(grid))
                    toRemove.Add(beh.gameObject);
            }
            if (toRemove.Count == 0) return;
            foreach (var go in toRemove)
                Undo.DestroyObjectImmediate(go);
        }

        private void PlaceElement(LevelElementType type, Vector2Int pos, Vector2Int size)
        {
            var go = new GameObject(LevelPalette.GetLabel(type));
            go.transform.position = settings.ElementCenter(pos, size);

            var beh = go.AddComponent<LevelElementBehaviour>();
            beh.type = type;
            beh.gridPos = pos;
            beh.gridSize = size;

            // 视觉：白色占位块染色
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetWhiteSprite();
            sr.color = LevelPalette.GetColor(type);
            sr.sortingOrder = 0;

            // 只有单格物体留缝隙以显示格子边界；多格物体完整铺满
            bool multi = size.x > 1 || size.y > 1;
            float shrink = multi ? 1f : (1f - SingleBlockGap);
            go.transform.localScale = new Vector3(size.x * settings.cellSize * shrink, size.y * settings.cellSize * shrink, 1f);

            // 碰撞体：补偿视觉缩放，世界空间正好覆盖完整格子
            var col = go.AddComponent<BoxCollider2D>();
            Vector2 full = new Vector2(size.x, size.y) * settings.cellSize;
            Vector3 s = go.transform.localScale;
            col.size = new Vector2(full.x / s.x, full.y / s.y);
            col.isTrigger = !LevelPalette.IsSolid(type);

            go.transform.SetParent(GetOrCreateRoot(), true);

            Undo.RegisterCreatedObjectUndo(go, "放置元素");
            Selection.activeGameObject = go;
            EditorUtility.SetDirty(go);
        }

        private Transform GetOrCreateRoot()
        {
            var root = GameObject.Find("LevelElements");
            if (root != null) return root.transform;
            var go = new GameObject("LevelElements");
            Undo.RegisterCreatedObjectUndo(go, "创建关卡根节点");
            return go.transform;
        }

        private void AutoWalls()
        {
            int w = settings.gridWidth;
            int h = settings.gridHeight;
            for (int x = 0; x < w; x++)
            {
                TryAddWall(new Vector2Int(x, 0));
                TryAddWall(new Vector2Int(x, h - 1));
            }
            for (int y = 0; y < h; y++)
            {
                TryAddWall(new Vector2Int(0, y));
                TryAddWall(new Vector2Int(w - 1, y));
            }
        }

        private void TryAddWall(Vector2Int pos)
        {
            if (GetElementAt(pos) != null) return;
            PlaceElement(LevelElementType.Wall, pos, Vector2Int.one);
        }

        private void ClearAll()
        {
            var all = FindObjectsOfType<LevelElementBehaviour>();
            if (all.Length == 0) return;
            if (!EditorUtility.DisplayDialog("清空", $"确定删除场景中全部 {all.Length} 个关卡元素？", "清空", "取消"))
                return;
            foreach (var beh in all)
                Undo.DestroyObjectImmediate(beh.gameObject);
        }

        // ---------------- 白色占位 Sprite ----------------
        private static Sprite GetWhiteSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            if (sprite != null) return sprite;

            // 生成 4x4 白色 PNG 并导入为 Sprite（4px 对应 1 unit）
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();

            string dir = Path.GetDirectoryName(WhiteSpritePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(WhiteSpritePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(WhiteSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(WhiteSpritePath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 4f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        }
    }
}
