
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketGroupPageOpener))]
    public class VketGroupPageOpenerEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketGroupPageOpener vketGroupPageOpener;
        private SerializedProperty _groupIdProp;
        private SerializedProperty _openToStorePage;
        private Transform _canvas;
        private Image image;
        private bool a;
        
        private int summaryHeight = 88;
        
        private void OnEnable()
        { 
            vketGroupPageOpener = target as VketGroupPageOpener;
            _groupIdProp = serializedObject.FindProperty("_groupId");
            _openToStorePage = serializedObject.FindProperty("_openToStorePage");
            SetupImage();
        }

        void SetupImage()
        {
            if(!_canvas)
                _canvas = vketGroupPageOpener.transform.Find("Canvas");

            if (_canvas && !image)
                image = _canvas.Find("Image")?.GetComponent<Image>();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();
            
            // Draw Title and Summary
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("VketGroupPageOpener", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(summaryHeight));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketGroupPageOpenerEditor"));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_groupIdProp);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_groupIdProp.stringValue)))
            {
                if(GUILayout.Button("Open", GUILayout.Width(50)))
                {
                    Application.OpenURL($"https://vrchat.com/home/group/{_groupIdProp.stringValue}");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(_openToStorePage);
            
            SetupImage();
            if (image)
            {
                EditorGUI.BeginChangeCheck();
                var sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", image.sprite, typeof(Sprite), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(image, "Modify image sprite");
                    image.sprite = sprite;
                    image.enabled = true;
                }

                EditorGUILayout.Space();
            }
            else
            {
                Debug.Log("Not Found \"VketWebPageOpener/Canvas/Image\"");
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUI.BeginDisabledGroup(vketGroupPageOpener.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketGroupPageOpenerSettingWindow>(vketGroupPageOpener.transform);
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
