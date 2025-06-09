using System;

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Object = UnityEngine.Object;
#endif

/// <summary>
/// この属性のついたMonoBehaviourはビルド直前に削除される
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IgnoreBuildAttribute : Attribute
{
    public bool IsApplyPlayMode { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="isApplyPlayMode">PlayMode突入時にIgnoreするか</param>
    public IgnoreBuildAttribute(bool isApplyPlayMode)
    {
        IsApplyPlayMode = isApplyPlayMode;
    }

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public IgnoreBuildAttribute()
    {
        IsApplyPlayMode = true;
    }
}

#if UNITY_EDITOR
/// <summary>
/// ビルド直前にIgnoreBuildAttributeの付いたMonoBehaviourを削除する
/// </summary>
public class IgnoreBuild : IProcessSceneWithReport
{
    /// <summary>
    /// MonoBehaviourへのアクセス処理がある可能性が高いので遅めに処理する
    /// </summary>
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        Debug.Log("IgnoreBuild:Process");
        int removeCount = 0;
        foreach (var rootGameObject in scene.GetRootGameObjects())
        {
            foreach (var mono in rootGameObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mono == null)
                    continue;

                if (System.Attribute.GetCustomAttribute(mono.GetType(), typeof(IgnoreBuildAttribute)) is
                    IgnoreBuildAttribute iba)
                {
                    if (iba.IsApplyPlayMode || !Application.isPlaying)
                    {
                        Object.DestroyImmediate(mono);
                    }

                    removeCount++;
                }
            }
        }

        Debug.Log($"Ignore MonoBehaviour on Build, Count({removeCount})");
        Debug.Log("IgnoreBuild:End");
    }
}
#endif