
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
#endif

namespace Vket.EssentialResources
{
    [IgnoreBuild]
    public class LanguageMarker : ProcessSceneMarker
#if UNITY_EDITOR
        , IProcessSceneWithReport
#endif
    {
        public static readonly string[] LanguageTags =
        {
            "JpObject",
            "EnObject",
        };

#if UNITY_EDITOR
        public int callbackOrder => -1000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            Process(scene);
        }

        private static void Process(Scene scene)
        {
            Debug.Log("LanguageMarker:Process");
            EditorUtility.DisplayProgressBar("LanguageMarker", "Processing..", 0);

            int count = 0;

            foreach (var item in scene.GetRootGameObjects())
            {
                foreach (var languageMarker in item.GetComponentsInChildren<LanguageMarker>(true))
                {
                    int select = 0;
                    for (var index = 0; index < LanguageTags.Length; index++)
                    {
                        var languageTag = LanguageMarker.LanguageTags[index];
                        if (languageMarker.Tag == languageTag)
                            select = index;
                    }
                    switch (select)
                    {
                        case 0:
                            languageMarker.gameObject.SetActive(true);
                            break;
                        case 1:
                            languageMarker.gameObject.SetActive(false);
                            break;
                    }
                    count++;
                }
            }

            Debug.Log($"Execute methods in LanguageMarker. ({count})");
            Debug.Log("LanguageMarker:End");
            EditorUtility.ClearProgressBar();
        }

#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(LanguageMarker))]
    public class LanguageMarkerInspector : Editor
    {
        private LanguageMarker _languageMarker;
        private int _select;
        private SerializedProperty _tagProperty;
        private GameObject _targetGameObject;
        private void OnEnable()
        {
            _languageMarker = target as LanguageMarker;
            _targetGameObject = _languageMarker.gameObject;
            _tagProperty = serializedObject.FindProperty("_tag");
            if (string.IsNullOrEmpty(_tagProperty.stringValue))
            {
                _tagProperty.stringValue = LanguageMarker.LanguageTags[0];
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            for (var select = 0; select < LanguageMarker.LanguageTags.Length; select++)
            {
                var languageTag = LanguageMarker.LanguageTags[select];
                if (_tagProperty.stringValue == languageTag)
                    _select = select;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                _select = EditorGUILayout.Popup("言語Tag選択", _select, LanguageMarker.LanguageTags);
                if (check.changed)
                {
                    switch (_select)
                    {
                        case 0:
                            _targetGameObject.SetActive(true);
                            break;
                        case 1:
                            _targetGameObject.SetActive(false);
                            break;
                    }
                    _tagProperty.stringValue = LanguageMarker.LanguageTags[_select];
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

    [InitializeOnLoad]
    public class LanguageMarkerHierarchy
    {
        static LanguageMarkerHierarchy()
        {
            // 注册Hierarchy窗口回调
            EditorApplication.hierarchyWindowItemOnGUI += DisplayIcons;
        }

        private static void DisplayIcons(int instanceID, Rect selectionRect)
        {
            // 获取GameObject
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (go != null)
            {
                // 获取该GameObject上的所有组件
                Component[] components = go.GetComponents<Component>();

                foreach (Component component in components)
                {
                    // 检查是否为LanguageMarker类型的脚本
                    if (component is LanguageMarker)
                    {
                        // 绘制图标
                        Rect iconRect = new Rect(selectionRect);
                        GUIContent content = new GUIContent(go.name);
                        float textWidth = EditorStyles.label.CalcSize(content).x;
                        iconRect.x = selectionRect.xMin + textWidth + 25; // 调整图标位置
                        iconRect.width = 20;

                        // 获取脚本中的变量值A
                        LanguageMarker languageMarker = (LanguageMarker)component;
                        string tag = new SerializedObject(languageMarker).FindProperty("_tag").stringValue;

                        // 绘制图标
                        // 使用GUI.DrawTexture来绘制Texture2D图标
                        GUI.DrawTexture(iconRect, EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow@2x").image);

                        // 可以将变量值显示在图标旁边
                        Rect textRect = new Rect(selectionRect);
                        textRect.x = iconRect.x - -20;
                        textRect.width = 30;
                        EditorGUI.LabelField(textRect, tag.Substring(0, 2).ToUpper());
                    }
                }
            }
        }
    }
#endif
}
