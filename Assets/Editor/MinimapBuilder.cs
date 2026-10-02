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
        private static void CreateFullMap(RectTransform root, Minimap minimap)
        {
            GameObject panelObject = new GameObject("FullMapPanel", typeof(RectTransform), typeof(Image));
            panelObject.layer = 5;
            panelObject.transform.SetParent(root, false);
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            Stretch(panel);
            Image dim = panelObject.GetComponent<Image>();
            dim.color = new Color(0.01f, 0.005f, 0.008f, 0.94f);
            dim.raycastTarget = true;

            RawImage map = panelObject.transform.Find("FullMap") == null
                ? NewRect("FullMap", panel).gameObject.AddComponent<RawImage>()
                : panelObject.transform.Find("FullMap").GetComponent<RawImage>();
            map.color = new Color(0.72f, 0.68f, 0.62f, 1f);
            map.raycastTarget = true;
            map.rectTransform.anchorMin = new Vector2(0.08f, 0.1f);
            map.rectTransform.anchorMax = new Vector2(0.92f, 0.9f);
            map.rectTransform.offsetMin = Vector2.zero;
            map.rectTransform.offsetMax = Vector2.zero;

            RectTransform markers = NewRect("Markers", map.transform);
            Stretch(markers);
            Button close = CreateButton(panel, "CloseMap", "X", new Vector2(0.92f, 0.9f), new Vector2(120f, 80f));
            Button zoomIn = CreateButton(panel, "ZoomIn", "+", new Vector2(0.88f, 0.2f), new Vector2(100f, 80f));
            Button zoomOut = CreateButton(panel, "ZoomOut", "−", new Vector2(0.88f, 0.1f), new Vector2(100f, 80f));
            panelObject.SetActive(false);
            minimap.GetType().GetField("fullMapPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(minimap, panelObject);
            minimap.GetType().GetField("fullMapImage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(minimap, map);
            minimap.GetType().GetField("fullMapMarkers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(minimap, markers);
            minimap.GetType().GetField("closeMapButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(minimap, close);
            minimap.GetType().GetField("zoomInButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(minimap, zoomIn);
            minimap.GetType().GetField("zoomOutButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(minimap, zoomOut);
        }
        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 size)
        {
            RectTransform rt = NewRect(name, parent);
            Place(rt, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.02f, 0.025f, 0.88f);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            RectTransform labelRect = NewRect("Label", rt);
            Text text = labelRect.gameObject.AddComponent<Text>();
            Stretch(labelRect);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 42;
            text.color = new Color(0.95f, 0.88f, 0.82f, 1f);
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