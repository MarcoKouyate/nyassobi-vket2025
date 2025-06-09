using UnityEditor;
using UnityEngine;
using VketTools.Utilities;

namespace VketTools.Main
{
    /// <summary>
    /// 入稿方法ポップアップに表示するコンテンツ
    /// </summary>
    public class MessagePopupWindowContent : PopupWindowContent
    {
        private readonly string _message;
        public MessagePopupWindowContent(string message)
        {
            _message = message;
        }
        
        /// <summary>
        /// サイズを取得する
        /// </summary>
        public override Vector2 GetWindowSize()
        {
            return new Vector2(Screen.width, UIUtility.GetHelpBoxHeight(_message));
        }

        /// <summary>
        /// GUI描画
        /// </summary>
        public override void OnGUI(Rect rect)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
            EditorGUILayout.LabelField(_message, style );
        }
    }
}