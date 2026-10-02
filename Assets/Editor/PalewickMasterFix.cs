using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Palewick.EditorTools
{
    /// <summary>Single safe entry point for repairing and cleaning the project.</summary>
    public static class PalewickMasterFix
    {
        [MenuItem("Palewick/Fix Everything (One Click)", false, 0)]
        public static void FixEverything()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Palewick - Fix Everything",
                    "Exit Play Mode before running the project repair.",
                    "OK");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog(
                    "Palewick - Fix Everything",
                    "Wait for Unity to finish compiling/importing, then run the command again.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            StringBuilder report = new StringBuilder();
            List<string> failures = new List<string>();

            try
            {
                EditorUtility.DisplayProgressBar(
                    "Palewick - Fix Everything",
                    "Restoring the Built-in render pipeline...",
                    0.05f);
                int pipelineOverrides = RestoreBuiltInRenderPipeline();
                report.AppendLine("Render pipeline: Built-in restored; overrides removed=" + pipelineOverrides + ".");

                EditorUtility.DisplayProgressBar(
                    "Palewick - Fix Everything",
                    "Restoring original shaders and textures...",
                    0.15f);
                MaterialRestoreResult materials = FloodedGroundsTextureFixer.RestoreOriginalMaterials();
                report.AppendLine(materials.ToString());
                if (!materials.Succeeded)
                {
                    failures.Add("material restoration");
                }

                string[] scenePaths = PalewickMissingScriptsCleaner.ScenePaths;
                for (int i = 0; i < scenePaths.Length; i++)
                {
                    string path = scenePaths[i];
                    float progress = 0.25f + (0.65f * i / Mathf.Max(1, scenePaths.Length));
                    EditorUtility.DisplayProgressBar(
                        "Palewick - Fix Everything",
                        "Cleaning " + Path.GetFileNameWithoutExtension(path) + "...",
                        progress);

                    if (!File.Exists(path))
                    {
                        report.AppendLine(path + ": scene file not found.");
                        failures.Add(path);
                        continue;
                    }

                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    MissingScriptsResult missing = PalewickMissingScriptsCleaner.CleanScene(scene);
                    report.AppendLine(missing.ToString());
                    if (!missing.Succeeded)
                    {
                        failures.Add(scene.name + " missing scripts");
                    }

                    if (path == PwTouchControlsInstaller.LobbyScenePath)
                    {
                        int removed = PwTouchControlsInstaller.RemoveLobbyControls(scene);
                        report.AppendLine("Scene_Lobby: obsolete CONTROLS UI removed=" + removed + ".");
                    }
                    else if (path == PwTouchControlsInstaller.GameScenePath)
                    {
                        TouchControlsResult controls = PwTouchControlsInstaller.RebuildGameplayControls(scene);
                        report.AppendLine(controls.ToString());
                        if (!controls.Succeeded)
                        {
                            failures.Add("Scene_A touch controls");
                        }
                    }

                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        report.AppendLine(scene.name + ": FAILED to save.");
                        failures.Add(scene.name + " save");
                    }
                }

                EditorUtility.DisplayProgressBar(
                    "Palewick - Fix Everything",
                    "Saving project assets...",
                    0.95f);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception exception)
            {
                failures.Add(exception.GetType().Name);
                report.AppendLine("Unexpected failure: " + exception.Message);
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                try
                {
                    if (previousSetup != null && previousSetup.Length > 0)
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                    }
                }
                catch (Exception exception)
                {
                    failures.Add("scene setup restore");
                    report.AppendLine("Could not restore the previously open scene setup: " + exception.Message);
                    Debug.LogException(exception);
                }
            }

            bool succeeded = failures.Count == 0;
            string heading = succeeded
                ? "All repairs completed and all three scenes were saved."
                : "The repair finished with " + failures.Count + " issue(s). See Console for details.";
            string finalReport = heading + "\n\n" + report;
            if (succeeded)
            {
                Debug.Log("[Palewick] Fix Everything completed.\n" + report);
            }
            else
            {
                Debug.LogError("[Palewick] Fix Everything had issues: " +
                    string.Join(", ", failures) + "\n" + report);
            }

            EditorUtility.DisplayDialog(
                "Palewick - Fix Everything",
                finalReport,
                "OK");
        }

        private static int RestoreBuiltInRenderPipeline()
        {
            int changed = 0;

#pragma warning disable 0618
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                GraphicsSettings.defaultRenderPipeline = null;
                changed++;
            }
#pragma warning restore 0618

            int currentQuality = QualitySettings.GetQualityLevel();
            string[] qualityNames = QualitySettings.names;
            try
            {
                for (int i = 0; i < qualityNames.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    if (QualitySettings.renderPipeline != null)
                    {
                        QualitySettings.renderPipeline = null;
                        changed++;
                    }
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(currentQuality, false);
            }

            return changed;
        }
    }
}
