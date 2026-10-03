using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Palewick.EditorTools
{
    /// <summary>
    /// Replaces every legacy/customisable mobile layout with one compact gameplay HUD.
    /// Scene_Lobby never receives gameplay controls; old Controls-panel artifacts are
    /// removed by RemoveLobbyControls.
    /// </summary>
    public static class PwTouchControlsInstaller
    {
        public const string GameScenePath = "Assets/a.last/Flooded_Grounds/Scenes/Scene_A.unity";
        public const string LobbyScenePath = "Assets/a.loby/Scene_Lobby.unity";

        private const string JoystickPrefab = "Assets/a.last/Flooded_Grounds/Joystick Pack/Prefabs/Fixed Joystick.prefab";
        private const string IconFolder = "Assets/UI_Icons";
        private const string RigName = "PwTouchControls";

        private static readonly string[] LegacyGameplayNames =
        {
            RigName,
            "TouchControls",
            "Fixed Joystick",
            "Floating Joystick",
            "Dynamic Joystick",
            "Variable Joystick",
            "MoveJoystick",
            "JumpBtn",
            "JumpButton",
            "SprintBtn",
            "AutoRunBtn",
            "AutoRunButton",
            "InteractBtn",
            "InteractButton",
            "ViewSwitchBtn",
            "FlashlightBtn"
        };

        private static readonly string[] LobbyArtifactNames =
        {
            "TouchControlsPanel",
            "ControlsBtn",
            "ControlsPanel",
            "ControlPanel",
            "ControlsCustomizationPanel",
            RigName
        };

        /// <summary>Builds the clean joystick/jump/interact/flashlight HUD in Scene_A.</summary>
        public static TouchControlsResult RebuildGameplayControls(Scene scene)
        {
            TouchControlsResult result = new TouchControlsResult();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                result.error = "Scene_A is not loaded.";
                return result;
            }

            Canvas canvas = FindGameplayCanvas(scene);
            if (canvas == null)
            {
                result.error = "A gameplay Canvas was not found in Scene_A.";
                return result;
            }

            result.removedObjects = RemoveLegacyGameplayObjects(canvas.transform);

            RectTransform rig = CreateRect(RigName, canvas.transform);
            Stretch(rig);
            rig.SetAsLastSibling();

            CanvasGroup group = rig.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.88f;
            group.interactable = true;
            group.blocksRaycasts = true;

            PwTouchControls controls = rig.gameObject.AddComponent<PwTouchControls>();
            controls.opacity = 0.88f;
            controls.hideWhenNoPlayer = true;

            RectTransform joystickArea = CreateRect("JoystickArea", rig);
            Place(
                joystickArea,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(205f, 205f),
                new Vector2(330f, 330f));
            controls.moveJoystick = CreateJoystick(joystickArea);

            RectTransform buttons = CreateRect("ActionButtons", rig);
            Stretch(buttons);

            List<PwTouchButton> actions = new List<PwTouchButton>();
            actions.Add(CreateActionButton(
                buttons,
                "JumpBtn",
                PwTouchAction.Jump,
                "hud_jump.png",
                "JUMP",
                new Vector2(-145f, 145f),
                170f));
            actions.Add(CreateActionButton(
                buttons,
                "InteractButton",
                PwTouchAction.Interact,
                "hud_door.png",
                "USE",
                new Vector2(-340f, 125f),
                135f));
            PwTouchButton flashlightButton = CreateActionButton(
                buttons,
                "FlashlightBtn",
                PwTouchAction.Flashlight,
                "hud_flashlight.png",
                "LIGHT",
                new Vector2(-150f, 330f),
                130f);
            actions.Add(flashlightButton);

            // PlayerSetup discovers this bridge and gives it the spawned local
            // player's actual flashlight object.
            FlashlightController flashlightController =
                flashlightButton.gameObject.AddComponent<FlashlightController>();
            flashlightController.flashlightIcon = flashlightButton.icon;

            controls.buttons = actions;
            for (int i = 0; i < actions.Count; i++)
            {
                actions[i].owner = controls;
                EditorUtility.SetDirty(actions[i]);
            }
            EditorUtility.SetDirty(controls);

            result.createdButtons = actions.Count;
            result.createdJoystick = controls.moveJoystick != null;
            EditorSceneManager.MarkSceneDirty(scene);
            return result;
        }

        /// <summary>Removes the obsolete CONTROLS panel and its launcher from Scene_Lobby.</summary>
        public static int RemoveLobbyControls(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }

            List<GameObject> targets = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    Transform transform = transforms[t];
                    if (transform != null && IsLobbyArtifactName(transform.name))
                    {
                        AddTopmostTarget(targets, transform.gameObject);
                    }
                }

                PwTouchControls[] controls = roots[i].GetComponentsInChildren<PwTouchControls>(true);
                for (int c = 0; c < controls.Length; c++)
                {
                    if (controls[c] != null)
                    {
                        AddTopmostTarget(targets, controls[c].gameObject);
                    }
                }
            }

            int removed = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }
                Object.DestroyImmediate(targets[i]);
                removed++;
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            return removed;
        }

        private static int RemoveLegacyGameplayObjects(Transform canvasRoot)
        {
            List<GameObject> targets = new List<GameObject>();
            Transform[] transforms = canvasRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform != null && IsLegacyGameplayName(transform.name))
                {
                    AddTopmostTarget(targets, transform.gameObject);
                }
            }

            Joystick[] joysticks = canvasRoot.GetComponentsInChildren<Joystick>(true);
            for (int i = 0; i < joysticks.Length; i++)
            {
                if (joysticks[i] != null)
                {
                    AddTopmostTarget(targets, joysticks[i].gameObject);
                }
            }

            int removed = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }
                Object.DestroyImmediate(targets[i]);
                removed++;
            }
            return removed;
        }

        private static void AddTopmostTarget(List<GameObject> targets, GameObject candidate)
        {
            if (candidate == null)
            {
                return;
            }

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                GameObject existing = targets[i];
                if (existing == null)
                {
                    targets.RemoveAt(i);
                    continue;
                }
                if (candidate.transform.IsChildOf(existing.transform))
                {
                    return;
                }
                if (existing.transform.IsChildOf(candidate.transform))
                {
                    targets.RemoveAt(i);
                }
            }

            targets.Add(candidate);
        }

        private static bool IsLegacyGameplayName(string objectName)
        {
            for (int i = 0; i < LegacyGameplayNames.Length; i++)
            {
                if (objectName == LegacyGameplayNames[i])
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsLobbyArtifactName(string objectName)
        {
            for (int i = 0; i < LobbyArtifactNames.Length; i++)
            {
                if (objectName == LobbyArtifactNames[i])
                {
                    return true;
                }
            }
            return false;
        }

        private static Joystick CreateJoystick(RectTransform parent)
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
                        Place(
                            rect,
                            new Vector2(0.5f, 0.5f),
                            new Vector2(0.5f, 0.5f),
                            new Vector2(0.5f, 0.5f),
                            Vector2.zero,
                            new Vector2(320f, 320f));
                    }
                    StyleHorrorJoystick(instance);
                    return instance.GetComponent<Joystick>();
                }
            }

            // Safe fallback if the joystick-pack prefab is ever removed.
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            RectTransform background = CreateRect("MoveJoystick", parent);
            Place(
                background,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(320f, 320f));
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = uiSprite;
            backgroundImage.color = new Color(0.16f, 0.025f, 0.035f, 0.72f);
            Outline backgroundOutline = background.gameObject.AddComponent<Outline>();
            backgroundOutline.effectColor = new Color(0.52f, 0.045f, 0.06f, 0.9f);
            backgroundOutline.effectDistance = new Vector2(4f, -4f);

            RectTransform handle = CreateRect("Handle", background);
            Place(
                handle,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(120f, 120f));
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = knobSprite;
            handleImage.color = new Color(0.86f, 0.78f, 0.7f, 0.92f);
            Outline handleOutline = handle.gameObject.AddComponent<Outline>();
            handleOutline.effectColor = new Color(0.42f, 0.025f, 0.035f, 0.95f);
            handleOutline.effectDistance = new Vector2(3f, -3f);

            FixedJoystick joystick = background.gameObject.AddComponent<FixedJoystick>();
            SerializedObject serialized = new SerializedObject(joystick);
            SerializedProperty backgroundProperty = serialized.FindProperty("background");
            SerializedProperty handleProperty = serialized.FindProperty("handle");
            if (backgroundProperty != null)
            {
                backgroundProperty.objectReferenceValue = background;
            }
            if (handleProperty != null)
            {
                handleProperty.objectReferenceValue = handle;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return joystick;
        }

        private static void StyleHorrorJoystick(GameObject joystickObject)
        {
            Image[] images = joystickObject.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) continue;
                bool handle = image.transform.name.IndexOf("Handle", System.StringComparison.OrdinalIgnoreCase) >= 0;
                image.color = handle
                    ? new Color(0.86f, 0.78f, 0.7f, 0.92f)
                    : new Color(0.16f, 0.025f, 0.035f, 0.72f);
                image.preserveAspect = true;
                Outline outline = image.GetComponent<Outline>();
                if (outline == null) outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = handle
                    ? new Color(0.42f, 0.025f, 0.035f, 0.95f)
                    : new Color(0.52f, 0.045f, 0.06f, 0.9f);
                outline.effectDistance = handle ? new Vector2(3f, -3f) : new Vector2(4f, -4f);
            }
        }

        private static PwTouchButton CreateActionButton(
            RectTransform parent,
            string objectName,
            PwTouchAction action,
            string iconFile,
            string fallbackLabel,
            Vector2 position,
            float size)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Place(
                rect,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f),
                position,
                new Vector2(size, size));

            Sprite horrorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + "/" + iconFile);
            Image background = rect.gameObject.AddComponent<Image>();
            background.sprite = horrorSprite;
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.preserveAspect = true;
            background.raycastTarget = true;

            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(7f, -7f);
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.48f, 0.02f, 0.035f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);

            if (horrorSprite == null)
            {
                background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                background.color = new Color(0.16f, 0.025f, 0.035f, 0.94f);
                RectTransform labelRect = CreateRect("Label", rect);
                Stretch(labelRect);
                Text label = labelRect.gameObject.AddComponent<Text>();
                label.text = fallbackLabel;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(0.9f, 0.84f, 0.78f, 1f);
                label.fontSize = Mathf.Max(16, Mathf.RoundToInt(size * 0.2f));
                label.raycastTarget = false;
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            PwTouchButton button = rect.gameObject.AddComponent<PwTouchButton>();
            button.action = action;
            button.background = background;
            button.icon = background;
            return button;
        }

        private static Canvas FindGameplayCanvas(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            Canvas fallback = null;
            for (int i = 0; i < roots.Length; i++)
            {
                Canvas[] canvases = roots[i].GetComponentsInChildren<Canvas>(true);
                for (int c = 0; c < canvases.Length; c++)
                {
                    Canvas canvas = canvases[c];
                    if (canvas == null)
                    {
                        continue;
                    }
                    if (fallback == null)
                    {
                        fallback = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                    }
                    if (FindDeep(canvas.transform, "PubgPauseMenu") != null ||
                        FindDeep(canvas.transform, "JumpBtn") != null)
                    {
                        return canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
                    }
                }
            }
            return fallback;
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name == objectName)
            {
                return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.layer = 5;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Place(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
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
    }

    public struct TouchControlsResult
    {
        public int removedObjects;
        public int createdButtons;
        public bool createdJoystick;
        public string error;

        public bool Succeeded
        {
            get { return string.IsNullOrEmpty(error); }
        }

        public override string ToString()
        {
            if (!Succeeded)
            {
                return "Scene_A controls: FAILED - " + error;
            }
            return "Scene_A controls: joystick=" + (createdJoystick ? "yes" : "no") +
                ", action buttons=" + createdButtons +
                ", old objects removed=" + removedObjects + ".";
        }
    }
}
