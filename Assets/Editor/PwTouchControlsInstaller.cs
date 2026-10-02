using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    /// <summary>
    /// Builds the new on-screen controls: the gameplay rig in Scene_A and the
    /// "Controls" customisation panel in Scene_Lobby. Legacy / duplicated control
    /// objects are removed first so only one rig remains in each scene.
    /// </summary>
    public static class PwTouchControlsInstaller
    {
        private const string GameScene = "Assets/a.last/Flooded_Grounds/Scenes/Scene_A.unity";
        private const string LobbyScene = "Assets/a.loby/Scene_Lobby.unity";
        private const string JoystickPrefab = "Assets/a.last/Flooded_Grounds/Joystick Pack/Prefabs/Fixed Joystick.prefab";
        private const string ArtFolder = "Assets/UI_Lobby";
        private const string IconFolder = "Assets/UI_Icons";
        private const string RigName = "PwTouchControls";
        private const string PanelName = "TouchControlsPanel";
        private const string OpenButtonName = "ControlsBtn";
        private static readonly string[] LegacyNames = { "JumpBtn", "JumpButton", "AutoRunBtn", "AutoRunButton", "Fixed Joystick", "Floating Joystick", "Dynamic Joystick", "Variable Joystick", "MoveJoystick", "TouchControls", RigName };
        private static readonly string[] AdoptNames = { "InteractButton", "ViewSwitchBtn", "FlashlightBtn" };
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color HintColor = new Color(0.76f, 0.69f, 0.66f, 0.9f);
        private static Font font;

        [MenuItem("Palewick/Build Touch Controls (All Scenes)")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            string report = string.Empty;
            if (File.Exists(GameScene))
            {
                Scene scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
                report += BuildGameControls(false) + "\n";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                report += "Scene_A not found.\n";
            }
            if (File.Exists(LobbyScene))
            {
                Scene scene = EditorSceneManager.OpenScene(LobbyScene, OpenSceneMode.Single);
                report += BuildLobbyControls(false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                report += "Scene_Lobby not found.";
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Palewick] Touch controls installed.\n" + report);
            EditorUtility.DisplayDialog("Touch Controls", report, "OK");
        }

        [MenuItem("Palewick/Build Touch Controls (Open Scene_A)")]
        public static void BuildGameControlsMenu()
        {
            string report = BuildGameControls(true);
            EditorUtility.DisplayDialog("Touch Controls", report, "OK");
        }

        [MenuItem("Palewick/Build Controls Panel (Open Scene_Lobby)")]
        public static void BuildLobbyControlsMenu()
        {
            string report = BuildLobbyControls(true);
            EditorUtility.DisplayDialog("Touch Controls", report, "OK");
        }

        // ---------------------------------------------------------------- Scene_A

        private static string BuildGameControls(bool interactive)
        {
            Canvas canvas = FindGameCanvas();
            if (canvas == null)
            {
                return "Scene_A: HUD Canvas (the one holding PubgPauseMenu) was not found.";
            }
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Undo.SetCurrentGroupName("Build Touch Controls");
            int group = Undo.GetCurrentGroup();
            Transform root = canvas.transform;
            // Keep the buttons that other scripts wire by name, move them into the new rig.
            Dictionary<string, Transform> adopted = new Dictionary<string, Transform>();
            for (int i = 0; i < AdoptNames.Length; i++)
            {
                Transform found = FindDeep(root, AdoptNames[i]);
                if (found != null)
                {
                    adopted[AdoptNames[i]] = found;
                }
            }
            int removed = RemoveLegacy(root, adopted);
            RectTransform rig = Node(RigName, root);
            Stretch(rig);
            rig.SetAsLastSibling();
            CanvasGroup rigGroup = rig.gameObject.AddComponent<CanvasGroup>();
            rigGroup.alpha = 0.9f;
            PwTouchControls controls = rig.gameObject.AddComponent<PwTouchControls>();
            controls.layoutRoot = rig;
            RectTransform joystickArea = Node("JoystickArea", rig);
            Place(joystickArea, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(300f, 300f), new Vector2(360f, 360f));
            controls.joystickRect = joystickArea;
            Joystick joystick = SpawnJoystick(joystickArea);
            controls.moveJoystick = joystick;
            RectTransform buttonsArea = Node("ButtonsArea", rig);
            Place(buttonsArea, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(620f, 480f));
            controls.buttonsRect = buttonsArea;
            List<PwTouchButton> list = new List<PwTouchButton>();
            list.Add(MakeButton(buttonsArea, "JumpBtn", "JUMP", PwTouchAction.Jump, new Vector2(-130f, 130f), 180f));
            list.Add(MakeButton(buttonsArea, "SprintBtn", "RUN", PwTouchAction.Sprint, new Vector2(-310f, 180f), 140f));
            RectTransform autoRun = MakeShell(buttonsArea, "AutoRunBtn", "AUTO", new Vector2(-150f, 330f), 120f);
            AutoRunButton autoRunButton = autoRun.gameObject.AddComponent<AutoRunButton>();
            autoRunButton.icon = autoRun.GetComponent<Image>();
            int moved = 0;
            Vector2[] adoptedSpots = { new Vector2(-330f, 350f), new Vector2(-480f, 140f), new Vector2(-490f, 300f) };
            float[] adoptedSizes = { 150f, 115f, 115f };
            for (int i = 0; i < AdoptNames.Length; i++)
            {
                Transform existing;
                if (!adopted.TryGetValue(AdoptNames[i], out existing) || existing == null)
                {
                    continue;
                }
                Undo.SetTransformParent(existing, buttonsArea, "Adopt " + AdoptNames[i]);
                RectTransform rect = existing as RectTransform;
                if (rect != null)
                {
                    Place(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), adoptedSpots[i], new Vector2(adoptedSizes[i], adoptedSizes[i]));
                    rect.localScale = Vector3.one;
                }
                moved++;
            }
            controls.buttons = list;
            EditorUtility.SetDirty(controls);
            Undo.RegisterCreatedObjectUndo(rig.gameObject, "Touch Controls");
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            if (interactive)
            {
                Selection.activeGameObject = rig.gameObject;
            }
            return "Scene_A: rig built (" + list.Count + " new buttons, " + moved + " existing buttons re-placed, " + removed + " legacy objects removed).";
        }

        private static Joystick SpawnJoystick(RectTransform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JoystickPrefab);
            if (prefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (instance != null)
                {
                    instance.name = "MoveJoystick";
                    RectTransform rect = instance.transform as RectTransform;
                    if (rect != null)
                    {
                        Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 340f));
                    }
                    Undo.RegisterCreatedObjectUndo(instance, "Move Joystick");
                    return instance.GetComponent<Joystick>();
                }
            }
            // Fallback: build a fixed joystick by hand if the pack prefab is missing.
            RectTransform back = Node("MoveJoystick", parent);
            Place(back, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 340f));
            Img(back.gameObject, RoundSprite("pw_touch_ring.png", 192, 0.1f, 0.85f), new Color(1f, 1f, 1f, 0.5f));
            RectTransform handle = Node("Handle", back);
            Place(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            Img(handle.gameObject, RoundSprite("pw_touch_dot.png", 128, 1f, 0f), new Color(1f, 1f, 1f, 0.75f));
            FixedJoystick fixedJoystick = back.gameObject.AddComponent<FixedJoystick>();
            SerializedObject so = new SerializedObject(fixedJoystick);
            SerializedProperty background = so.FindProperty("background");
            if (background != null)
            {
                background.objectReferenceValue = back;
            }
            SerializedProperty handleProp = so.FindProperty("handle");
            if (handleProp != null)
            {
                handleProp.objectReferenceValue = handle;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return fixedJoystick;
        }

        private static int RemoveLegacy(Transform root, Dictionary<string, Transform> keep)
        {
            int removed = 0;
            for (int i = 0; i < LegacyNames.Length; i++)
            {
                while (true)
                {
                    Transform found = FindDeep(root, LegacyNames[i]);
                    if (found == null || keep.ContainsValue(found))
                    {
                        break;
                    }
                    Undo.DestroyObjectImmediate(found.gameObject);
                    removed++;
                }
            }
            Joystick[] joysticks = Object.FindObjectsByType<Joystick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < joysticks.Length; i++)
            {
                if (joysticks[i] != null && !keep.ContainsValue(joysticks[i].transform))
                {
                    Undo.DestroyObjectImmediate(joysticks[i].gameObject);
                    removed++;
                }
            }
            return removed;
        }

        // ------------------------------------------------------------ Scene_Lobby

        private static string BuildLobbyControls(bool interactive)
        {
            Canvas canvas = FindLobbyCanvas();
            if (canvas == null)
            {
                return "Scene_Lobby: Canvas was not found (open Scene_Lobby first).";
            }
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Undo.SetCurrentGroupName("Build Controls Panel");
            int group = Undo.GetCurrentGroup();
            Transform root = canvas.transform;
            Transform oldPanel = FindDeep(root, PanelName);
            if (oldPanel != null)
            {
                Undo.DestroyObjectImmediate(oldPanel.gameObject);
            }
            Transform oldButton = FindDeep(root, OpenButtonName);
            if (oldButton != null)
            {
                Undo.DestroyObjectImmediate(oldButton.gameObject);
            }
            RectTransform panel = Node(PanelName, root);
            Stretch(panel);
            panel.SetAsLastSibling();
            Img(panel.gameObject, null, new Color(0f, 0f, 0f, 0.78f));
            RectTransform box = Node("Box", panel);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 720f));
            Img(box.gameObject, Spr("lobby_panel.png"), new Color(1f, 1f, 1f, 0.97f));
            RectTransform title = Node("Title", box);
            Place(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(700f, 70f));
            Label(title.gameObject, "CONTROLS", 54, TextColor, TextAnchor.MiddleCenter);
            // Live preview rig: the very same component used in gameplay, in edit mode.
            RectTransform rig = Node(RigName, box);
            Place(rig, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1100f, 420f));
            PwTouchControls controls = rig.gameObject.AddComponent<PwTouchControls>();
            controls.layoutRoot = rig;
            controls.editMode = true;
            controls.hideWhenNoPlayer = false;
            rig.gameObject.AddComponent<CanvasGroup>();
            RectTransform stage = Node("Stage", rig);
            Stretch(stage);
            Img(stage.gameObject, null, new Color(0.08f, 0.07f, 0.07f, 0.55f));
            RectTransform joystickArea = Node("JoystickArea", rig);
            Place(joystickArea, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(180f, 170f), new Vector2(240f, 240f));
            controls.joystickRect = joystickArea;
            RectTransform preview = Node("JoystickPreview", joystickArea);
            Place(preview, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 220f));
            Img(preview.gameObject, RoundSprite("pw_touch_ring.png", 192, 0.1f, 0.85f), new Color(1f, 1f, 1f, 0.5f));
            RectTransform buttonsArea = Node("ButtonsArea", rig);
            Place(buttonsArea, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(520f, 400f));
            controls.buttonsRect = buttonsArea;
            List<PwTouchButton> list = new List<PwTouchButton>();
            list.Add(MakeButton(buttonsArea, "JumpBtn", "JUMP", PwTouchAction.None, new Vector2(-120f, 120f), 150f));
            list.Add(MakeButton(buttonsArea, "SprintBtn", "RUN", PwTouchAction.None, new Vector2(-270f, 160f), 120f));
            list.Add(MakeButton(buttonsArea, "AutoRunBtn", "AUTO", PwTouchAction.None, new Vector2(-140f, 280f), 105f));
            list.Add(MakeButton(buttonsArea, "InteractBtn", "USE", PwTouchAction.None, new Vector2(-300f, 300f), 120f));
            controls.buttons = list;
            RectTransform hint = Node("Hint", box);
            Place(hint, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(900f, 44f));
            Label(hint.gameObject, "Drag the buttons to move them. Your layout is saved on this device.", 26, HintColor, TextAnchor.MiddleCenter);
            // Sliders and toggles
            Slider size = MakeSlider(box, "SizeSlider", "Button size", new Vector2(-250f, 220f), 0.6f, 1.6f, controls.buttonScale);
            UnityEventTools.AddPersistentListener(size.onValueChanged, new UnityAction<float>(controls.SetScale));
            Slider alpha = MakeSlider(box, "OpacitySlider", "Opacity", new Vector2(250f, 220f), 0.25f, 1f, controls.opacity);
            UnityEventTools.AddPersistentListener(alpha.onValueChanged, new UnityAction<float>(controls.SetOpacity));
            Toggle mirror = MakeToggle(box, "MirrorToggle", "Left handed", new Vector2(-250f, 140f), controls.mirrored);
            UnityEventTools.AddPersistentListener(mirror.onValueChanged, new UnityAction<bool>(controls.SetMirrored));
            Toggle hold = MakeToggle(box, "HoldSprintToggle", "Hold to sprint", new Vector2(250f, 140f), controls.holdToSprint);
            UnityEventTools.AddPersistentListener(hold.onValueChanged, new UnityAction<bool>(controls.SetHoldToSprint));
            Button save = MakeTextButton(box, "SaveBtn", "SAVE", new Vector2(1f, 0f), new Vector2(-60f, 40f), new Vector2(240f, 80f));
            UnityEventTools.AddVoidPersistentListener(save.onClick, new UnityAction(controls.SavePrefs));
            UnityEventTools.AddBoolPersistentListener(save.onClick, new UnityAction<bool>(panel.gameObject.SetActive), false);
            Button reset = MakeTextButton(box, "ResetBtn", "RESET", new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(240f, 80f));
            UnityEventTools.AddVoidPersistentListener(reset.onClick, new UnityAction(controls.ResetLayout));
            Button close = MakeTextButton(box, "CloseBtn", "X", new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(70f, 70f));
            UnityEventTools.AddBoolPersistentListener(close.onClick, new UnityAction<bool>(panel.gameObject.SetActive), false);
            panel.gameObject.SetActive(false);
            // Entry point in the lobby.
            RectTransform openRect = Node(OpenButtonName, root);
            Place(openRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(300f, 92f));
            Image openImage = Img(openRect.gameObject, Spr("lobby_btn_side.png"), new Color(1f, 1f, 1f, 0.95f));
            Button openButton = openRect.gameObject.AddComponent<Button>();
            openButton.targetGraphic = openImage;
            openRect.gameObject.AddComponent<PwUiButtonFx>();
            RectTransform openLabel = Node("Label", openRect);
            Stretch(openLabel);
            Label(openLabel.gameObject, "CONTROLS", 36, TextColor, TextAnchor.MiddleCenter);
            UnityEventTools.AddBoolPersistentListener(openButton.onClick, new UnityAction<bool>(panel.gameObject.SetActive), true);
            EditorUtility.SetDirty(controls);
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Controls Panel");
            Undo.RegisterCreatedObjectUndo(openRect.gameObject, "Controls Button");
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            if (interactive)
            {
                Selection.activeGameObject = panel.gameObject;
            }
            return "Scene_Lobby: controls panel + CONTROLS button built.";
        }

        // ------------------------------------------------------------- UI helpers

        private static PwTouchButton MakeButton(RectTransform parent, string name, string caption, PwTouchAction action, Vector2 position, float size)
        {
            RectTransform rect = MakeShell(parent, name, caption, position, size);
            PwTouchButton button = rect.gameObject.AddComponent<PwTouchButton>();
            button.action = action;
            button.icon = rect.GetComponent<Image>();
            button.background = button.icon;
            return button;
        }

        private static RectTransform MakeShell(RectTransform parent, string name, string caption, Vector2 position, float size)
        {
            RectTransform rect = Node(name, parent);
            Place(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            Img(rect.gameObject, RoundSprite("pw_touch_ring.png", 192, 0.1f, 0.85f), new Color(1f, 1f, 1f, 0.8f));
            RectTransform labelRect = Node("Label", rect);
            Stretch(labelRect);
            Label(labelRect.gameObject, caption, Mathf.RoundToInt(size * 0.26f), TextColor, TextAnchor.MiddleCenter);
            return rect;
        }

        private static Slider MakeSlider(RectTransform parent, string name, string caption, Vector2 position, float min, float max, float value)
        {
            RectTransform holder = Node(name, parent);
            Place(holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(440f, 60f));
            RectTransform caps = Node("Caption", holder);
            Place(caps, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(190f, 50f));
            Label(caps.gameObject, caption, 28, HintColor, TextAnchor.MiddleLeft);
            RectTransform bar = Node("Slider", holder);
            Place(bar, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(230f, 36f));
            Slider slider = bar.gameObject.AddComponent<Slider>();
            RectTransform background = Node("Background", bar);
            Stretch(background);
            Img(background.gameObject, Spr("lobby_bar_bg.png"), new Color(1f, 1f, 1f, 0.6f));
            RectTransform fillArea = Node("Fill Area", bar);
            Stretch(fillArea);
            RectTransform fill = Node("Fill", fillArea);
            Stretch(fill);
            Image fillImage = Img(fill.gameObject, Spr("lobby_bar_fill.png"), new Color(1f, 0.55f, 0.35f, 0.95f));
            RectTransform handleArea = Node("Handle Slide Area", bar);
            Stretch(handleArea);
            RectTransform handle = Node("Handle", handleArea);
            Place(handle, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 0f));
            Image handleImage = Img(handle.gameObject, RoundSprite("pw_touch_dot.png", 128, 1f, 0f), new Color(1f, 0.86f, 0.7f, 1f));
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = Mathf.Clamp(value, min, max);
            fillImage.type = Image.Type.Sliced;
            return slider;
        }

        private static Toggle MakeToggle(RectTransform parent, string name, string caption, Vector2 position, bool value)
        {
            RectTransform holder = Node(name, parent);
            Place(holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(440f, 56f));
            Toggle toggle = holder.gameObject.AddComponent<Toggle>();
            RectTransform caps = Node("Caption", holder);
            Place(caps, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(300f, 50f));
            Label(caps.gameObject, caption, 28, HintColor, TextAnchor.MiddleLeft);
            RectTransform box = Node("Box", holder);
            Place(box, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(52f, 52f));
            Image boxImage = Img(box.gameObject, RoundSprite("pw_touch_ring.png", 192, 0.1f, 0.85f), new Color(1f, 1f, 1f, 0.55f));
            RectTransform check = Node("Check", box);
            Place(check, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            Image checkImage = Img(check.gameObject, RoundSprite("pw_touch_dot.png", 128, 1f, 0f), new Color(1f, 0.6f, 0.4f, 1f));
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = value;
            return toggle;
        }

        private static Button MakeTextButton(RectTransform parent, string name, string caption, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = Node(name, parent);
            Place(rect, anchor, anchor, anchor, position, size);
            Image image = Img(rect.gameObject, Spr("lobby_btn_side.png"), new Color(1f, 1f, 1f, 0.95f));
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            rect.gameObject.AddComponent<PwUiButtonFx>();
            RectTransform label = Node("Label", rect);
            Stretch(label);
            Label(label.gameObject, caption, 34, TextColor, TextAnchor.MiddleCenter);
            return button;
        }

        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Image Img(GameObject go, Sprite sprite, Color color)
        {
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;
            image.preserveAspect = sprite != null;
            return image;
        }

        private static Text Label(GameObject go, string value, int size, Color color, TextAnchor anchor)
        {
            Text text = go.GetComponent<Text>();
            if (text == null)
            {
                text = go.AddComponent<Text>();
            }
            text.text = value;
            text.fontSize = Mathf.Max(8, size);
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (font != null)
            {
                text.font = font;
            }
            else
            {
                text.font = AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
            }
            return text;
        }

        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }

        /// <summary>Circle sprite generated once and cached in Assets/UI_Icons.</summary>
        private static Sprite RoundSprite(string fileName, int size, float innerFill, float ringAlpha)
        {
            string path = IconFolder + "/" + fileName;
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }
            if (!AssetDatabase.IsValidFolder(IconFolder))
            {
                AssetDatabase.CreateFolder("Assets", "UI_Icons");
            }
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            float outer = half - 2f;
            float inner = outer * 0.82f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;
                    if (d <= inner)
                    {
                        alpha = innerFill;
                    }
                    else if (d <= outer)
                    {
                        alpha = ringAlpha;
                    }
                    float edge = Mathf.Clamp01(outer - d);
                    alpha *= Mathf.Clamp01(edge + (d < outer ? 1f : 0f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name == name)
            {
                return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static Canvas FindGameCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && FindDeep(canvases[i].transform, "PubgPauseMenu") != null)
                {
                    return canvases[i];
                }
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static Canvas FindLobbyCanvas()
        {
            LobbyManager lobby = Object.FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
            if (lobby != null)
            {
                Canvas inLobby = lobby.GetComponentInChildren<Canvas>(true);
                if (inLobby != null)
                {
                    return inLobby.rootCanvas != null ? inLobby.rootCanvas : inLobby;
                }
            }
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].name == "Canvas")
                {
                    return canvases[i];
                }
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }
    }
}
