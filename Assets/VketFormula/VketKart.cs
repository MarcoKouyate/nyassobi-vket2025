using UnityEditor;
using UnityEngine;

namespace VketFormula
{
    [IgnoreBuild]
    public class VketKart : MonoBehaviour
    {
        [SerializeField] private int _selectType;
        [SerializeField] private AnimationClip _drivingClip;
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(VketKart))]
    public class VketKartInspector : Editor
    {
        private static readonly string[] KartTypes = {
            "バランス","スピード","テクニック"
        };
        private static readonly string[] KartTypes_en =
        {
            "Balance","Speed","Technique"
        };

        private Vector2 _scrollPosition = Vector2.zero;
        private bool _isEnglish;
        private VketKart _vketKart;

        private SerializedProperty _scriptProp;
        private SerializedProperty _selectTypeProp;
        private SerializedProperty _drivingClipProp;

        private void OnEnable()
        {
            _scriptProp = serializedObject.FindProperty("m_Script");
            _selectTypeProp = serializedObject.FindProperty("_selectType");
            _drivingClipProp = serializedObject.FindProperty("_drivingClip");
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_scriptProp);
            }
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (_isEnglish)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("JP"))
                    {
                        _isEnglish = false;
                    }
                }
                EditorGUILayout.TextArea(
                    "Attach it to the object you wish to use as a racing vehicle.\n" +
                    "*Please note that not all models can be employed.\n" +
                    "*We may adjust the scale of the 3D model here in consideration of the visitor experience.\n" +
                    "*The seating arrangement for players will be decided by us.\n" +
                    "*Only the appearance of the model will be incorporated, so no animations or other gimmicks that are activated during pickup or use will be in operation during the world experience."
                , EditorStyles.wordWrappedLabel);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("EN"))
                    {
                        _isEnglish = true;
                    }
                }
                EditorGUILayout.TextArea(
                    "レーシングヴィークルとして使用したいオブジェクトにアタッチしてください。\n" +
                    "※全てのモデルを採用できるとは限らないため予めご了承ください。\n" +
                    "※来場者体験を鑑みて3Dモデルのスケールをこちらで調整させてもらう場合があります。\n" +
                    "※プレイヤーが座る位置はこちらで決定させていただきます。\n" +
                    "※モデルの見た目のみを組み込みするので、ワールドの体験時はPickupやUse時起動するアニメーション等ギミックは一切動作しません。"
                , EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
            serializedObject.Update();
            _selectTypeProp.intValue = EditorGUILayout.Popup("KartType", _selectTypeProp.intValue, _isEnglish ? KartTypes_en : KartTypes);
            EditorGUILayout.PropertyField(_drivingClipProp);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
        }
    }
#endif
}