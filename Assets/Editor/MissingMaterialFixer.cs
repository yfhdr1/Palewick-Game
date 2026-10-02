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
            int rebound = FixFloodedGroundsAlbedo();
            int fixedSlots = FixSceneRenderers();
            EditorSceneManager.MarkAllScenesDirty();
            string report = "Material assets repaired (broken shader): " + brokenAssets +
                "\nAlbedo textures re-bound (Flooded_Grounds / empty Standard): " + rebound +
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
        private const string FloodedTextures = "Assets/a.last/Flooded_Grounds/Content/Textures";
        private static bool IsAlbedoTexture(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            string lower = fileName.ToLowerInvariant();
            if (lower.EndsWith("_n") || lower.EndsWith("_normal") || lower.EndsWith("_nrm")) return false;
            if (lower.EndsWith("_ao") || lower.EndsWith("_occlusion")) return false;
            if (lower.EndsWith("_m") || lower.EndsWith("_ms") || lower.EndsWith("_metallic") || lower.EndsWith("_rough") || lower.EndsWith("_roughness") || lower.EndsWith("_s") || lower.EndsWith("_h") || lower.EndsWith("_height")) return false;
            return true;
        }
        private static bool IsAlbedoFor(string textureName, string baseName)
        {
            if (!IsAlbedoTexture(textureName)) return false;
            string t = textureName.ToLowerInvariant();
            string b = baseName.ToLowerInvariant();
            if (t == b) return true;
            if (t == b + "_a" || t == b + "_d" || t == b + "_alb" || t == b + "_albedo" || t == b + "_basecolor" || t == b + "_as") return true;
            // BLD_Cabins -> BLD_Cabins1_A
            for (int n = 1; n <= 4; n++)
            {
                string numbered = b + n.ToString();
                if (t == numbered || t == numbered + "_a" || t == numbered + "_d" || t == numbered + "_as" || t == numbered + "_albedo" || t == numbered + "_basecolor") return true;
            }
            return false;
        }
        private static Texture2D FindAlbedoTexture(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return null;
            if (!AssetDatabase.IsValidFolder(FloodedTextures)) return null;
            List<string> names = new List<string>();
            names.Add(materialName);
            string stripped = materialName;
            string[] suffixes = { "_LOD", "_Mossy", "_Rusty", "_Dirty", "_Clean", "_New", "_Old" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (stripped.EndsWith(suffixes[i]))
                {
                    stripped = stripped.Substring(0, stripped.Length - suffixes[i].Length);
                    names.Add(stripped);
                }
            }
            string trimmed = CleanName(stripped);
            if (!string.IsNullOrEmpty(trimmed) && !names.Contains(trimmed)) names.Add(trimmed);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { FloodedTextures });
            for (int n = 0; n < names.Count; n++)
            {
                string baseName = names[n];
                if (string.IsNullOrEmpty(baseName) || baseName.Length < 3) continue;
                for (int g = 0; g < guids.Length; g++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                    string file = Path.GetFileNameWithoutExtension(path);
                    if (!IsAlbedoFor(file, baseName)) continue;
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null) return texture;
                }
            }
            return null;
        }
        private static bool IsBlankStandard(Material material)
        {
            if (material == null || material.shader == null) return false;
            if (material.shader.name != "Standard") return false;
            if (!material.HasProperty("_MainTex") && !material.HasProperty("_BaseMap")) return false;
            Texture tex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            if (tex == null && material.HasProperty("_BaseMap")) tex = material.GetTexture("_BaseMap");
            if (tex != null) return false;
            Color color = material.HasProperty("_Color") ? material.color : Color.white;
            return color.r > 0.85f && color.g > 0.85f && color.b > 0.85f;
        }
        private static void SetAlbedo(Material material, Texture2D texture)
        {
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        }
        private static int FixFloodedGroundsAlbedo()
        {
            int count = 0;
            string[] roots = { "Assets/a.last/Flooded_Grounds", "Assets" };
            HashSet<string> done = new HashSet<string>();
            for (int r = 0; r < roots.Length; r++)
            {
                if (!AssetDatabase.IsValidFolder(roots[r])) continue;
                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { roots[r] });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!done.Add(path)) continue;
                    if (path.StartsWith("Assets/Photon") || path.StartsWith("Assets/TextMesh Pro")) continue;
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null) continue;
                    bool flooded = path.Contains("Flooded_Grounds");
                    bool blank = IsBlankStandard(material);
                    if (!flooded && !blank) continue;
                    Texture current = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                    if (current == null && material.HasProperty("_BaseMap")) current = material.GetTexture("_BaseMap");
                    if (current != null) continue;
                    Texture2D albedo = FindAlbedoTexture(Path.GetFileNameWithoutExtension(path));
                    if (albedo == null) continue;
                    SetAlbedo(material, albedo);
                    if (blank && material.HasProperty("_Color")) material.color = Color.white;
                    EditorUtility.SetDirty(material);
                    count++;
                }
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
            if (material == null || string.IsNullOrEmpty(name)) return;
            if (!material.HasProperty("_MainTex") && !material.HasProperty("_BaseMap")) return;
            Texture2D albedo = FindAlbedoTexture(name);
            if (albedo != null)
            {
                SetAlbedo(material, albedo);
                return;
            }
            string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D", new[] { "Assets/a.last" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!IsAlbedoTexture(Path.GetFileNameWithoutExtension(path))) continue;
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    SetAlbedo(material, texture);
                    return;
                }
            }
        }
    }
}
