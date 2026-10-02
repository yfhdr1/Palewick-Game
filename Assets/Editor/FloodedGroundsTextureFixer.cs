using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    /// <summary>
    /// Upgrades every Flooded_Grounds material to the Universal Render Pipeline shaders
    /// and re-binds the Albedo / Normal / Metallic-Smoothness / AO / Emission textures,
    /// first from the old shader slots and then by the "NAME_A, NAME_N, NAME_MS, NAME_AO"
    /// naming convention used by the pack.
    /// </summary>
    public static class FloodedGroundsTextureFixer
    {
        private const string PackFolder = "Assets/a.last/Flooded_Grounds";
        private const string TexturesFolder = "Assets/a.last/Flooded_Grounds/Content/Textures";
        private const string MaterialsFolder = "Assets/a.last/Flooded_Grounds/Content/Materials";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        private static readonly string[] AlbedoSlots = { "_BaseMap", "_MainTex", "_Albedo", "_AlbedoMap", "_BaseColorMap", "_Diffuse", "_Tex" };
        private static readonly string[] NormalSlots = { "_BumpMap", "_NormalMap", "_Normal", "_BumpMap1", "_NormalTex" };
        private static readonly string[] MaskSlots = { "_MetallicGlossMap", "_SpecGlossMap", "_MS", "_MaskMap", "_MetallicMap" };
        private static readonly string[] OcclusionSlots = { "_OcclusionMap", "_AO", "_AOMap", "_Occlusion" };
        private static readonly string[] EmissionSlots = { "_EmissionMap", "_Emission", "_EmissiveMap" };
        private static Dictionary<string, Texture2D> textureCache;

        [MenuItem("Palewick/Fix Flooded Grounds Textures (URP)")]
        public static void Fix()
        {
            Shader lit = Shader.Find(LitShader);
            if (lit == null)
            {
                EditorUtility.DisplayDialog("Flooded Grounds", "The Universal Render Pipeline shaders were not found.\nMake sure the project uses URP before running this tool.", "OK");
                return;
            }
            textureCache = null;
            List<string> paths = CollectMaterialPaths();
            int upgraded = 0;
            int rebound = 0;
            int skipped = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    EditorUtility.DisplayProgressBar("Flooded Grounds", "Upgrading " + Path.GetFileName(path), (float)i / Mathf.Max(1, paths.Count));
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        continue;
                    }
                    int boundHere;
                    bool changedShader;
                    if (!Upgrade(material, out changedShader, out boundHere))
                    {
                        skipped++;
                        continue;
                    }
                    if (changedShader)
                    {
                        upgraded++;
                    }
                    rebound += boundHere;
                    EditorUtility.SetDirty(material);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            int renderers = RefreshOpenScenes();
            string report =
                "Materials scanned: " + paths.Count +
                "\nShaders upgraded to URP: " + upgraded +
                "\nTexture slots re-bound: " + rebound +
                "\nLeft untouched (skybox / already fine): " + skipped +
                "\nRenderers refreshed in open scenes: " + renderers;
            Debug.Log("[Palewick] Flooded Grounds URP fix done.\n" + report);
            EditorUtility.DisplayDialog("Flooded Grounds", report, "OK");
        }

        [MenuItem("Palewick/Report Flooded Grounds Shaders")]
        public static void Report()
        {
            List<string> paths = CollectMaterialPaths();
            Dictionary<string, int> perShader = new Dictionary<string, int>();
            int missingAlbedo = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
                if (material == null)
                {
                    continue;
                }
                string shaderName = material.shader == null ? "<missing>" : material.shader.name;
                int count;
                perShader.TryGetValue(shaderName, out count);
                perShader[shaderName] = count + 1;
                if (GetFirstTexture(material, AlbedoSlots) == null)
                {
                    missingAlbedo++;
                }
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Flooded Grounds materials: ").Append(paths.Count).Append('\n');
            foreach (KeyValuePair<string, int> pair in perShader)
            {
                sb.Append("  ").Append(pair.Value).Append(" x ").Append(pair.Key).Append('\n');
            }
            sb.Append("Materials without an albedo texture: ").Append(missingAlbedo);
            Debug.Log("[Palewick] " + sb);
            EditorUtility.DisplayDialog("Flooded Grounds", sb.ToString(), "OK");
        }

        private static List<string> CollectMaterialPaths()
        {
            List<string> paths = new List<string>();
            string root = AssetDatabase.IsValidFolder(MaterialsFolder) ? MaterialsFolder : PackFolder;
            if (!AssetDatabase.IsValidFolder(root))
            {
                return paths;
            }
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { root });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                {
                    paths.Add(path);
                }
            }
            paths.Sort();
            return paths;
        }

        private static bool Upgrade(Material material, out bool changedShader, out int rebound)
        {
            changedShader = false;
            rebound = 0;
            string shaderName = material.shader == null ? string.Empty : material.shader.name;
            // Skyboxes keep their own shader, they already render correctly under URP.
            if (shaderName.Contains("Skybox"))
            {
                return false;
            }
            bool isWater = shaderName.Contains("Water") || material.name.Contains("Water") || material.name.Contains("Ocean");
            bool isParticle = shaderName.Contains("Particle") || shaderName.Contains("Additive") || shaderName.Contains("Alpha Blended");
            string targetName = isParticle ? ParticleShader : LitShader;
            Shader target = Shader.Find(targetName);
            if (target == null)
            {
                target = Shader.Find(UnlitShader);
            }
            if (target == null)
            {
                return false;
            }
            Texture albedo = GetFirstTexture(material, AlbedoSlots);
            Texture normal = GetFirstTexture(material, NormalSlots);
            Texture mask = GetFirstTexture(material, MaskSlots);
            Texture occlusion = GetFirstTexture(material, OcclusionSlots);
            Texture emission = GetFirstTexture(material, EmissionSlots);
            Vector2 scale = Vector2.one;
            Vector2 offset = Vector2.zero;
            string mainSlot = FirstExistingSlot(material, AlbedoSlots);
            if (mainSlot != null)
            {
                scale = material.GetTextureScale(mainSlot);
                offset = material.GetTextureOffset(mainSlot);
            }
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            if (material.HasProperty("_BaseColor"))
            {
                color = material.GetColor("_BaseColor");
            }
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : (material.HasProperty("_Smoothness") ? material.GetFloat("_Smoothness") : 0.35f);
            Color emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            float bumpScale = material.HasProperty("_BumpScale") ? material.GetFloat("_BumpScale") : 1f;
            if (material.shader != target)
            {
                material.shader = target;
                changedShader = true;
            }
            string baseName = CleanName(material.name);
            if (albedo == null)
            {
                albedo = FindTexture(baseName, new[] { "_A", "_D", "_AS", "_Albedo", "_BaseColor", string.Empty });
                if (albedo != null)
                {
                    rebound++;
                }
            }
            if (normal == null)
            {
                normal = FindTexture(baseName, new[] { "_N", "_Normal", "_NRM" });
                if (normal != null)
                {
                    rebound++;
                }
            }
            if (mask == null)
            {
                mask = FindTexture(baseName, new[] { "_MS", "_M", "_Metallic", "_MetallicSmoothness" });
                if (mask != null)
                {
                    rebound++;
                }
            }
            if (occlusion == null)
            {
                occlusion = FindTexture(baseName, new[] { "_AO", "_Occlusion" });
                if (occlusion != null)
                {
                    rebound++;
                }
            }
            SetTexture(material, "_BaseMap", albedo, scale, offset);
            SetTexture(material, "_MainTex", albedo, scale, offset);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (isParticle)
            {
                return true;
            }
            SetTexture(material, "_BumpMap", normal, scale, offset);
            SetTexture(material, "_MetallicGlossMap", mask, scale, offset);
            SetTexture(material, "_OcclusionMap", occlusion, scale, offset);
            SetTexture(material, "_EmissionMap", emission, scale, offset);
            SetKeyword(material, "_NORMALMAP", normal != null);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", mask != null);
            SetKeyword(material, "_OCCLUSIONMAP", occlusion != null);
            bool emissive = emission != null || emissionColor.maxColorComponent > 0.01f;
            SetKeyword(material, "_EMISSION", emissive);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emissive ? emissionColor : Color.black);
            }
            material.globalIlluminationFlags = emissive ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if (material.HasProperty("_WorkflowMode"))
            {
                material.SetFloat("_WorkflowMode", 1f);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            }
            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                material.SetFloat("_SmoothnessTextureChannel", 0f);
            }
            if (material.HasProperty("_BumpScale"))
            {
                material.SetFloat("_BumpScale", bumpScale);
            }
            if (material.HasProperty("_OcclusionStrength"))
            {
                material.SetFloat("_OcclusionStrength", occlusion != null ? 1f : 0f);
            }
            SetSurface(material, isWater);
            material.enableInstancing = true;
            return true;
        }

        private static void SetSurface(Material material, bool transparent)
        {
            if (!material.HasProperty("_Surface"))
            {
                return;
            }
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", true);
                SetKeyword(material, "_ALPHAPREMULTIPLY_ON", false);
                material.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                material.SetFloat("_Surface", 0f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", false);
                material.SetOverrideTag("RenderType", "Opaque");
            }
        }

        private static void SetKeyword(Material material, string keyword, bool on)
        {
            if (on)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private static void SetTexture(Material material, string slot, Texture texture, Vector2 scale, Vector2 offset)
        {
            if (texture == null || !material.HasProperty(slot))
            {
                return;
            }
            material.SetTexture(slot, texture);
            material.SetTextureScale(slot, scale);
            material.SetTextureOffset(slot, offset);
        }

        private static string FirstExistingSlot(Material material, string[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (material.HasProperty(slots[i]) && material.GetTexture(slots[i]) != null)
                {
                    return slots[i];
                }
            }
            return null;
        }

        private static Texture GetFirstTexture(Material material, string[] slots)
        {
            string slot = FirstExistingSlot(material, slots);
            return slot == null ? null : material.GetTexture(slot);
        }

        private static string CleanName(string materialName)
        {
            string name = materialName;
            string[] suffixes = { "_LOD", "_Mossy", "_Dirty", "_Wet", "_Dark", "_Instance", " (Instance)", "_Alt", "_Fix" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (name.EndsWith(suffixes[i]))
                {
                    name = name.Substring(0, name.Length - suffixes[i].Length);
                }
            }
            return name.Trim();
        }

        private static void BuildCache()
        {
            if (textureCache != null)
            {
                return;
            }
            textureCache = new Dictionary<string, Texture2D>();
            string folder = AssetDatabase.IsValidFolder(TexturesFolder) ? TexturesFolder : PackFolder;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    continue;
                }
                string key = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (!textureCache.ContainsKey(key))
                {
                    textureCache.Add(key, texture);
                }
            }
        }

        private static Texture2D FindTexture(string baseName, string[] suffixes)
        {
            if (string.IsNullOrEmpty(baseName))
            {
                return null;
            }
            BuildCache();
            string lower = baseName.ToLowerInvariant();
            for (int s = 0; s < suffixes.Length; s++)
            {
                string suffix = suffixes[s].ToLowerInvariant();
                Texture2D direct;
                if (textureCache.TryGetValue(lower + suffix, out direct))
                {
                    return direct;
                }
                // The pack numbers its texture sets: BLD_Cabins -> BLD_Cabins1_A
                for (int n = 1; n <= 4; n++)
                {
                    Texture2D numbered;
                    if (textureCache.TryGetValue(lower + n.ToString() + suffix, out numbered))
                    {
                        return numbered;
                    }
                }
            }
            return null;
        }

        private static int RefreshOpenScenes()
        {
            int touched = 0;
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }
                Material[] materials = renderer.sharedMaterials;
                bool dirty = false;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null || material.shader == null || material.shader.name.Contains("InternalError"))
                    {
                        continue;
                    }
                    string path = AssetDatabase.GetAssetPath(material);
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(PackFolder))
                    {
                        dirty = true;
                    }
                }
                if (dirty)
                {
                    EditorUtility.SetDirty(renderer);
                    touched++;
                }
            }
            if (touched > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
            }
            return touched;
        }
    }
}
