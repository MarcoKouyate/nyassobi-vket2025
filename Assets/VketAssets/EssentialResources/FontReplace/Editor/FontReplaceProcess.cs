#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine;

public class FontReplaceProcess : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    List<TextMeshProUGUI> tmps = null;
    FontReplace fontReplace = null;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        tmps = new List<TextMeshProUGUI>();
        fontReplace = null;

        foreach (var item in scene.GetRootGameObjects())
        {
            if (fontReplace == null)
            {
                fontReplace=item.GetComponentInChildren<FontReplace>(true);
            }
            tmps.AddRange(item.GetComponentsInChildren<TextMeshProUGUI>(true));
        }
        if (fontReplace)
        {
            if (fontReplace._Empty==null)
            {
                Debug.LogError("未填写任何字体文件在FontReplace._Empty");
                return;
            }
            if (fontReplace.enableStandaloneReplace && (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows))
            {
                Replace();
            }
            else if (fontReplace.enableAndroidReplace && (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android))
            {
                Replace();
            }
        }
        Object.DestroyImmediate(fontReplace);
    }
    void Replace()
    {
        foreach (var item in tmps)
        {
            if (fontReplace.thinkReplaceFont.Contains(item.font)) {
                item.font = fontReplace._Empty;
                if (item.gameObject.activeInHierarchy)
                    item.ClearMesh();
                item.UpdateFontAsset();
            }
        }
    }
}
#endif