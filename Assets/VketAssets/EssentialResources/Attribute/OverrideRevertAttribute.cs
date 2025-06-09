using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Vket.EssentialResources.Attribute
{
    /// <summary>
    /// Prefab化した状態でシーンに配置されている場合のみ、シーン保存時にOverrideをRevertしてくれます。
    /// 常にRevertする前提のPrefabであればAssets/VketAssets/Assets/EssentialResources/SetupEditorClasses/UdonOverrideReverter.csの代わりに使用できます。
    /// 
    /// 引数にPrefabの子のUdonSharpBehaviourのOverrideもRevertするかを指定できます。
    ///
    /// 以下のように使用します。
    /// ```
    ///　using VketAssets.Assets.EssentialResources.Attribute;
    /// 
    /// 子を含めない場合
    /// [OverrideRevert]
    /// public class ~ : UdonSharpBehaviour
    /// 
    /// 子を含める場合
    /// [OverrideRevert(true)]
    /// public class ~ : UdonSharpBehaviour
    /// ```
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class OverrideRevertAttribute : System.Attribute
    {
        private bool IncludeChildren { get; }

        public OverrideRevertAttribute(bool includeChildren = false)
        {
            IncludeChildren = includeChildren;
        }

#if UNITY_EDITOR
        /// <summary>
        /// シーン保存時に呼び出す関数を登録
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OverrideRevert()
        {
            EditorSceneManager.sceneSaving += (scene, path) =>
            {
                //Debug.Log("OverrideRevert:Process");
                EditorUtility.DisplayProgressBar("OverrideRevert", "Process...", 0);
                int count = 0;
                
                // シーン上のOverrideRevertAttributeを検索する
                foreach (var obj in scene.GetRootGameObjects())
                {
                    foreach (var mono in obj.GetComponentsInChildren<UdonSharpBehaviour>(true))
                    {
                        if (mono == null) continue;

                        if (GetCustomAttribute(mono.GetType(), typeof(OverrideRevertAttribute)) is OverrideRevertAttribute attribute)
                        {
                            //Debug.Log("Search:(" + mono.gameObject.name + ")" + mono.GetType().Name);
                            EditorUtility.DisplayProgressBar("OverrideRevert", "Search:(" + mono.gameObject.name + ")" + mono.GetType().Name, 0);
                            // OverrideRevertAttributeが見つかったので、Revertを実行する
                            RevertVariable(mono, attribute.IncludeChildren);
                            // 実行回数のインクリメント
                            count++;
                        }
                    }
                }

                if (count > 0)
                    Debug.Log($"Execute methods in override revert. ({count})");
                
                //Debug.Log("OverrideRevert:End");
                EditorUtility.ClearProgressBar();
            };
        }

        /// <summary>
        /// OverrideRevertAttribute属性がついているPrefabに対してRevertを実行する
        /// </summary>
        /// <param name="targetBehaviour">対象のOverrideRevertAttribute属性がついているUdonSharpBehaviour</param>
        /// <param name="includeChildren">子を含むか</param>
        static void RevertVariable(UdonSharpBehaviour targetBehaviour, bool includeChildren)
        {
            List<UdonSharpBehaviour> usList = new List<UdonSharpBehaviour>();
            if (PrefabUtility.IsPartOfPrefabInstance(targetBehaviour))
                usList.Add(targetBehaviour);
            
            if (includeChildren)
            {
                foreach (var udonBehaviour in targetBehaviour.GetComponentsInChildren<UdonSharpBehaviour>(true))
                {
                    if (!PrefabUtility.IsPartOfPrefabInstance(udonBehaviour))
                        continue;
                    
                    if (!usList.Contains(udonBehaviour))
                    {
                        usList.Add(udonBehaviour);
                    }
                }
            }
            
            for (int i = 0; i < usList.Count; i++)
            {
                SerializedObject seri = new SerializedObject(usList[i]);
                seri.Update();
                var iter = seri.GetIterator();
                if (iter.NextVisible(true))
                {
                    do
                    {
                        if (iter.prefabOverride)
                        {
                            PrefabUtility.RevertPropertyOverride(iter, InteractionMode.AutomatedAction);
                            EditorUtility.DisplayProgressBar($"UdonOverrideReverter Process({i}/{usList.Count}) :{usList[i].name}", iter.displayName, (float)i / usList.Count);
                        }
                    } while (iter.NextVisible(false));
                }
            }
        }
#endif
    }
}