using System;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using VketTools.Utilities;

namespace VketTools.Main
{
    /// <summary>
    /// 隠すためのEditorWindow
    /// Popupで表示する想定
    /// </summary>
    public class BlinderPopupWindow : EditorWindow
    {
        private Color? _color;
        private string _message;
        
        /// <summary>
        /// BlinderPopupWindowの更新
        /// </summary>
        /// <param name="rect">座標と大きさ</param>
        /// <param name="message">メッセージ</param>
        /// <param name="color">色があれば指定</param>
        public void UpdateGUI(Rect rect, string message = "", Color? color = null)
        {
            minSize = rect.size;
            maxSize = rect.size;
            position = rect;
            _message = message;
            _color = color;
        }

        private void OnGUI()
        {
            if (_color.HasValue)
            {
                var rect = position;
                rect.position = Vector2.zero;
                EditorGUI.DrawRect(rect, _color.GetValueOrDefault());
            }

            if (!string.IsNullOrEmpty(_message))
            {
                var content = new GUIContent(_message);
                GUILayout.Label(content, UIUtility.GetContentSizeFitStyle(content, GUI.skin.label, position.width), GUILayout.Width(position.width));
            }
        }
    }
    
    /// <summary>
    /// Toolbarを隠すエディタ拡張
    /// </summary>
    public static class VketBlinderToolbar
    {
        private static bool _isBlind;

        /// <summary>
        /// ツールバーのボタン3つ分の長さ
        /// </summary>
        const float ButtonsWidth = 120;
        
        #region Type
        private static readonly Type ViewType = typeof(Editor).Assembly.GetType("UnityEditor.View");
        private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly Type ContainerWindowType = typeof(Editor).Assembly.GetType("UnityEditor.ContainerWindow");
        #endregion
        
        #region Info
        private static readonly PropertyInfo ViewPositionProp = ViewType.GetProperty("position", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
        private static readonly PropertyInfo ContainerWindowPositionProp = ContainerWindowType.GetProperty("position", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
        private static readonly FieldInfo ContainerWindowShowModeField = ContainerWindowType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
        #endregion
        
        /// <summary>
        /// ツールバーのインスタンス
        /// </summary>
        static ScriptableObject _currentToolbar;
        
        /// <summary>
        /// ブラインドポップアップウィンドウ
        /// </summary>
        private static BlinderPopupWindow _window;

        /// <summary>
        /// 上書きする座標
        /// </summary>
        private static Rect _overrideRect;
        
        /// <summary>
        /// 隠し状態の更新
        /// </summary>
        public static bool IsBlind
        {
            set
            {
                _isBlind = value;
                if (_isBlind)
                {
                    ShowBlinderWindow(_overrideRect);
                }
                else
                {
                    CloseBlinderWindow();
                }
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Update処理を設定
            EditorApplication.update += Update;
            // アプリを終了する時に呼び出されるコールバックを設定
            EditorApplication.wantsToQuit += WantsToQuit;
        }

        /// <summary>
        /// アプリ終了前にウィンドウを閉じる
        /// </summary>
        /// <returns></returns>
        private static bool WantsToQuit()
        {
            IsBlind = false;
            return true;
        }

        /// <summary>
        /// Update時に呼ばれる
        /// </summary>
        private static void Update()
        {
            if (_isBlind)
            {
                UpdateBlinderWindowRect();
                UpdateBlinderWindow();
            }
        }

        /// <summary>
        /// 隠す座標の更新
        /// </summary>
        private static void UpdateBlinderWindowRect()
        {
            var containerWindowPosition = GetMainWindowPosition();
            var toolBarSize = GetToolbarSize();
            float playButtonsPosition = Mathf.RoundToInt((toolBarSize.x - ButtonsWidth) / 2);
            var overrideRect = new Rect(playButtonsPosition + containerWindowPosition.x, containerWindowPosition.y, ButtonsWidth, toolBarSize.y);
            
            if(_overrideRect.Equals(overrideRect))
                return;
                
            _overrideRect = overrideRect;
        }
        
        /// <summary>
        /// BlinderWindowの更新
        /// </summary>
        private static void UpdateBlinderWindow()
        {
            if (_window)
            {
                _window.UpdateGUI(_overrideRect, /* "処理中" */ AssetUtility.GetMain("Vket_BlinderToolbar.Message"));
            }
        }

        /// <summary>
        /// ウィンドウを開く
        /// </summary>
        /// <param name="rect">隠す位置</param>
        private static void ShowBlinderWindow(Rect rect)
        {
            if (!_window)
            {
                _window = ScriptableObject.CreateInstance<BlinderPopupWindow>();
                _window.UpdateGUI(rect, /* "処理中" */ AssetUtility.GetMain("Vket_BlinderToolbar.Message"));
                _window.ShowPopup();
            }
        }

        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        private static void CloseBlinderWindow()
        {
            if (_window)
            {
                _window.Close();
                _window = null;
            }
        }
        
        /// <summary>
        /// ツールバーのサイズ取得
        /// </summary>
        /// <returns>ツールバーのサイズ</returns>
        private static Vector2 GetToolbarSize()
        {
            // ツールバーの取得
            if (_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
                _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            }

            if (_currentToolbar != null && ViewPositionProp != null)
            {
                var rect = (Rect)ViewPositionProp.GetValue(_currentToolbar, null);
                return rect.size;
            }
            return Vector2.zero;
        }
        
        /// <summary>
        /// UnityEditor全体の左上Rectを取得
        /// </summary>
        /// <returns>UnityEditor全体の左上Rect</returns>
        private static Rect GetMainWindowPosition()
        {
            if (ContainerWindowShowModeField == null || ContainerWindowPositionProp == null)
                return Rect.zero;
 
            foreach (var window in Resources.FindObjectsOfTypeAll(ContainerWindowType))
            {
                var showMode = (int) ContainerWindowShowModeField.GetValue(window);
                if (showMode == 4)// UnityEditor.ShowMode.MainWindow
                {
                    return (Rect) ContainerWindowPositionProp.GetValue(window, null);
                }
            }
 
            return Rect.zero;
        }
    }
}