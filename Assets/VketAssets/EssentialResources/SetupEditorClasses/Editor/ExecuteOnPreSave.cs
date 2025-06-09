#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExecuteOnPreSave
{
    [InitializeOnLoadMethod]
    private static void RegisterDelegate()
    {
        EditorSceneManager.sceneSaving += (Scene scene, string path) =>
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<IPreSaveExecute>(true))
                {
                    try
                    {
                        component.PreSaveExecute();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e);
                    }
                    count++;
                }
            }

            if (count > 0)
                Debug.Log($"Execute methods in pre saving. ({count})");
        };
    }
}
#endif
