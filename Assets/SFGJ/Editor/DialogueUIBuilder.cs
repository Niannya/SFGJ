using System.IO;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SFGJ.EditorTools
{
    /// <summary>
    /// 基于 Dialogue System 的 Basic Standard Dialogue UI 生成项目专用界面。
    /// 保留插件面板、按钮与打字机逻辑，只重做版式、配色和头像区域。
    /// </summary>
    public static class DialogueUIBuilder
    {
        public const string CustomDialogueUIPath = "Assets/SFGJ/UI/SFGJ Dialogue UI.prefab";

        private const string TemplatePath =
            "Assets/Plugins/Pixel Crushers/Dialogue System/Prefabs/Standard UI Prefabs/Templates/Basic/Basic Standard Dialogue UI.prefab";
        private const string TextureDirectory = "Assets/SFGJ/UI/Textures";

        private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.085f, 0.96f);
        private static readonly Color TextColor = new Color(0.88f, 0.91f, 0.92f, 1f);
        private static readonly Color MutedTextColor = new Color(0.48f, 0.59f, 0.64f, 1f);
        private static readonly Color TealColor = new Color(0.38f, 0.72f, 0.72f, 1f);
        private static readonly Color AmberColor = new Color(0.86f, 0.66f, 0.43f, 1f);

        [MenuItem("SFGJ/对话 UI/生成并应用剧情对话 UI")]
        public static void GenerateAndApply()
        {
            var prefab = EnsureCustomDialogueUI();
            if (prefab == null) return;

            var controller = Object.FindObjectOfType<DialogueSystemController>();
            if (controller != null)
            {
                ApplyToScene(controller, prefab);
            }
            else
            {
                Debug.Log("[对话 UI] UI 已生成。执行 SFGJ/集成对话系统 后会自动应用到场景。");
            }

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        /// <summary>已生成时直接复用，避免覆盖用户后续在 prefab 中做的手动调整。</summary>
        public static GameObject EnsureCustomDialogueUI()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CustomDialogueUIPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/SFGJ/UI");
            EnsureFolder(TextureDirectory);
            if (!File.Exists(TemplatePath))
            {
                EditorUtility.DisplayDialog("找不到 UI 模板",
                    "找不到 Basic Standard Dialogue UI.prefab，请确认 Dialogue System 插件路径未被移动。", "OK");
                return null;
            }

            if (!AssetDatabase.CopyAsset(TemplatePath, CustomDialogueUIPath))
            {
                Debug.LogError("[对话 UI] 无法复制基础 UI 模板到 " + CustomDialogueUIPath);
                return null;
            }

            var root = PrefabUtility.LoadPrefabContents(CustomDialogueUIPath);
            try
            {
                root.name = "SFGJ Dialogue UI";
                ConfigureRoot(root);
                var ui = root.GetComponent<StandardDialogueUI>();
                if (ui == null)
                {
                    Debug.LogError("[对话 UI] 基础模板缺少 StandardDialogueUI 组件。");
                    return null;
                }

                foreach (var panel in ui.conversationUIElements.subtitlePanels)
                {
                    if (panel != null) ConfigureSubtitlePanel(panel, panel == ui.conversationUIElements.defaultPCSubtitlePanel);
                }

                foreach (var menu in ui.conversationUIElements.menuPanels)
                {
                    if (menu != null) ConfigureResponseMenu(menu);
                }

                ConfigureAlert(ui);
                PrefabUtility.SaveAsPrefabAsset(root, CustomDialogueUIPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[对话 UI] 已生成带人物头像框的剧情 UI：" + CustomDialogueUIPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(CustomDialogueUIPath);
        }

        public static void ApplyToScene(DialogueSystemController controller, GameObject uiPrefab)
        {
            if (controller == null || uiPrefab == null) return;

            // 优先使用场景内实例，让用户能直接在 Hierarchy 中调整实际 UI。
            var sceneUI = controller.GetComponentInChildren<StandardDialogueUI>(true);
            if (sceneUI == null)
            {
                var canvas = controller.GetComponentInChildren<Canvas>(true);
                var instance = PrefabUtility.InstantiatePrefab(uiPrefab) as GameObject;
                if (instance != null)
                {
                    instance.transform.SetParent(canvas != null ? canvas.transform : controller.transform, false);
                    sceneUI = instance.GetComponent<StandardDialogueUI>();
                }
            }

            controller.displaySettings.dialogueUI = sceneUI != null ? sceneUI.gameObject : uiPrefab;
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log("[对话 UI] 已将 Dialogue Manager 的 Dialogue UI 切换为场景内的项目剧情界面。");
        }

        private static void ConfigureRoot(GameObject root)
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = false;
            }

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            var mainPanel = root.transform.Find("Dialogue Panel");
            if (mainPanel != null)
            {
                Stretch(mainPanel as RectTransform, Vector4.zero);
                var image = mainPanel.GetComponent<Image>();
                if (image != null)
                {
                    image.color = Color.clear;
                    image.sprite = null;
                    image.raycastTarget = false;
                }
            }
        }

        private static void ConfigureSubtitlePanel(StandardUISubtitlePanel panel, bool isPlayer)
        {
            var rect = panel.panel != null ? panel.panel : panel.GetComponent<RectTransform>();
            if (rect == null) return;

            SetBottomPanel(rect, 252f, 42f, 80f);
            StylePanelImage(rect.GetComponent<Image>(), false);

            var portrait = FindChild(rect, "Portrait Image")?.GetComponent<Image>();
            var nameText = FindChild(rect, "Portrait Name")?.GetComponent<Text>();
            var subtitle = FindChild(rect, "Subtitle Text")?.GetComponent<Text>();
            var continueButton = FindChild(rect, "Continue Button")?.GetComponent<Button>();

            CreatePortraitArea(rect, portrait, isPlayer, true);
            panel.portraitImage = portrait;
            panel.usePortraitNativeSize = false;

            if (nameText != null)
            {
                SetTextArea(nameText.rectTransform, isPlayer, true);
                StyleText(nameText, 30, isPlayer ? AmberColor : TealColor,
                    isPlayer ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, FontStyle.Bold);
                nameText.text = isPlayer ? "我" : "???";
                panel.portraitName = new UITextField(nameText);
            }

            if (subtitle != null)
            {
                SetTextArea(subtitle.rectTransform, isPlayer, false);
                StyleText(subtitle, 28, TextColor,
                    isPlayer ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
                subtitle.lineSpacing = 1.25f;
                subtitle.text = isPlayer ? "（玩家台词）" : "（角色台词）";
                panel.subtitleText = new UITextField(subtitle);
                panel.accumulateText = false;
                panel.addSpeakerName = false;
            }

            if (continueButton != null)
            {
                SetContinueButtonRect(continueButton, isPlayer);
                StyleButton(continueButton, false);
                var label = continueButton.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = "继续  ›";
                    StyleText(label, 20, TextColor, TextAnchor.MiddleCenter);
                }
                panel.continueButton = continueButton;
            }

            CreateAccent(rect, isPlayer);
        }

        private static void ConfigureResponseMenu(StandardUIMenuPanel menu)
        {
            var graphic = menu.panel != null ? menu.panel : menu.GetComponent<Graphic>();
            if (graphic == null) return;
            var rect = graphic.rectTransform;
            SetBottomPanel(rect, 330f, 36f, 130f);
            StylePanelImage(graphic as Image, false);

            var portrait = FindChild(rect, "Portrait Image")?.GetComponent<Image>();
            CreatePortraitArea(rect, portrait, false, false);
            menu.pcImage = portrait;
            menu.usePortraitNativeSize = false;

            var nameText = FindChild(rect, "Portrait Name")?.GetComponent<Text>();
            if (nameText != null)
            {
                var nameRect = nameText.rectTransform;
                nameRect.anchorMin = new Vector2(0f, 0f);
                nameRect.anchorMax = new Vector2(0f, 0f);
                nameRect.pivot = new Vector2(0f, 0f);
                nameRect.anchoredPosition = new Vector2(30f, 42f);
                nameRect.sizeDelta = new Vector2(148f, 36f);
                StyleText(nameText, 23, TealColor, TextAnchor.MiddleCenter, FontStyle.Bold);
                nameText.text = "我";
                menu.pcName = new UITextField(nameText);
            }

            if (menu.buttonTemplateHolder != null)
            {
                var scrollRect = FindParentByName(menu.buttonTemplateHolder.transform, "Scroll Rect") as RectTransform;
                if (scrollRect != null)
                {
                    scrollRect.anchorMin = Vector2.zero;
                    scrollRect.anchorMax = Vector2.one;
                    scrollRect.offsetMin = new Vector2(220f, 28f);
                    scrollRect.offsetMax = new Vector2(-28f, -28f);
                }
            }

            var template = menu.buttonTemplate;
            if (template != null)
            {
                var button = template.button != null ? template.button : template.GetComponent<Button>();
                if (button != null)
                {
                    StyleButton(button, true);
                    var buttonRect = button.GetComponent<RectTransform>();
                    buttonRect.sizeDelta = new Vector2(620f, 56f);
                }

                var label = template.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    Stretch(label.rectTransform, new Vector4(24f, 6f, 24f, 6f));
                    StyleText(label, 25, TextColor, TextAnchor.MiddleLeft);
                }
                template.defaultColor = TextColor;
                template.setLabelColor = true;
            }
        }

        private static void ConfigureAlert(StandardDialogueUI ui)
        {
            var panel = ui.alertUIElements.panel;
            if (panel == null) return;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -72f);
            rect.sizeDelta = new Vector2(860f, 112f);
            StylePanelImage(rect.GetComponent<Image>(), false);

            var alertText = ui.alertUIElements.alertText.uiText;
            if (alertText != null)
            {
                Stretch(alertText.rectTransform, new Vector4(34f, 20f, 140f, 18f));
                StyleText(alertText, 27, TextColor, TextAnchor.MiddleLeft);
            }

            var continueButton = FindChild(rect, "Continue Button")?.GetComponent<Button>();
            if (continueButton != null)
            {
                var buttonRect = continueButton.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(1f, 0f);
                buttonRect.anchorMax = new Vector2(1f, 0f);
                buttonRect.pivot = new Vector2(1f, 0f);
                buttonRect.anchoredPosition = new Vector2(-22f, 18f);
                buttonRect.sizeDelta = new Vector2(104f, 34f);
                StyleButton(continueButton, false);
                var label = continueButton.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = "继续  ›";
                    StyleText(label, 19, TextColor, TextAnchor.MiddleCenter);
                }
            }
        }

        private static void CreatePortraitArea(RectTransform panel, Image portrait, bool mirror, bool subtitleArea)
        {
            if (portrait == null) return;
            var width = subtitleArea ? 152f : 148f;
            var height = subtitleArea ? 176f : 144f;
            var x = subtitleArea ? 32f : 30f;
            var y = subtitleArea ? 48f : 94f;

            var area = new GameObject("Portrait Area", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(panel, false);
            area.anchorMin = new Vector2(mirror ? 1f : 0f, 0f);
            area.anchorMax = area.anchorMin;
            area.pivot = new Vector2(mirror ? 1f : 0f, 0f);
            area.anchoredPosition = new Vector2(mirror ? -x : x, y);
            area.sizeDelta = new Vector2(width, height);

            var echo = CreateImage("Identity Echo", area, GetFrameSprite());
            echo.color = new Color(AmberColor.r, AmberColor.g, AmberColor.b, 0.42f);
            Stretch(echo.rectTransform, new Vector4(-5f, -4f, 4f, 5f));
            echo.type = Image.Type.Sliced;
            echo.fillCenter = false;

            var placeholder = CreateImage("Portrait Placeholder", area, GetPlaceholderSprite());
            placeholder.color = Color.white;
            placeholder.preserveAspect = true;
            Stretch(placeholder.rectTransform, new Vector4(5f, 5f, 5f, 5f));

            portrait.transform.SetParent(area, false);
            portrait.transform.localScale = Vector3.one;
            Stretch(portrait.rectTransform, new Vector4(6f, 6f, 6f, 6f));
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.color = Color.white;
            portrait.sprite = null;

            var secondEcho = CreateImage("Memory Echo", area, GetFrameSprite());
            secondEcho.color = new Color(TealColor.r, TealColor.g, TealColor.b, 0.48f);
            Stretch(secondEcho.rectTransform, new Vector4(4f, 5f, -5f, -4f));
            secondEcho.type = Image.Type.Sliced;
            secondEcho.fillCenter = false;

            var frame = CreateImage("Portrait Frame", area, GetFrameSprite());
            frame.color = new Color(0.72f, 0.82f, 0.83f, 0.9f);
            frame.type = Image.Type.Sliced;
            frame.fillCenter = false;
            Stretch(frame.rectTransform, Vector4.zero);

            if (subtitleArea)
            {
                var caption = CreateText("Portrait Caption", panel, "人 格 侧 写", 15, MutedTextColor, TextAnchor.MiddleCenter);
                var captionRect = caption.rectTransform;
                captionRect.anchorMin = new Vector2(mirror ? 1f : 0f, 0f);
                captionRect.anchorMax = captionRect.anchorMin;
                captionRect.pivot = new Vector2(mirror ? 1f : 0f, 0f);
                captionRect.anchoredPosition = new Vector2(mirror ? -32f : 32f, 21f);
                captionRect.sizeDelta = new Vector2(152f, 22f);
            }
        }

        private static void CreateAccent(RectTransform panel, bool isPlayer)
        {
            var accent = CreateImage("Identity Accent", panel, null);
            accent.color = isPlayer
                ? new Color(AmberColor.r, AmberColor.g, AmberColor.b, 0.75f)
                : new Color(TealColor.r, TealColor.g, TealColor.b, 0.75f);
            var rect = accent.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(isPlayer ? 224f : 218f, -3f);
            rect.offsetMax = new Vector2(isPlayer ? -36f : -42f, -1f);
        }

        private static void SetTextArea(RectTransform rect, bool mirror, bool isName)
        {
            rect.anchorMin = new Vector2(0f, isName ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, isName ? 1f : 0.5f);
            if (isName)
            {
                rect.offsetMin = new Vector2(mirror ? 44f : 218f, -62f);
                rect.offsetMax = new Vector2(mirror ? -218f : -44f, -20f);
            }
            else
            {
                rect.offsetMin = new Vector2(mirror ? 76f : 218f, 62f);
                rect.offsetMax = new Vector2(mirror ? -218f : -76f, -78f);
            }
        }

        private static void SetContinueButtonRect(Button button, bool mirror)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(mirror ? 0f : 1f, 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(mirror ? 0f : 1f, 0f);
            rect.anchoredPosition = new Vector2(mirror ? 42f : -42f, 20f);
            rect.sizeDelta = new Vector2(116f, 34f);
        }

        private static void SetBottomPanel(RectTransform rect, float height, float bottom, float sideMargin)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-2f * sideMargin, height);
            rect.anchoredPosition = new Vector2(0f, bottom);
        }

        private static void StylePanelImage(Image image, bool clearCenter)
        {
            if (image == null) return;
            image.sprite = GetPanelSprite();
            image.type = Image.Type.Sliced;
            image.fillCenter = !clearCenter;
            image.color = Color.white;
            image.raycastTarget = true;
        }

        private static void StyleButton(Button button, bool response)
        {
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = response ? GetResponseSprite() : GetPanelSprite();
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = response ? new Color(0.8f, 1f, 1f, 1f) : new Color(0.9f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.86f, 0.88f, 1f);
            colors.selectedColor = new Color(1f, 0.9f, 0.72f, 1f);
            colors.disabledColor = new Color(0.45f, 0.5f, 0.54f, 0.65f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }

        private static Text StyleText(Text text, int size, Color color, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;
            return text;
        }

        private static Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var text = go.GetComponent<Text>();
            text.text = content;
            StyleText(text, size, color, alignment);
            go.transform.SetParent(parent, false);
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindParentByName(Transform child, string name)
        {
            var current = child;
            while (current != null)
            {
                if (current.name == name) return current;
                current = current.parent;
            }
            return null;
        }

        private static void Stretch(RectTransform rect, Vector4 offsets)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(offsets.x, offsets.y);
            rect.offsetMax = new Vector2(-offsets.z, -offsets.w);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static Sprite GetPanelSprite()
        {
            return EnsureSprite("DialoguePanel", 64, 64, new Vector4(16f, 16f, 16f, 16f), (x, y) =>
            {
                if (!InsideRoundedRect(x, y, 64, 64, 8)) return Color.clear;
                var edge = x < 2 || y < 2 || x >= 62 || y >= 62;
                if (edge) return new Color(TealColor.r, TealColor.g, TealColor.b, 0.55f);
                if (y == 60) return new Color(TealColor.r, TealColor.g, TealColor.b, 0.25f);
                return PanelColor;
            });
        }

        private static Sprite GetResponseSprite()
        {
            return EnsureSprite("ResponseButton", 64, 64, new Vector4(12f, 12f, 12f, 12f), (x, y) =>
            {
                if (!InsideRoundedRect(x, y, 64, 64, 6)) return Color.clear;
                if (x < 3) return new Color(TealColor.r, TealColor.g, TealColor.b, 0.9f);
                if (x < 5 || y < 1 || y >= 63 || x >= 63)
                    return new Color(TealColor.r, TealColor.g, TealColor.b, 0.35f);
                return new Color(0.055f, 0.085f, 0.12f, 0.96f);
            });
        }

        private static Sprite GetFrameSprite()
        {
            return EnsureSprite("PortraitFrame", 64, 64, new Vector4(10f, 10f, 10f, 10f), (x, y) =>
            {
                var outer = x < 2 || y < 2 || x >= 62 || y >= 62;
                var corner = (x < 12 && (y == 6 || y == 57)) || (x >= 52 && (y == 6 || y == 57)) ||
                    (y < 12 && (x == 6 || x == 57)) || (y >= 52 && (x == 6 || x == 57));
                return outer || corner ? Color.white : Color.clear;
            });
        }

        private static Sprite GetPlaceholderSprite()
        {
            return EnsureSprite("PortraitPlaceholder", 256, 320, Vector4.zero, (x, y) =>
            {
                var gradient = 0.85f + 0.15f * y / 320f;
                var color = new Color(0.065f * gradient, 0.1f * gradient, 0.14f * gradient, 1f);
                var head = Ellipse(x, y, 128f, 198f, 54f, 66f);
                var shoulders = Ellipse(x, y, 128f, 61f, 105f, 110f) && y < 145f;
                var echoHead = Ellipse(x, y, 140f, 198f, 54f, 66f);
                var echoShoulders = Ellipse(x, y, 140f, 61f, 105f, 110f) && y < 145f;
                if (echoHead || echoShoulders)
                    color = new Color(AmberColor.r, AmberColor.g, AmberColor.b, 0.16f);
                if (head || shoulders)
                    color = x < 128
                        ? new Color(TealColor.r, TealColor.g, TealColor.b, 0.42f)
                        : new Color(0.52f, 0.62f, 0.66f, 0.34f);
                if (Mathf.Abs(x - 128) < 1 && y > 34 && y < 278)
                    color = new Color(0.72f, 0.82f, 0.83f, 0.24f);
                return color;
            });
        }

        private static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            var dx = (x - cx) / rx;
            var dy = (y - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }

        private static bool InsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            var nearestX = Mathf.Clamp(x, radius, width - 1 - radius);
            var nearestY = Mathf.Clamp(y, radius, height - 1 - radius);
            var dx = x - nearestX;
            var dy = y - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static Sprite EnsureSprite(string name, int width, int height, Vector4 border, System.Func<int, int, Color> paint)
        {
            var path = TextureDirectory + "/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null) return AssetDatabase.LoadAssetAtPath<Sprite>(path);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++) pixels[y * width + x] = paint(x, y);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = border;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
