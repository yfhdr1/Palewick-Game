using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Palewick.EditorTools
{
    /// <summary>Removes broken MonoBehaviour component slots from Palewick scenes.</summary>
    public static class PalewickMissingScriptsCleaner
    {
        public static readonly string[] ScenePaths =
        {
            "Assets/a.loby/Scene_Intro.unity",
            "Assets/a.loby/Scene_Lobby.unity",
            "Assets/a.last/Flooded_Grounds/Scenes/Scene_A.unity"
        };

        /// <summary>Removes all Missing (Mono Script) components in a loaded scene.</summary>
        public static MissingScriptsResult CleanScene(Scene scene)
        {
            MissingScriptsResult result = new MissingScriptsResult
            {
                sceneName = scene.IsValid() ? scene.name : "<invalid>"
            };

            if (!scene.IsValid() || !scene.isLoaded)
            {
                result.error = "Scene is not loaded.";
                return result;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    GameObject gameObject = transforms[i] == null ? null : transforms[i].gameObject;
                    if (gameObject == null)
                    {
                        continue;
                    }

                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                    if (missing <= 0)
                    {
                        continue;
                    }

                    result.objectsCleaned++;
                    try
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                        result.componentsRemoved += missing;
                        EditorUtility.SetDirty(gameObject);
                    }
                    catch (Exception exception)
                    {
                        result.failedObjects++;
                        Debug.LogWarning(
                            "[Palewick] Could not remove a missing script from " +
                            GetHierarchyPath(gameObject.transform) + ": " + exception.Message,
                            gameObject);
                    }
                }
            }

            if (result.componentsRemoved > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            return result;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<destroyed>";
            }

            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }

    public struct MissingScriptsResult
    {
        public string sceneName;
        public int componentsRemoved;
        public int objectsCleaned;
        public int failedObjects;
        public string error;

        public bool Succeeded
        {
            get { return string.IsNullOrEmpty(error) && failedObjects == 0; }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(error))
            {
                return sceneName + " missing scripts: FAILED - " + error;
            }
            return sceneName + " missing scripts: removed=" + componentsRemoved +
                " from " + objectsCleaned + " objects, failures=" + failedObjects + ".";
        }
    }
}
