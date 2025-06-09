using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UdonSharp;
#endif

namespace Vket.EssentialResources
{
    /// <summary>
    /// 指定したProcessSceneMarkerTagのUnityEngine.Objectをビルド前にアタッチする
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ProcessSceneMarkerAttachAttribute : PropertyAttribute
#if UNITY_EDITOR
        , IProcessSceneWithReport
#endif
    {
        public string Tag { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag">アタッチ対象のProcessSceneMarker</param>
        public ProcessSceneMarkerAttachAttribute(string tag)
        {
            Tag = tag;
        }
        
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ProcessSceneMarkerAttachAttribute()
        {
            Tag = "None";
        }

#if UNITY_EDITOR
        
        public int callbackOrder => -1000;

        [MenuItem("Tools/Hikky/ManualFindAttachProcessSceneMarker")]
        public static void ProcessSceneMarkerAttributeExecute()
        {
            // シーン上のProcessSceneMarkerAttachを実行
            ProcessSceneMarkerFindAttach.ProcessSceneMarkerExecute();
            // AttributeのProcessSceneMarkerAttach実行
            Process(SceneManager.GetActiveScene());
        }
        
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Process(scene);
        }

        private static void Process(Scene scene)
        {
            // MarkerTagをキーとする対象のUdonSharpBehaviourとFieldInfoのTupleリスト
            var dictionary = new Dictionary<string, List<ValueTuple<UdonSharpBehaviour, FieldInfo>>>();
            Debug.Log("ProcessSceneMarkerAttributeAttach:Process");
            EditorUtility.DisplayProgressBar("ProcessSceneMarkerAttributeAttach", "Process...", 0);
            
            var rootGameObjects = scene.GetRootGameObjects();

            // シーンのオブジェクトからProcessSceneMarkerAttributeAttachな変数(Field)をすべて検索
            foreach (var obj in rootGameObjects)
            {
                // シーンのUdonSharpBehaviourを全て走査
                foreach (var udonSharpBehaviour in obj.GetComponentsInChildren<UdonSharpBehaviour>(true))
                {
                    if (udonSharpBehaviour == null)
                        continue;

                    var targetFields = new List<FieldInfo>();
                    var target = udonSharpBehaviour.GetType();
                    // 子クラスのフィールドもすべて取得
                    while (target != null && target != typeof(UdonSharpBehaviour))
                    {
                        targetFields.AddRange(target.GetFields(BindingFlags.Instance | BindingFlags.Static |
                                                               BindingFlags.Public | BindingFlags.NonPublic));
                        target = target.BaseType;
                    }
                    
                    // Fieldを全て走査
                    foreach (var field in targetFields.Distinct())
                    {
                        // SceneSingletonAttribute属性の場合は辞書に登録
                        if (GetCustomAttribute(field, typeof(ProcessSceneMarkerAttachAttribute)) is ProcessSceneMarkerAttachAttribute
                            attribute)
                        {
                            EditorUtility.DisplayProgressBar("ProcessSceneMarkerAttributeAttach",
                                "Search:(" + field.FieldType + ")" + field.Name, 0);

                            if (!dictionary.ContainsKey(attribute.Tag))
                            {
                                dictionary.Add(attribute.Tag, new List<(UdonSharpBehaviour, FieldInfo)>());
                            }

                            dictionary[attribute.Tag].Add((udonSharpBehaviour, field));
                        }
                    }
                }
            }

            int count = 0;
            
            foreach (string markerTag in dictionary.Keys)
            {
                // MarkerTagを検索
                var markers = ProcessSceneMarkerFindAttach.FindProcessSceneMarkerComponents(scene, markerTag);

                if (markers == null || markers.Length == 0)
                {
                    EditorUtility.DisplayDialog("ProcessSceneMarkerAttributeAttach", "Not Found ProcessSceneMarkerTag '" + markerTag + "' In Scene",
                        "OK");
                    continue;
                }

                foreach (var tuple in dictionary[markerTag])
                {
                    ProcessSceneMarkerFindAttach.AttachProcessSceneMarkerComponent(markerTag, markers, tuple.Item1, tuple.Item2);
                    count++;
                }
            }

            Debug.Log($"Execute methods in ProcessSceneMarkerAttributeAttach. ({count})");
            Debug.Log("ProcessSceneMarkerAttributeAttach:End");
            EditorUtility.ClearProgressBar();
        }
        
#endif
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ProcessSceneMarkerAttachAttribute))]
    public class ProcessSceneMarkerAttachDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            Rect temp = position;
            var content = new GUIContent("ProcessSceneAttach:");
            EditorGUI.LabelField(position, content);
            var labelWidth = GUI.skin.label.CalcSize(content).x;
            position.x += labelWidth;
            position.width -= labelWidth;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndDisabledGroup();
            position.x = temp.x;
            position.width = temp.width;
            position.y += EditorGUIUtility.singleLineHeight;

            var processSceneMarkerAttribute = attribute as ProcessSceneMarkerAttachAttribute;
            EditorGUI.indentLevel++;
            EditorGUI.LabelField(position, $"->Tag:{processSceneMarkerAttribute.Tag}");
            EditorGUI.indentLevel--;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2;
    }
#endif
}