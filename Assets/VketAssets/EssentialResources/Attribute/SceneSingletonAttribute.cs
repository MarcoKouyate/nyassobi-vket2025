
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vket.EssentialResources.Attribute { 
    
    /// <summary>
    /// 在IProcessSceneWithReport自动寻找场景中对应类型的UdonSharpBehaviour
    /// IProcessSceneWithReport のシーン内の対応するタイプの UdonSharpBehaviour を自動的に検索します。
    /// Inspectorでの編集不可能状態で表示することができます。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneSingletonAttribute : PropertyAttribute
#if UNITY_EDITOR
        , IProcessSceneWithReport
#endif
    {
        // IgnoreBuildよりも先に処理する
        public int callbackOrder => -100;

#if UNITY_EDITOR
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Dictionary<Type, List<(UdonSharpBehaviour, FieldInfo)>> mapperTable = new Dictionary<Type, List<(UdonSharpBehaviour, FieldInfo)>>();
            Debug.Log("SceneSingleton:Process");
            EditorUtility.DisplayProgressBar("SceneSingleton", "Process...", 0);
            var rootObjs = scene.GetRootGameObjects();
            foreach (var obj in rootObjs) {
                foreach (var mono in obj.GetComponentsInChildren<UdonSharpBehaviour>(true))
                {
                    if (mono == null) 
                        continue;
                    
                    var targetFields = new List<FieldInfo>();
                    var target = mono.GetType();
                    // 子クラスのフィールドもすべて取得
                    while (target != null && target != typeof(UdonSharpBehaviour) && target != typeof(MonoBehaviour))
                    {
                        targetFields.AddRange(target.GetFields(BindingFlags.Instance | BindingFlags.Static |
                                                               BindingFlags.Public | BindingFlags.NonPublic));
                        target = target.BaseType;
                    }
                    
                    foreach (FieldInfo field in targetFields.Distinct())
                    {
                        SceneSingletonAttribute sceneSingAttribute = GetCustomAttribute(field, typeof(SceneSingletonAttribute)) as SceneSingletonAttribute;
                        if (sceneSingAttribute != null)
                        {
                            EditorUtility.DisplayProgressBar("SceneSingleton", "Search:(" + field.FieldType + ")" + field.Name, 0);
                            if (!mapperTable.ContainsKey(field.FieldType))
                            {
                                mapperTable.Add(field.FieldType, new List<(UdonSharpBehaviour, FieldInfo)>());
                            }
                            mapperTable[field.FieldType].Add((mono, field));
                        }
                    }
                }
            }
            foreach (Type keyType in mapperTable.Keys)
            {
                UnityEngine.Object targetSingletonObject = null;
                foreach (var item in rootObjs)
                {
                    targetSingletonObject= item.GetComponentInChildren(keyType,true);
                    if (targetSingletonObject!=null)
                    {
                        break;
                    }
                }
                if (targetSingletonObject)
                {
                    foreach (var refer in mapperTable[keyType])
                    {
                        refer.Item2.SetValue(refer.Item1, targetSingletonObject);
                        EditorUtility.DisplayProgressBar("SceneSingleton", "Setup:(" + refer.Item2.FieldType + ")" + refer.Item1, 0);
                    }
                }
                else
                {
#if VKET_TOOLS || VIORAMA_COMMON
                    Debug.Log("SceneSingleton:Not Found Script '" + keyType + "' In Scene");
#else
                    EditorUtility.DisplayDialog("SceneSingleton", "Not Found Script '" + keyType + "' In Scene", "OK");
#endif
                }
            }
            Debug.Log("SceneSingleton:End");
            EditorUtility.ClearProgressBar();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SceneSingletonAttribute))]
    public class SceneSingletonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var content = new GUIContent("SceneSingleton:");
            EditorGUI.LabelField(position, content);
            var labelWidth = GUI.skin.label.CalcSize(content).x;
            position.x += labelWidth;
            position.width -= labelWidth;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndDisabledGroup();
        }
    }
#endif
}