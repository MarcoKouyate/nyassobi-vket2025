using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VketTools.Utilities
{
    public static class ReferenceObjectsUtility
    {
        [MenuItem("VketTools/Utilities/HideReferenceObjects", false, 800)]
        public static void HideReferenceObjects()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (rootGameObject.name == "ReferenceObjects")
                {
                    rootGameObject.hideFlags = HideFlags.HideInHierarchy;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("VketTools/Utilities/ShowReferenceObjects", false, 800)]
        public static void ShowReferenceObjects()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (rootGameObject.name == "ReferenceObjects")
                {
                    rootGameObject.hideFlags = HideFlags.None;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
