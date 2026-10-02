using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class MobileOptimizationTool
    {
        private static readonly string[] UiFolders = { "Assets/UI_Icons", "Assets/UI_Lobby" };
        [MenuItem("Palewick/Optimize Mobile Performance")]
        public static void Optimize()
        {
            int scalers = FixCanvasScalers();
            int raycasts = DisableTextRaycasts();
            int textures = CompressUiTextures();
            EditorSceneManager.MarkAllScenesDirty();
            string report = "Canvas Scalers fixed: " + scalers +
                "\nText raycast targets disabled: " + raycasts +
                "\nUI textures set to ASTC for Android: " + textures +
                "\n\nSave the scene (Ctrl+S) to keep the changes.";
            Debug.Log("[Palewick] Mobile optimization done.\n" + report);
            EditorUtility.DisplayDialog("Mobile Optimization", report, "OK");
        }
        private static int FixCanvasScalers()
        {
            int count = 0;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isRootCanvas) continue;
                if (canvas.pixelPerfect)
                {
                    Undo.RecordObject(canvas, "Optimize Canvas");
                    canvas.pixelPerfect = false;
                    EditorUtility.SetDirty(canvas);
                }
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) continue;
                bool changed = scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                    scaler.referenceResolution != new Vector2(1920f, 1080f) ||
                    scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight ||
                    !Mathf.Approximately(scaler.matchWidthOrHeight, 1f);
                if (!changed) continue;
                Undo.RecordObject(scaler, "Optimize Scaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
                EditorUtility.SetDirty(scaler);
                count++;
            }
            return count;
        }
        private static int DisableTextRaycasts()
        {
            int count = 0;
            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null || !text.raycastTarget) continue;
                if (NeedsRaycast(text)) continue;
                Undo.RecordObject(text, "Optimize Text");
                text.raycastTarget = false;
                EditorUtility.SetDirty(text);
                count++;
            }
            return count;
        }
        private static bool NeedsRaycast(Graphic graphic)
        {
            if (graphic.GetComponent<IEventSystemHandler>() != null) return true;
            Selectable[] selectables = graphic.GetComponentsInParent<Selectable>(true);
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i] != null && selectables[i].targetGraphic == graphic) return true;
            }
            if (graphic.GetComponentInParent<InputField>(true) != null) return true;
            return false;
        }
        private static int CompressUiTextures()
        {
            int count = 0;
            List<string> paths = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", UiFolders);
            for (int i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
            for (int i = 0; i < paths.Count; i++)
            {
                TextureImporter importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
                if (importer == null) continue;
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                bool changed = false;
                if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6)
                {
                    android.overridden = true;
                    android.format = TextureImporterFormat.ASTC_6x6;
                    android.maxTextureSize = Mathf.Min(android.maxTextureSize, 1024);
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (changed)
                {
                    importer.SetPlatformTextureSettings(android);
                    importer.SaveAndReimport();
                    count++;
                }
            }
            return count;
        }
    }
}
