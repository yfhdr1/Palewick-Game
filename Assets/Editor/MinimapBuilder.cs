using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Palewick.EditorTools
{
    public static class MinimapBuilder
    {
        private const string IconFolder = "Assets/UI_Icons";
        private const float Diameter = 250f;
        private const float FrameInner = 0.67f;
        private const float FrameCenter = 0.46f;
        [MenuItem("Palewick/Create Minimap In Canvas")]
        public static void Create()
        {
            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Minimap", "Canvas with PubgPauseMenu not found in the open scene.", "OK");
                return;
            }
            Transform existing = canvas.transform.Find("MinimapRoot");
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Minimap", "MinimapRoot already exists in the Canvas.", "OK");
                return;
            }
            Sprite circle = SaveSprite("minimap_circle.png", Minimap.MakeDiscTexture(128, 0f));
            Sprite dot = SaveSprite("minimap_dot.png", Minimap.MakeDiscTexture(64, 7f));
            Sprite arrow = SaveSprite("minimap_arrow.png", Minimap.MakeArrowTexture(64));
            Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/hud_minimap_frame.png");
            RectTransform root = NewRect("MinimapRoot", canvas.transform);
            Stretch(root);
            int index = canvas.transform.childCount - 1;
            Transform menu = canvas.transform.Find("PubgPauseMenu");
            if (menu != null && menu.GetSiblingIndex() < index)
            {
                index = menu.GetSiblingIndex();
            }
            Transform loading = canvas.transform.Find("LoadingPanel");
            if (loading != null && loading.GetSiblingIndex() < index)
            {
                index = loading.GetSiblingIndex();
            }
            root.SetSiblingIndex(index);
            RectTransform box = NewRect("Minimap", root);
            Place(box, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -170f), new Vector2(Diameter, Diameter));
            Image maskImage = AddImage(NewRect("Mask", box), Color.white);
            maskImage.sprite = circle;
            Stretch(maskImage.rectTransform);
            Mask mask = maskImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            Image back = AddImage(NewRect("Back", maskImage.rectTransform), new Color(0.05f, 0.02f, 0.02f, 0.85f));
            Stretch(back.rectTransform);
            RawImage map = NewRect("Map", maskImage.rectTransform).gameObject.AddComponent<RawImage>();
            map.color = new Color(1f, 0.92f, 0.9f, 0.92f);
            map.raycastTarget = false;
            Stretch(map.rectTransform);
            RectTransform markers = NewRect("Markers", maskImage.rectTransform);
            Stretch(markers);
            Image me = AddImage(NewRect("Me", markers), new Color(0.93f, 0.78f, 0.1f, 1f));
            me.sprite = arrow;
            Place(me.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            Image frame = AddImage(NewRect("Frame", box), Color.white);
            float f = Diameter / FrameInner;
            Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -(0.5f - FrameCenter) * f), new Vector2(f, f));
            if (frameSprite != null)
            {
                frame.sprite = frameSprite;
            }
            else
            {
                frame.enabled = false;
                Debug.LogWarning("Minimap: Assets/Resources/hud_minimap_frame.png not found or not a Sprite.");
            }
            Minimap mm = box.gameObject.AddComponent<Minimap>();
            mm.mapImage = map;
            mm.markers = markers;
            mm.localArrow = me;
            mm.dotSprite = dot;
            CreateFullMap(root, mm);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Minimap");
            Selection.activeGameObject = box.gameObject;
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }
        private static readonly Color PanelDark = new Color(0.047f, 0.05f, 0.055f, 0.985f);
        private static readonly Color FrameLine = new Color(0.26f, 0.28f, 0.3f, 1f);
        private static readonly Color AccentLine = new Color(0.78f, 0.62f, 0.2f, 1f);
        private static readonly Color ButtonDark = new Color(0.09f, 0.095f, 0.105f, 0.96f);
        private static readonly Color TextBone = new Color(0.9f, 0.9f, 0.86f, 1f);
        private static readonly Color TextDim = new Color(0.62f, 0.64f, 0.62f, 1f);
        private static void CreateFullMap(RectTransform root, Minimap minimap)
        {
            GameObject panelObject = new GameObject("FullMapPanel", typeof(RectTransform), typeof(Image));
            panelObject.layer = 5;
            panelObject.transform.SetParent(root, false);
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            Stretch(panel);
            Image dim = panelObject.GetComponent<Image>();
            dim.color = new Color(0.008f, 0.009f, 0.012f, 0.97f);
            dim.raycastTarget = true;
            RectTransform frame = NewRect("MapFrame", panel);
            frame.anchorMin = new Vector2(0.055f, 0.045f);
            frame.anchorMax = new Vector2(0.945f, 0.955f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
            Image frameImg = frame.gameObject.AddComponent<Image>();
            frameImg.color = PanelDark;
            frameImg.raycastTarget = true;
            Outline frameOutline = frame.gameObject.AddComponent<Outline>();
            frameOutline.effectColor = FrameLine;
            frameOutline.effectDistance = new Vector2(2f, -2f);
            RectTransform header = NewRect("Header", frame);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 86f);
            Image headerImg = AddImage(header, new Color(0.03f, 0.032f, 0.038f, 1f));
            headerImg.raycastTarget = true;
            RectTransform headerLine = NewRect("HeaderLine", header);
            headerLine.anchorMin = new Vector2(0f, 0f);
            headerLine.anchorMax = new Vector2(1f, 0f);
            headerLine.pivot = new Vector2(0.5f, 0f);
            headerLine.anchoredPosition = Vector2.zero;
            headerLine.sizeDelta = new Vector2(0f, 2f);
            AddImage(headerLine, AccentLine);
            MapLabel(header, "Title", "TACTICAL MAP", 40, TextBone, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, 0f), new Vector2(-240f, 0f), true);
            Text scale = MapLabel(header, "ScaleText", "1000 m", 30, TextDim, TextAnchor.MiddleRight,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, 0f), new Vector2(-110f, 0f), false);
            RectTransform view = NewRect("MapView", frame);
            view.anchorMin = new Vector2(0.018f, 0.03f);
            view.anchorMax = new Vector2(0.982f, 1f);
            view.offsetMin = new Vector2(0f, 24f);
            view.offsetMax = new Vector2(0f, -100f);
            Image viewBack = view.gameObject.AddComponent<Image>();
            viewBack.color = new Color(0.016f, 0.02f, 0.024f, 1f);
            viewBack.raycastTarget = false;
            Outline viewOutline = view.gameObject.AddComponent<Outline>();
            viewOutline.effectColor = new Color(0.18f, 0.2f, 0.22f, 1f);
            viewOutline.effectDistance = new Vector2(1.5f, -1.5f);
            RawImage map = NewRect("FullMap", view).gameObject.AddComponent<RawImage>();
            map.color = new Color(0.78f, 0.76f, 0.72f, 1f);
            map.raycastTarget = true;
            Stretch(map.rectTransform);
            RawImage grid = NewRect("GridOverlay", view).gameObject.AddComponent<RawImage>();
            grid.color = Color.white;
            grid.raycastTarget = false;
            Stretch(grid.rectTransform);
            grid.texture = SaveGridTexture(Minimap.MakeGridTexture(128));
            RectTransform markers = NewRect("Markers", view);
            Stretch(markers);
            CreateCorners(view);
            Button close = TacticalButton(frame, "CloseMap", "X", new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(62f, 62f), 34);
            Button zoomIn = TacticalButton(frame, "ZoomIn", "+", new Vector2(1f, 0f), new Vector2(-26f, 196f), new Vector2(78f, 78f), 46);
            Button zoomOut = TacticalButton(frame, "ZoomOut", "-", new Vector2(1f, 0f), new Vector2(-26f, 104f), new Vector2(78f, 78f), 46);
            CreateLegend(frame);
            panelObject.SetActive(false);
            SetField(minimap, "fullMapPanel", panelObject);
            SetField(minimap, "fullMapImage", map);
            SetField(minimap, "fullMapMarkers", markers);
            SetField(minimap, "closeMapButton", close);
            SetField(minimap, "zoomInButton", zoomIn);
            SetField(minimap, "zoomOutButton", zoomOut);
            SetField(minimap, "fullMapGrid", grid);
            SetField(minimap, "fullMapScaleText", scale);
        }
        private static void SetField(Minimap minimap, string field, Object value)
        {
            minimap.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(minimap, value);
        }
        private static Texture2D SaveGridTexture(Texture2D tex)
        {
            string path = IconFolder + "/map_grid.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static void CreateCorners(RectTransform view)
        {
            Vector2[] anchors = { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = anchors[i];
                float sx = a.x == 0f ? 1f : -1f;
                float sy = a.y == 0f ? 1f : -1f;
                RectTransform h = NewRect("CornerH" + i, view);
                h.anchorMin = a;
                h.anchorMax = a;
                h.pivot = a;
                h.anchoredPosition = new Vector2(sx * 8f, sy * 8f);
                h.sizeDelta = new Vector2(46f, 4f);
                AddImage(h, AccentLine);
                RectTransform v = NewRect("CornerV" + i, view);
                v.anchorMin = a;
                v.anchorMax = a;
                v.pivot = a;
                v.anchoredPosition = new Vector2(sx * 8f, sy * 8f);
                v.sizeDelta = new Vector2(4f, 46f);
                AddImage(v, AccentLine);
            }
        }
        private static void CreateLegend(RectTransform frame)
        {
            RectTransform legend = NewRect("Legend", frame);
            legend.anchorMin = new Vector2(0f, 0f);
            legend.anchorMax = new Vector2(0f, 0f);
            legend.pivot = new Vector2(0f, 0f);
            legend.anchoredPosition = new Vector2(30f, 36f);
            legend.sizeDelta = new Vector2(420f, 48f);
            Image back = AddImage(legend, new Color(0.03f, 0.032f, 0.038f, 0.9f));
            back.raycastTarget = false;
            Sprite dot = AssetDatabase.LoadAssetAtPath<Sprite>(IconFolder + "/minimap_dot.png");
            RectTransform d1 = NewRect("PlayerDot", legend);
            Place(d1, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), new Vector2(20f, 20f));
            Image i1 = AddImage(d1, new Color(0.93f, 0.78f, 0.1f, 1f));
            i1.sprite = dot;
            MapLabel(legend, "PlayerLabel", "PLAYERS", 24, TextDim, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(52f, 0f), new Vector2(0f, 0f), false);
            RectTransform d2 = NewRect("MonsterDot", legend);
            Place(d2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 0f), new Vector2(20f, 20f));
            Image i2 = AddImage(d2, new Color(0.9f, 0.04f, 0.04f, 1f));
            i2.sprite = dot;
            MapLabel(legend, "MonsterLabel", "MONSTER", 24, TextDim, TextAnchor.MiddleLeft,
                new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(52f, 0f), new Vector2(0f, 0f), false);
        }
        private static Text MapLabel(RectTransform parent, string name, string value, int size, Color color, TextAnchor anchor, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, bool bold)
        {
            RectTransform rt = NewRect(name, parent);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Shadow shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }
        private static Button TacticalButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize)
        {
            RectTransform rt = NewRect(name, parent);
            Place(rt, anchor, anchor, pos, size);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = ButtonDark;
            image.raycastTarget = true;
            Outline outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = FrameLine;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.9f, 1f);
            colors.pressedColor = new Color(0.75f, 0.68f, 0.5f, 1f);
            button.colors = colors;
            RectTransform labelRect = NewRect("Label", rt);
            Stretch(labelRect);
            Text text = labelRect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = fontSize;
            text.color = TextBone;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return button;
        }
        private static Canvas FindCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].transform.Find("PubgPauseMenu") != null)
                {
                    return canvases[i];
                }
            }
            return null;
        }
        private static Sprite SaveSprite(string fileName, Texture2D tex)
        {
            string path = IconFolder + "/" + fileName;
            if (!AssetDatabase.IsValidFolder(IconFolder))
            {
                AssetDatabase.CreateFolder("Assets", "UI_Icons");
            }
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
        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }
        private static Image AddImage(RectTransform rt, Color color)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}