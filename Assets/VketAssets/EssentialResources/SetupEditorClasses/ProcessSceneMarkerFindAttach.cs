using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

using Object = UnityEngine.Object;

namespace Vket.EssentialResources
{
    /// <summary>
    /// PreSaveMarkerがアタッチされたコンポーネントからMarkerTagが一致したものを検索し、
    /// Variableの型と同じコンポーネントを取得してアタッチします。
    /// 
    /// ※※使い方※※
    /// ①アタッチ先のコンポーネントと同じ場所に"PreSaveMarker"コンポーネントをアタッチし、
    /// 　"MarkerTag"に検索対象となる文字列を指定する。
    /// ②UdonBehaviour（U#)と同じ場所にこのScriptをアタッチし、"SearchMarkerTag"に"MarkerTag"と同じ文字列を指定する。
    /// ③"Variable"にU#で宣言したPublic変数名を指定する。
    /// 
    /// 配列型の変数を指定したら、見つけたコンポーネントを全て取得するように変更しました。
    /// また、UdonSharpBehaviour継承クラスの型ならその型のUdonBehaviourのみを取得するように変更しました。(hatsuca 20211217)
    /// </summary>
    [IgnoreBuild]
    public class ProcessSceneMarkerFindAttach : MonoBehaviour
#if UNITY_EDITOR
        , IProcessSceneWithReport
#endif
    {
        [SerializeField] private string _variable;
        [SerializeField] private string _searchTag;
        [SerializeField] private bool _arrayAppendMode;
        [SerializeField] private bool _onlyFindChild;

#if UNITY_EDITOR


        public string SearchTag { get => _searchTag; }
        public int callbackOrder => -1000;

        public bool OnlyFindChild { get => _onlyFindChild; }
        public bool ArrayAppendMode { get => _arrayAppendMode; }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Process(scene);
        }

        static bool _isManual = false;

        /// <summary>
        /// 手動実行
        /// Attribute側から呼ぶ想定
        /// </summary>
        public static void ProcessSceneMarkerExecute()
        {
            Process(SceneManager.GetActiveScene(), true);
        }

        private static void Process(Scene scene, bool isManual = false)
        {
            _isManual = isManual;
            Debug.Log("ProcessSceneMarkerFindAttach:Process");
            EditorUtility.DisplayProgressBar("ProcessSceneMarkerFindAttach", "Processing..", 0);

            int count = 0;

            foreach (var item in scene.GetRootGameObjects())
            {
                foreach (var processSceneMarkerAttach in item.GetComponentsInChildren<ProcessSceneMarkerFindAttach>(true))
                {
                    var targetBehaviours = processSceneMarkerAttach.GetComponents<UdonSharpBehaviour>();
                    foreach (var targetBehaviour in targetBehaviours)
                    {
                        var fieldInfo = targetBehaviour.GetType().GetField(processSceneMarkerAttach._variable,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (fieldInfo == null)
                        {
                            Debug.LogWarning($"{targetBehaviour.gameObject.name}: \"{processSceneMarkerAttach._variable}\" Variable Field not found");
                            continue;
                        }

                        // ★ 根据 _onlyFindChild 切换查找方式
                        ProcessSceneMarker[] markers = processSceneMarkerAttach._onlyFindChild
                            ? FindProcessSceneMarkerComponentsInChildren(processSceneMarkerAttach.transform, processSceneMarkerAttach._searchTag, true)
                            : FindProcessSceneMarkerComponents(scene, processSceneMarkerAttach._searchTag);

                        AttachProcessSceneMarkerComponent(
                            processSceneMarkerAttach._searchTag,
                            markers,
                            targetBehaviour,
                            fieldInfo,
                            processSceneMarkerAttach._arrayAppendMode
                        );
                        count++;
                    }
                }
            }

            Debug.Log($"Execute methods in ProcessSceneMarkerFindAttach. ({count})");
            Debug.Log("ProcessSceneMarkerFindAttach:End");
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// ProcessSceneMarkerの検索
        /// markerTagを指定しない場合はシーン上のマーカーをすべて返す
        /// </summary>
        /// <param name="scene">検索するシーン</param>
        /// <param name="markerTag">タグ指定する場合は指定</param>
        /// <returns>マーカーの配列</returns>
        public static ProcessSceneMarker[] FindProcessSceneMarkerComponents(Scene scene, string markerTag = "NotFindProcessSceneMarkerTag")
        {
            var markers = new List<ProcessSceneMarker>();
            var processSceneMarkers = Resources.FindObjectsOfTypeAll<ProcessSceneMarker>();
            bool notFindTag = markerTag == "NotFindProcessSceneMarkerTag";
            foreach (var processSceneMarker in processSceneMarkers)
            {
                if (processSceneMarker.gameObject.scene == scene && (notFindTag || processSceneMarker.Tag == markerTag))
                    markers.Add(processSceneMarker);
            }

            return markers.ToArray();
        }

        /// <summary>
        /// ProcessSceneMarkerの検索
        /// </summary>
        /// <param name="markerTag">マーカータグ</param>
        /// <param name="scene">対象のシーン</param>
        /// <returns>最初に見つかったマーカーを返し、見つからない場合はnullを返す</returns>
        public static ProcessSceneMarker FindPreSaveMarkerComponent(string markerTag, Scene? scene = null)
        {
            scene = scene ?? SceneManager.GetActiveScene();
            var processSceneMarkers = Resources.FindObjectsOfTypeAll<ProcessSceneMarker>();
            foreach (var processSceneMarker in processSceneMarkers)
            {
                if (processSceneMarker.Tag == markerTag && processSceneMarker.gameObject.scene == scene)
                {
                    return processSceneMarker;
                }
            }
            return null;
        }

        /// <summary>
        /// Childの中からProcessSceneMarkerを探す
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="markerTag"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static ProcessSceneMarker[] FindProcessSceneMarkerComponentsInChildren(Transform parent, string markerTag = "NotFindProcessSceneMarkerTag", bool includeInactive = true)
        {
            // 1. 先拿到 parent 下所有的子物体里附加的 ProcessSceneMarker
            var markers = parent.GetComponentsInChildren<ProcessSceneMarker>(includeInactive);

            // 2. 如果没指定 Tag，就返回全部
            if (markerTag == "NotFindProcessSceneMarkerTag")
            {
                return markers;
            }

            // 3. 如果指定了Tag，只留下指定Tag的Marker
            var result = new List<ProcessSceneMarker>();
            foreach (var marker in markers)
            {
                if (marker.Tag == markerTag)
                {
                    result.Add(marker);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// ProcessSceneMarkerをUdonSharpBehaviourの対象フィールドに適用する
        /// フィールドが配列の場合は複数取得しているマーカーから取得したObject配列を格納する
        /// </summary>
        /// <param name="markerTag">ログ出力用(マーカータグ)</param>
        /// <param name="markers">マーカータグに対応するマーカーの配列</param>
        /// <param name="targetBehaviour">ターゲット</param>
        /// <param name="fieldInfo">ターゲットの対象フィールド</param>
        /// <param name="arrayAppendMode">追加入力モード</param>
        public static void AttachProcessSceneMarkerComponent(string markerTag, ProcessSceneMarker[] markers,
            UdonSharpBehaviour targetBehaviour, FieldInfo fieldInfo, bool arrayAppendMode = false)
        {
            // 代入する変数の型
            var insertType = fieldInfo.FieldType;

            // 配列の場合
            if (insertType.IsArray)
            {
                if (_isManual)
                {
                    Debug.LogWarning($"[ProcessSceneMarkerFindAttach]{targetBehaviour.gameObject.name}>{markerTag}:{fieldInfo.Name}.{insertType.Name} :Array Append Mode is not supported in manual mode", targetBehaviour);
                    arrayAppendMode = false;
                }
                //Append Mode
                if (arrayAppendMode)
                {
                    var currentValue = fieldInfo.GetValue(targetBehaviour) as Array;
                    var newValues = ConstructVariableArray(markers, insertType);
                    //currentValue+newValues
                    var newArray = Array.CreateInstance(insertType.GetElementType(), currentValue.Length + newValues.Length);
                    Array.Copy(currentValue, 0, newArray, 0, currentValue.Length);
                    Array.Copy(newValues, 0, newArray, currentValue.Length, newValues.Length);
                    fieldInfo.SetValue(targetBehaviour, newArray);
                }
                else
                {
                    fieldInfo.SetValue(targetBehaviour, ConstructVariableArray(markers, insertType));
                }
            }
            // 普通の変数の場合
            else
            {
                var value = ConstructVariable(markers, insertType);
                if (value != null)
                    fieldInfo.SetValue(targetBehaviour, value);
                else
                    Debug.LogWarning(
                        $"{targetBehaviour.gameObject.name}: \"{markerTag}\" ({fieldInfo.FieldType}) MarkerTag Component not found");
            }

            // 変更を適用
            UdonSharpEditorUtility.CopyProxyToUdon(targetBehaviour);
            EditorUtility.SetDirty(targetBehaviour);

            Debug.Log($"ProcessSceneMarkerAttach Type:{fieldInfo.FieldType}, Object:{targetBehaviour.gameObject}",
                targetBehaviour);
            EditorUtility.DisplayProgressBar("ProcessSceneMarkerAttach",
                "Setup:(" + fieldInfo.FieldType + ")" + targetBehaviour, 0);
        }

        /// <summary>
        /// マーカーを走査して検知したType一致のObject配列を返す
        /// </summary>
        /// <param name="markerComponents">シーンにあるProcessSceneMarkerの配列</param>
        /// <param name="variableType">検索する型(配列)</param>
        /// <returns>マーカーから検知したType一致のObject配列</returns>
        private static Array ConstructVariableArray(ProcessSceneMarker[] markerComponents, Type variableType)
        {
            // 配列のTypeから要素のTypeを取得
            Type baseVariableType = variableType.GetElementType();

            var objects = new List<Object>();
            foreach (var markerComponent in markerComponents)
            {
                if (baseVariableType == typeof(GameObject))
                {
                    objects.Add(markerComponent.gameObject);
                    continue;
                }

                var component = markerComponent.GetComponent(baseVariableType);
                if (component != null)
                    objects.Add(component);
            }
            Array array = Array.CreateInstance(baseVariableType, objects.Count);
            for (int i = 0; i < objects.Count; i++)
            {
                array.SetValue(objects[i], i);
            }
            return array;
        }

        /// <summary>
        /// マーカーを走査して最初に見つかったType一致のObjectを返す
        /// </summary>
        /// <param name="markerComponents">シーンにあるProcessSceneMarkerの配列</param>
        /// <param name="variableType">検索する型</param>
        /// <returns>最初に見つかったType一致のObject</returns>
        private static Object ConstructVariable(ProcessSceneMarker[] markerComponents, Type variableType)
        {
            foreach (var markerComponent in markerComponents)
            {
                if (variableType == typeof(GameObject))
                {
                    return markerComponent.gameObject;
                }

                var component = markerComponent.GetComponent(variableType);
                if (component != null)
                    return component;
            }

            return null;
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ProcessSceneMarkerFindAttach))]
    public class ProcessSceneMarkerFindAttachInspector : Editor
    {
        private ProcessSceneMarkerFindAttach _markerAttach;

        /// <summary>
        /// ProcessSceneMarkerAttachの対象となり得る(Component型, GameObject型)フィールドのリスト
        /// </summary>
        List<FieldInfo> _fieldInfos = new List<FieldInfo>();

        /// <summary>
        /// ProcessSceneMarkerAttachに属するUdonSharpBehaviourのフィールド一覧を文字列にしたもの
        /// </summary>
        string[] _fieldNames = Array.Empty<string>();

        /// <summary>
        /// 選択中のfieldIndex
        /// </summary>
        int _selectFieldIndex = -1;

        /// <summary>
        /// 選択中のtagIndex
        /// </summary>
        int _selectTagIndex = -1;

        //in Preview Data
        List<UnityEngine.Object> _previewObjects = new List<UnityEngine.Object>();
        string _previewTag;

        /// <summary>
        /// シーン上に存在するProcessSceneMarker一覧
        /// </summary>
        ProcessSceneMarker[] markers = Array.Empty<ProcessSceneMarker>();

        /// <summary>
        /// FieldのTypeと一致するタグのリスト
        /// </summary>
        List<(string, UnityEngine.Object)> _matchTagList = new List<(string, UnityEngine.Object)>();

        private void OnEnable()
        {
            _markerAttach = target as ProcessSceneMarkerFindAttach;

            // 初期化
            if (_markerAttach)
            {
                // 先搜 UdonSharpBehaviour 所有 Field
                var targetBehaviours = _markerAttach.gameObject.GetComponents<UdonSharpBehaviour>();
                foreach (UdonSharpBehaviour targetBehaviour in targetBehaviours)
                {
                    var fieldInfos = targetBehaviour.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.NonPublic);
                    foreach (var fieldInfo in fieldInfos)
                    {
                        var fieldType = fieldInfo.FieldType;
                        if (fieldType.IsArray)
                        {
                            fieldType = fieldType.GetElementType();
                        }
                        else if (fieldType.IsGenericType)
                        {
                            // ジェネリック型の場合は最初の要素
                            fieldType = fieldType.GetGenericArguments()[0];
                        }

                        // GameObject 或者 Component 子类
                        if (!fieldType.IsValueType && (
                                fieldType == typeof(GameObject) ||
                                fieldType.IsSubclassOf(typeof(GameObject)) ||
                                fieldType.IsSubclassOf(typeof(Component)))
                           )
                        {
                            _fieldInfos.Add(fieldInfo);
                        }
                    }
                }
                // 生成字段名数组，显示在Inspector下拉菜单
                _fieldNames = _fieldInfos.Select(fieldInfo => $"{fieldInfo.Name} ({fieldInfo.FieldType.Name})").ToArray();

                // 根据现有 _variable 名找到下标
                _selectFieldIndex = _fieldInfos.IndexOf(
                    _fieldInfos.FirstOrDefault(
                        si => si.Name == serializedObject.FindProperty("_variable").stringValue
                    )
                );

                // ★ 预览时，也根据 _onlyFindChild 来选用函数
                if (_markerAttach.OnlyFindChild)
                {
                    markers = ProcessSceneMarkerFindAttach.FindProcessSceneMarkerComponentsInChildren(
                        _markerAttach.transform,
                        "NotFindProcessSceneMarkerTag",
                        true
                    );
                }
                else
                {
                    markers = ProcessSceneMarkerFindAttach.FindProcessSceneMarkerComponents(
                        _markerAttach.gameObject.scene
                    );
                }

                if (_selectFieldIndex != -1)
                {
                    SetupMatchTagList();
                }
            }
        }

        /// <summary>
        /// 対象Fieldのコンポーネントと一致するタグリストの作成
        /// </summary>
        void SetupMatchTagList()
        {
            _selectTagIndex = -1;
            _matchTagList.Clear();

            var selectedType = _fieldInfos[_selectFieldIndex].FieldType;
            foreach (var marker in markers)
            {
                if (selectedType == typeof(GameObject))
                {
                    _matchTagList.Add((marker.Tag, marker.gameObject));
                    continue;
                }

                // 如果是数组，则拿元素类型
                var checkType = selectedType;
                while (checkType.IsArray)
                {
                    checkType = checkType.GetElementType();
                }

                if (checkType == typeof(GameObject))
                {
                    _matchTagList.Add((marker.Tag, marker.gameObject));
                    continue;
                }
                var component = marker.gameObject.GetComponent(checkType);
                if (component != null)
                {
                    _matchTagList.Add((marker.Tag, component));
                }
            }
            serializedObject.Update();
            _selectTagIndex = _matchTagList.IndexOf(
                _matchTagList.FirstOrDefault(
                    si => si.Item1 == serializedObject.FindProperty("_searchTag").stringValue
                )
            );
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUI.BeginChangeCheck();
                _selectFieldIndex = EditorGUILayout.Popup("Variable", _selectFieldIndex, _fieldNames);
                if (EditorGUI.EndChangeCheck())
                {
                    if (_selectFieldIndex != -1)
                    {
                        SetupMatchTagList();
                    }
                }

                if (_selectFieldIndex != -1)
                {
                    _selectTagIndex = EditorGUILayout.Popup("Tag", _selectTagIndex, _matchTagList.Select(si => si.Item1).ToArray());
                    if (_selectTagIndex != -1)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Apply"))
                        {
                            serializedObject.Update();
                            serializedObject.FindProperty("_searchTag").stringValue = _matchTagList[_selectTagIndex].Item1;
                            serializedObject.FindProperty("_variable").stringValue = _fieldInfos[_selectFieldIndex].Name;
                            serializedObject.ApplyModifiedProperties();
                        }
                        //Preview ボタン
                        if (GUILayout.Button("Preview"))
                        {
                            _previewObjects.Clear();
                            _previewTag = _matchTagList[_selectTagIndex].Item1 + " (" + _matchTagList[_selectTagIndex].Item2.GetType().Name + ")";
                            //Try Find Need Attach Component And Preview Result Add To _previewObjects
                            foreach (var previewObject in _matchTagList)
                            {
                                if (previewObject.Item1 == _matchTagList[_selectTagIndex].Item1)
                                {
                                    _previewObjects.Add(previewObject.Item2);
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        if (_previewObjects.Count > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label("Preview Result: " + _previewTag);
                            if (GUILayout.Button("X"))
                            {
                                _previewObjects.Clear();
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.BeginDisabledGroup(true);
                            int count = 0;
                            foreach (var previewObject in _previewObjects)
                            {
                                EditorGUILayout.ObjectField(count.ToString(), previewObject, typeof(UnityEngine.Object), true);
                                count++;
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please Select Tag", MessageType.Warning);
                    }
                }

            }
            EditorGUILayout.EndVertical();
        }
    }
#endif
}