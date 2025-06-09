using UnityEngine;
using UdonSharp;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using VRC.Udon;
using UnityEditor;
#endif

#if UNITY_EDITOR
[IgnoreBuild]
public class UdonOverrideReverter : MonoBehaviour
{
    [SerializeField]
    private bool enable = true;
    [SerializeField]
    private bool includeChildren;

    public static List<UdonSharpBehaviour> USList = new List<UdonSharpBehaviour>();

    public static void RevertVariable()
    {
        for (int i = 0; i < USList.Count; i++)
        {
            if (PrefabUtility.GetOutermostPrefabInstanceRoot(USList[i]) != PrefabUtility.GetNearestPrefabInstanceRoot(USList[i]))
            {
                SerializedObject seri = new SerializedObject(USList[i]);
                seri.Update();
                var iter = seri.GetIterator();
                if (iter.NextVisible(true))
                {
                    do
                    {
                        if (iter.prefabOverride)
                        {
                            PrefabUtility.RevertPropertyOverride(iter, InteractionMode.AutomatedAction);
                            EditorUtility.DisplayProgressBar($"UdonOverrideReverter Process({i}/{USList.Count}) :{USList[i].name}", iter.displayName, (float)i / USList.Count);
                        }
                    } while (iter.NextVisible(false));
                }
            }
        }
        EditorUtility.ClearProgressBar();
    }

    public void PreSaveExecute()
    {
        if (!enable)
            return;

        if (includeChildren)
        {
            foreach (var udonBehaviour in GetComponentsInChildren<UdonSharpBehaviour>(true))
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(udonBehaviour))
                    continue;
                if (!USList.Contains(udonBehaviour))
                {
                    USList.Add(udonBehaviour);
                }
            }
        }
        else
        {
            if (TryGetComponent<UdonSharpBehaviour>(out var udonBehaviour))
            {
                if (!USList.Contains(udonBehaviour))
                {
                    USList.Add(udonBehaviour);
                }
            }
        }
    }
}
[CustomEditor(typeof(UdonOverrideReverter))]
public class UdonOverrideReverterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.TextArea(
        "UdonOverrideReverterは、シーン上に配置された\n" +
        "UdonSharpBehaviourを含むGameObjectのために使用します。\n\n" +
        "このスクリプトをアタッチすることで、\n" +
        "シーン上でのPrefabに対する値のオーバーライドを\n" +
        "Prefab内で指定されている値に自動的に戻すことが可能です。");
    }
}
[InitializeOnLoad]
public class UdonOverrideReverterEvent
{
    static UdonOverrideReverterEvent()
    {
        RegisterDelegate();
    }

    private static void RegisterDelegate()
    {
        EditorSceneManager.sceneSaving += (Scene scene, string path) =>
        {
            UdonOverrideReverter.USList.Clear();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<UdonOverrideReverter>(true))
                {
                    component.PreSaveExecute();
                }
            }
            UdonOverrideReverter.RevertVariable();
            UdonOverrideReverter.USList.Clear();
        };
    }
}
#endif