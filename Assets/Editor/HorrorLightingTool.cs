using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Palewick.EditorTools
{
    public static class HorrorLightingTool
    {
        [MenuItem("Tools/Palewick/Apply Horror Lighting")]
        public static void ApplyHorrorLighting()
        {
            LightingRepairResult result = ApplyHorrorLighting(SceneManager.GetActiveScene());
            if (result.Succeeded)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("Palewick: " + result);
            }
            else
            {
                Debug.LogError("Palewick: " + result);
            }
        }

        public static LightingRepairResult ApplyHorrorLighting(Scene scene)
        {
            LightingRepairResult result = new LightingRepairResult();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                result.error = "Scene_A is not loaded.";
                return result;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.19f, 0.25f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.18f, 0.16f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.075f, 0.065f, 0.06f, 1f);
            RenderSettings.ambientIntensity = 0.82f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.065f, 1f);
            RenderSettings.fogDensity = 0.012f;

            GameObject[] roots = scene.GetRootGameObjects();
            Light primary = null;
            for (int i = 0; i < roots.Length; i++)
            {
                Light[] lights = roots[i].GetComponentsInChildren<Light>(true);
                for (int l = 0; l < lights.Length; l++)
                {
                    Light light = lights[l];
                    if (light == null || light.type != LightType.Directional) continue;
                    if (primary == null) primary = light;
                    light.color = new Color(0.55f, 0.64f, 0.82f, 1f);
                    light.intensity = 0.62f;
                    light.shadows = LightShadows.Soft;
                    result.directionalLights++;
                    EditorUtility.SetDirty(light);
                }
            }

            if (primary == null)
            {
                GameObject lightObject = new GameObject("HorrorMoonlight");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                primary = lightObject.AddComponent<Light>();
                primary.type = LightType.Directional;
                primary.color = new Color(0.55f, 0.64f, 0.82f, 1f);
                primary.intensity = 0.62f;
                primary.shadows = LightShadows.Soft;
                result.directionalLights = 1;
            }

            RenderSettings.sun = primary;
            EditorSceneManager.MarkSceneDirty(scene);
            return result;
        }
    }

    public struct LightingRepairResult
    {
        public int directionalLights;
        public string error;
        public bool Succeeded
        {
            get { return string.IsNullOrEmpty(error); }
        }
        public override string ToString()
        {
            if (!Succeeded) return "Scene_A lighting: FAILED - " + error;
            return "Scene_A lighting: horror color balance restored; directional lights=" + directionalLights + ".";
        }
    }
}
