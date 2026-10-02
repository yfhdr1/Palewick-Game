using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class MissingMaterialFixer
    {
        private const string FixFolder = "Assets/PwFixedMaterials";
        [MenuItem("Palewick/Fix Missing Materials")]
        public static void Fix()
        {
            int brokenAssets = FixBrokenMaterialAssets();
            int fixedSlots = FixSceneRenderers();
            EditorSceneManager.MarkAllScenesDirty();
            string report = "Material assets repaired (broken shader): " + brokenAssets +
                "\nRenderer slots fixed in the open scene: " + fixedSlots +
                "\n\nSave the scene (Ctrl+S) to keep the changes.";
            Debug.Log("[Palewick] Missing material fix done.\n" + report);
            EditorUtility.DisplayDialog("Fix Missing Materials", report, "OK");
        }
        private static bool IsBroken(Material material)
        {
            if (material == null) return true;
            if (material.shader == null) return true;
            string shaderName = material.shader.name;
            return shaderName == "Hidden/InternalErrorShader" || shaderName.Contains("InternalError");
        }
        private static int FixBrokenMaterialAssets()
        {
            int count = 0;
            Shader standard = Shader.Find("Standard");
            if (standard == null) return 0;
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith("Assets/Photon") || path.StartsWith("Assets/TextMesh Pro")) continue;
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !IsBroken(material)) continue;
                Texture mainTex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                Color color = material.HasProperty("_Color") ? material.color : Color.white;
                material.shader = standard;
                if (mainTex != null && material.HasProperty("_MainTex")) material.SetTexture("_MainTex", mainTex);
                if (material.HasProperty("_Color")) material.color = color;
                if (mainTex == null) TryBindTextureByName(material, Path.GetFileNameWithoutExtension(path));
                EditorUtility.SetDirty(material);
                count++;
            }
            if (count > 0) AssetDatabase.SaveAssets();
            return count;
        }
        private static int FixSceneRenderers()
        {
            int count = 0;
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer is ParticleSystemRenderer) continue;
                Material[] shared = renderer.sharedMaterials;
                bool dirty = false;
                for (int m = 0; m < shared.Length; m++)
                {
                    if (!IsBroken(shared[m])) continue;
                    Material replacement = FindReplacement(renderer, m);
                    if (replacement == null) replacement = FallbackMaterial();
                    if (replacement == null) continue;
                    Undo.RecordObject(renderer, "Fix Material");
                    shared[m] = replacement;
                    dirty = true;
                    count++;
                }
                if (dirty)
                {
                    renderer.sharedMaterials = shared;
                    EditorUtility.SetDirty(renderer);
                }
            }
            return count;
        }
        private static Material FindReplacement(Renderer renderer, int slot)
        {
            List<string> names = new List<string>();
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) names.Add(CleanName(filter.sharedMesh.name));
            names.Add(CleanName(renderer.gameObject.name));
            Transform parent = renderer.transform.parent;
            if (parent != null) names.Add(CleanName(parent.name));
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                if (string.IsNullOrEmpty(name) || name.Length < 3) continue;
                string[] guids = AssetDatabase.FindAssets(name + " t:Material");
                for (int g = 0; g < guids.Length; g++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                    Material candidate = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (candidate != null && !IsBroken(candidate)) return candidate;
                }
            }
            return null;
        }
        private static string CleanName(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            value = value.Replace("(Clone)", "").Trim();
            int space = value.IndexOf(" (");
            if (space > 0) value = value.Substring(0, space);
            for (int i = value.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(value[i]) && value[i] != '_' && value[i] != '.' && value[i] != ' ')
                {
                    return value.Substring(0, i + 1);
                }
            }
            return value;
        }
        private static Material FallbackMaterial()
        {
            Shader standard = Shader.Find("Standard");
            if (standard == null) return null;
            if (!AssetDatabase.IsValidFolder(FixFolder))
            {
                AssetDatabase.CreateFolder("Assets", "PwFixedMaterials");
            }
            string path = FixFolder + "/PwFallbackWood.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(standard);
                material.color = new Color(0.36f, 0.3f, 0.24f, 1f);
                material.SetFloat("_Glossiness", 0.08f);
                TryBindTextureByName(material, "wood");
                AssetDatabase.CreateAsset(material, path);
                AssetDatabase.SaveAssets();
            }
            return material;
        }
        private static void TryBindTextureByName(Material material, string name)
        {
            if (material == null || !material.HasProperty("_MainTex") || string.IsNullOrEmpty(name)) return;
            string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D", new[] { "Assets/a.last" });
            for (int i = 0; i < guids.Length; i++)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (texture != null)
                {
                    material.SetTexture("_MainTex", texture);
                    return;
                }
            }
        }
    }
}
