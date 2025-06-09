using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Vket.EssentialResources.Attribute 
{
    /// <summary>
    /// ビルド時に対応するタイプのComponentを自動的にアタッチします。
    /// Inspectorでの編集不可能状態で表示することができます。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SelfComponentAttribute : PropertyAttribute
#if UNITY_EDITOR
        , IProcessSceneWithReport
#endif
    {
#if UNITY_EDITOR
        
        // IgnoreBuildよりも先に処理する
        public int callbackOrder => -100;
        
        [MenuItem("HIKKY/SelfComponentAttach/AttributeExecute")]
        public static void SelfComponentAttachAttributeExecute()
        {
            Process(SceneManager.GetActiveScene());
        }
        
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Process(scene);
        }

        private static void Process(Scene scene)
        {
            Debug.Log("SelfComponent:Process");
            EditorUtility.DisplayProgressBar("SelfComponent", "Process...", 0);

            var rootGameObjects = scene.GetRootGameObjects();
            
            // シーンのオブジェクトからSelfComponentAttributeな変数(Field)をすべて検索
            foreach (var obj in rootGameObjects)
            {
                // シーンのMonoBehaviourを全て走査
                foreach (var monoBehaviour in obj.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (monoBehaviour == null) 
                        continue;
                   
                    var targetFields = new List<FieldInfo>();
                    var target = monoBehaviour.GetType();
                    // 子クラスのフィールドもすべて取得
                    while (target != null && target != typeof(UdonSharpBehaviour) && target != typeof(MonoBehaviour))
                    {
                        targetFields.AddRange(target.GetFields(BindingFlags.Instance | BindingFlags.Static |
                                                               BindingFlags.Public | BindingFlags.NonPublic));
                        target = target.BaseType;
                    }
                    
                    // Fieldを全て走査
                    foreach (var field in targetFields.Distinct())
                    {
                        // SelfComponentAttribute属性の場合は辞書に登録
                        if (GetCustomAttribute(field, typeof(SelfComponentAttribute)) is SelfComponentAttribute)
                        {
                            EditorUtility.DisplayProgressBar("SelfComponent", "Search:(" + field.FieldType + ")" + field.Name, 0);
                            var type = field.FieldType;
                            field.SetValue(monoBehaviour, monoBehaviour.GetComponent(type));
                        }
                    }
                }
            }
            Debug.Log("SelfComponent:End");
            EditorUtility.ClearProgressBar();
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SelfComponentAttribute))]
    public class SelfComponentDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var content = new GUIContent("SelfComponent:");
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