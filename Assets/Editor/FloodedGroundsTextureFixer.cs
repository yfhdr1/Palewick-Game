using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Palewick.EditorTools
{
    /// <summary>
    /// Restores the Flooded_Grounds pack to its original Built-in Render Pipeline
    /// shaders. This intentionally does not convert anything to URP/Lit.
    /// </summary>
    public static class FloodedGroundsTextureFixer
    {
        private const string PackFolder = "Assets/a.last/Flooded_Grounds";
        private const string MaterialsFolder = PackFolder + "/Content/Materials";
        private const string TexturesFolder = PackFolder + "/Content/Textures";

        private const string StandardShader = "Standard";
        private const string TopBlendShader = "Flooded_Grounds/PBR_TopBlend";
        private const string WaterShader = "Flooded_Grounds/PBR_Water";
        private const string SkyShader = "Flooded_Grounds/Skybox_Rotating";
        private const string TriplanarShader = "Flooded_Grounds/Triplanar_BumpSpec";

        private static readonly HashSet<string> TopBlendMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BLD_Bridge1",
            "BLD_Cabins_Mossy",
            "NAT_CobbleRocks1",
            "NAT_Rocks1",
            "PROP_Car1",
            "PROP_Car1_Rusty",
            "PROP_FlyingSaucer_Mossy",
            "PROP_Ship1"
        };

        private static readonly string[] AlbedoSlots =
        {
            "_MainTex", "_BaseMap", "_Albedo", "_AlbedoMap", "_BaseColorMap", "_Diffuse"
        };

        private static readonly string[] NormalSlots =
        {
            "_BumpMap", "_BumpMap1", "_NormalMap", "_Normal", "_NormalTex"
        };

        private static readonly string[] MaskSlots =
        {
            "_MetallicGlossMap", "_Spc", "_SpecGlossMap", "_MaskMap", "_MetallicMap"
        };

        private static readonly string[] OcclusionSlots =
        {
            "_OcclusionMap", "_AO", "_AOMap", "_Occlusion"
        };

        private static readonly string[] EmissionSlots =
        {
            "_EmissionMap", "_EmissiveMap"
        };

        private static Dictionary<string, Texture2D> textureCache;

        /// <summary>Restores original shaders and texture bindings without showing UI.</summary>
        public static MaterialRestoreResult RestoreOriginalMaterials()
        {
            MaterialRestoreResult result = new MaterialRestoreResult();
            textureCache = null;

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                result.error = "Flooded_Grounds materials folder was not found.";
                return result;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
            Array.Sort(guids, StringComparer.Ordinal);
            result.scanned = guids.Length;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                RestoreMaterial(material, ref result);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static void RestoreMaterial(Material material, ref MaterialRestoreResult result)
        {
            TextureBinding binding = CaptureTextures(material);
            Shader target = FindOriginalShader(material.name);
            if (target == null)
            {
                result.missingShaders++;
                return;
            }

            bool changed = material.shader != target;
            if (changed)
            {
                material.shader = target;
                result.shadersRestored++;
            }

            int rebound = 0;
            rebound += SetTexture(material, "_MainTex", binding.albedo, binding.scale, binding.offset);
            rebound += SetTexture(material, "_BumpMap", binding.normal, binding.scale, binding.offset);

            if (target.name == TopBlendShader)
            {
                rebound += SetTexture(material, "_Spc", binding.mask, binding.scale, binding.offset);
                rebound += SetTexture(material, "_AO", binding.occlusion, binding.scale, binding.offset);
            }
            else if (target.name == TriplanarShader)
            {
                rebound += SetTexture(material, "_BumpMap1", binding.normal, binding.scale, binding.offset);
            }
            else
            {
                rebound += SetTexture(material, "_MetallicGlossMap", binding.mask, binding.scale, binding.offset);
                rebound += SetTexture(material, "_OcclusionMap", binding.occlusion, binding.scale, binding.offset);
                rebound += SetTexture(material, "_EmissionMap", binding.emission, binding.scale, binding.offset);
            }

            RestoreColor(material, binding.color);
            RestoreKeywords(material, binding);

            // Keyword cleanup and color restoration may also change serialized state.
            EditorUtility.SetDirty(material);
            result.textureBindings += rebound;
        }

        private static TextureBinding CaptureTextures(Material material)
        {
            TextureBinding binding = new TextureBinding();
            string albedoSlot;
            binding.albedo = GetFirstTexture(material, AlbedoSlots, out albedoSlot);
            binding.normal = GetFirstTexture(material, NormalSlots, out _);
            binding.mask = GetFirstTexture(material, MaskSlots, out _);
            binding.occlusion = GetFirstTexture(material, OcclusionSlots, out _);
            binding.emission = GetFirstTexture(material, EmissionSlots, out _);
            binding.scale = Vector2.one;
            binding.offset = Vector2.zero;

            if (!string.IsNullOrEmpty(albedoSlot))
            {
                binding.scale = material.GetTextureScale(albedoSlot);
                binding.offset = material.GetTextureOffset(albedoSlot);
            }

            if (material.HasProperty("_BaseColor"))
            {
                binding.color = material.GetColor("_BaseColor");
            }
            else if (material.HasProperty("_Color"))
            {
                binding.color = material.GetColor("_Color");
            }
            else
            {
                binding.color = Color.white;
            }

            string baseName = StripVariantSuffixes(material.name);
            if (binding.albedo == null)
            {
                binding.albedo = FindTexture(baseName, TextureKind.Albedo);
            }
            if (binding.normal == null)
            {
                binding.normal = FindTexture(baseName, TextureKind.Normal);
            }
            if (binding.mask == null)
            {
                binding.mask = FindTexture(baseName, TextureKind.Mask);
            }
            if (binding.occlusion == null)
            {
                binding.occlusion = FindTexture(baseName, TextureKind.Occlusion);
            }
            if (binding.emission == null)
            {
                binding.emission = FindTexture(baseName, TextureKind.Emission);
            }

            return binding;
        }

        private static Shader FindOriginalShader(string materialName)
        {
            string shaderName;
            if (materialName.Equals("BGR_Sky1", StringComparison.OrdinalIgnoreCase))
            {
                shaderName = SkyShader;
            }
            else if (materialName.Equals("BGR_Water", StringComparison.OrdinalIgnoreCase))
            {
                shaderName = WaterShader;
            }
            else if (materialName.Equals("NAT_Bush1", StringComparison.OrdinalIgnoreCase))
            {
                shaderName = TriplanarShader;
            }
            else if (TopBlendMaterials.Contains(materialName))
            {
                shaderName = TopBlendShader;
            }
            else if (materialName.Equals("ATM_DustParticle", StringComparison.OrdinalIgnoreCase) ||
                     materialName.Equals("ATM_HaloRing", StringComparison.OrdinalIgnoreCase))
            {
                return FindFirstShader(
                    "Legacy Shaders/Particles/Additive",
                    "Particles/Additive",
                    StandardShader);
            }
            else if (materialName.Equals("ATM_Leaf1", StringComparison.OrdinalIgnoreCase))
            {
                return FindFirstShader(
                    "Legacy Shaders/Particles/Alpha Blended",
                    "Particles/Alpha Blended",
                    StandardShader);
            }
            else
            {
                shaderName = StandardShader;
            }

            Shader shader = Shader.Find(shaderName);
            return shader != null ? shader : Shader.Find(StandardShader);
        }

        private static Shader FindFirstShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null)
                {
                    return shader;
                }
            }
            return null;
        }

        private static int SetTexture(
            Material material,
            string property,
            Texture texture,
            Vector2 scale,
            Vector2 offset)
        {
            if (texture == null || !material.HasProperty(property))
            {
                return 0;
            }

            bool changed = material.GetTexture(property) != texture ||
                material.GetTextureScale(property) != scale ||
                material.GetTextureOffset(property) != offset;
            material.SetTexture(property, texture);
            material.SetTextureScale(property, scale);
            material.SetTextureOffset(property, offset);
            return changed ? 1 : 0;
        }

        private static void RestoreColor(Material material, Color color)
        {
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void RestoreKeywords(Material material, TextureBinding binding)
        {
            // Remove keywords left by the incorrect URP conversion.
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_METALLICSPECGLOSSMAP");

            SetKeyword(material, "_NORMALMAP", binding.normal != null);
            SetKeyword(material, "_METALLICGLOSSMAP", binding.mask != null);
            SetKeyword(material, "_EMISSION", binding.emission != null);
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private static Texture GetFirstTexture(Material material, string[] properties, out string foundProperty)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (!material.HasProperty(properties[i]))
                {
                    continue;
                }
                Texture texture = material.GetTexture(properties[i]);
                if (texture != null)
                {
                    foundProperty = properties[i];
                    return texture;
                }
            }

            foundProperty = null;
            return null;
        }

        private static Texture2D FindTexture(string materialBaseName, TextureKind kind)
        {
            BuildTextureCache();
            if (textureCache == null || textureCache.Count == 0)
            {
                return null;
            }

            string[] suffixes = GetSuffixes(kind);
            List<string> bases = new List<string>
            {
                materialBaseName,
                materialBaseName + "1",
                materialBaseName + "2",
                materialBaseName + "3",
                materialBaseName.Replace("RugsPaintings", "RugsPaintigs") + "1"
            };

            for (int b = 0; b < bases.Count; b++)
            {
                string baseKey = bases[b].ToLowerInvariant();
                for (int s = 0; s < suffixes.Length; s++)
                {
                    Texture2D texture;
                    if (textureCache.TryGetValue(baseKey + suffixes[s], out texture))
                    {
                        return texture;
                    }
                }
            }
            return null;
        }

        private static string[] GetSuffixes(TextureKind kind)
        {
            switch (kind)
            {
                case TextureKind.Albedo:
                    return new[] { "_a", "_as", "_d", "_albedo", "_basecolor", string.Empty };
                case TextureKind.Normal:
                    return new[] { "_n", "_normal", "_nrm" };
                case TextureKind.Mask:
                    return new[] { "_ms", "_m", "_metallic", "_metallicsmoothness" };
                case TextureKind.Occlusion:
                    return new[] { "_ao", "_occlusion" };
                case TextureKind.Emission:
                    return new[] { "_e", "_emission", "_emissive" };
                default:
                    return Array.Empty<string>();
            }
        }

        private static void BuildTextureCache()
        {
            if (textureCache != null)
            {
                return;
            }

            textureCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            if (!AssetDatabase.IsValidFolder(TexturesFolder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder });
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

        private static string StripVariantSuffixes(string materialName)
        {
            string value = materialName;
            string[] suffixes = { "_LOD", "_Mossy", "_Rusty", "_Dirty", "_Wet", "_Alt", "_Fix" };
            bool removed;
            do
            {
                removed = false;
                for (int i = 0; i < suffixes.Length; i++)
                {
                    if (value.EndsWith(suffixes[i], StringComparison.OrdinalIgnoreCase))
                    {
                        value = value.Substring(0, value.Length - suffixes[i].Length);
                        removed = true;
                        break;
                    }
                }
            }
            while (removed);
            return value;
        }

        private enum TextureKind
        {
            Albedo,
            Normal,
            Mask,
            Occlusion,
            Emission
        }

        private struct TextureBinding
        {
            public Texture albedo;
            public Texture normal;
            public Texture mask;
            public Texture occlusion;
            public Texture emission;
            public Vector2 scale;
            public Vector2 offset;
            public Color color;
        }
    }

    public struct MaterialRestoreResult
    {
        public int scanned;
        public int shadersRestored;
        public int textureBindings;
        public int missingShaders;
        public string error;

        public bool Succeeded
        {
            get { return string.IsNullOrEmpty(error) && missingShaders == 0; }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(error))
            {
                return "Materials: FAILED - " + error;
            }
            return "Materials: scanned=" + scanned +
                ", original shaders restored=" + shadersRestored +
                ", texture slots repaired=" + textureBindings +
                ", missing shaders=" + missingShaders + ".";
        }
    }
}
