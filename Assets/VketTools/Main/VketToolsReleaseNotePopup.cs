using System.IO;
using UnityEditor;
using UnityEngine;
using VketAssets.Utilities.Language.Runtime;

namespace VketTools.Main
{
    public class VketToolsReleaseNotePopup : PopupWindowContent
    {
        private const string FilePathJp = "Assets/VketTools/Config/ReleaseNote/vkettools_release_notes.md";
        private const string FilePathEn = "Assets/VketTools/Config/ReleaseNote/vkettools_release_notes_en.md";
        
        private Vector2 _scrollPos;
        private float contentHeight;
        private SystemLanguage _language;
        private string[] _loadLines;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(500, 600);
        }
        
        public override void OnOpen()
        {
            _language = LocalizedSetting.Instance.Language;
            _loadLines = LoadMd(_language);
        }

        private string[] LoadMd(SystemLanguage language) => File.ReadAllLines(language == SystemLanguage.Japanese ? FilePathJp : FilePathEn);

        public override void OnGUI(Rect rect)
        {
            var b1 = new GUIStyle(GUI.skin.button);
            var t1 = new GUIStyle(GUI.skin.label);

            b1.fontSize = 12;
            t1.fontSize = 12;
            t1.alignment = TextAnchor.MiddleCenter;
            
            var language = LocalizedSetting.Instance.Language;
            if (_language != language)
            {
                _loadLines = LoadMd(language);
                _language = language;
            }
            
            EditorGUI.LabelField(new Rect(0, 0, EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight), language == SystemLanguage.Japanese ? "リリースノート" : "ReleaseNote", t1);
            
            Rect scrollViewRect = new Rect(0, EditorGUIUtility.singleLineHeight, rect.width, rect.height - 50);
            Rect contentRect = new Rect(0, EditorGUIUtility.singleLineHeight, rect.width - 16, contentHeight);
            
            _scrollPos = GUI.BeginScrollView(scrollViewRect, _scrollPos, contentRect);
            {
                float y = 0;
                float lineSpacing = 4;
                float viewWidth = contentRect.width;

                GUIStyle style = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true
                };

                for (int i = 0; i < _loadLines.Length; i++)
                {
                    Rect lineRect = new Rect(0, y, viewWidth, 0);
                    float lineHeight = DrawMarkdownLine(lineRect, _loadLines[i], style);
                    y += lineHeight + lineSpacing;
                }
                contentHeight = y;
            }
            GUI.EndScrollView();
        }
        
        private float DrawMarkdownLine(Rect rect, string line, GUIStyle baseStyle)
        {
            string text = line;
            GUIStyle style = new GUIStyle(baseStyle);

            if (string.IsNullOrWhiteSpace(line))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (line.StartsWith("# "))
            {
                style.fontSize = 16;
                style.fontStyle = FontStyle.Bold;
                text = line.Substring(2);
            }
            else if (line.StartsWith("## "))
            {
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;
                text = line.Substring(3);
            }
            else if (line.StartsWith("- "))
            {
                text = "• " + line.Substring(2);
            }

            float height = style.CalcHeight(new GUIContent(text), rect.width);
            Rect labelRect = new Rect(rect.x, rect.y, rect.width, height);
            EditorGUI.LabelField(labelRect, text, style);
            return height;
        }
    }
}
