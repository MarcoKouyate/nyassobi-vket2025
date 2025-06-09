using UnityEngine;
using UnityEditor;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    /// <summary>
    /// VketPrefabに対して設定をする場合にこのクラスを継承したウィンドウを作成する想定
    /// </summary>
    public class VketPrefabSettingWindow : EditorWindow
    {
        /// <summary>
        /// VketPrefabのインスタンス
        /// </summary>
        protected Transform _vketPrefabInstance;
        
        /// <summary>
        /// 生成キャンセルボタンを表示するか
        /// </summary>
        protected bool _isShowCreateCancelButton = false;
        
        #region UI設定用
        
        /// <summary>
        /// //スクロール位置
        /// </summary>
        protected Vector2 _scrollPosition = Vector2.zero;

        #endregion
        
        /// <summary>
        /// 自動折り返し用スタイル
        /// </summary>
        protected GUIStyle _wrapStyle;
        
        /// <summary>
        /// 設定項目の説明用スタイル
        /// </summary>
        protected GUIStyle _descriptionStyle;
        
        /// <summary>
        /// 設定項目用スタイル
        /// </summary>
        protected GUIStyle _l3Style;
        protected GUIStyle _settingItemStyle;
        /// <summary>
        /// テキストスタイルの初期化
        /// </summary>
        protected virtual void InitStyle()
        {
            if (_wrapStyle != null)
                return;
            
            _wrapStyle = new GUIStyle( GUI.skin.label) { wordWrap = true };
            
            _descriptionStyle = new GUIStyle( GUI.skin.label ) { wordWrap = true, fontSize = 18};
            _settingItemStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 16 };
            _l3Style = new GUIStyle( GUI.skin.label ) { wordWrap = true, fontSize = 16};
        }

        public void InitWindow(VketPrefabInformation vketPrefabInfo)
        {
            if (vketPrefabInfo)
            {
                _vketPrefabInstance = VketPrefabsMainWindow.CreateVketPrefab(vketPrefabInfo);
                _isShowCreateCancelButton = true;
            }
            InitWindow();
        }

        /// <summary>
        /// ウィンドウの初期化
        /// overrideする想定
        /// </summary>
        protected virtual void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(200f, 200f);
        }
        
        /// <summary>
        /// プレハブを生成し、設定ウィンドウを開く
        /// </summary>
        /// <param name="vketPrefabInfo">生成するVketPrefabInfoを指定</param>
        /// <typeparam name="T">VketPrefabSettingWindow継承クラスを指定</typeparam>
        public static void OpenSettingWindow<T>(VketPrefabInformation vketPrefabInfo) where T : EditorWindow
        {
            VketPrefabSettingWindow window;
            
            // 既に開いている場合は閉じる
            if (EditorWindow.HasOpenInstances<T>())
            {
                window = GetWindow<T>() as VketPrefabSettingWindow;
                
                if(window)
                    window.Close();
            }
            
            // 開いて初期化
            window = GetWindow<T>(true,typeof(T).Name) as VketPrefabSettingWindow;

            if (window)
            {
                window.InitWindow(vketPrefabInfo);
                window.Show();
            }
        }
        
        /// <summary>
        /// 外部から設定ウィンドウを開く
        /// </summary>
        /// <param name="instance">設定するVketVideoPlayerのインスタンスを指定</param>
        /// <typeparam name="T">VketPrefabSettingWindow継承クラスを指定</typeparam>
        public static void OpenSettingWindow<T>(Transform instance) where T : EditorWindow
        {
            VketPrefabSettingWindow window;
            
            // 既に開いている場合は閉じる
            if (EditorWindow.HasOpenInstances<T>())
            {
                window = GetWindow<T>() as VketPrefabSettingWindow;
                
                if(window)
                    window.Close();
            }
            
            // 開いて初期化
            window = GetWindow<T>(true,typeof(T).Name) as VketPrefabSettingWindow;

            if (window)
            {
                window._vketPrefabInstance = instance;
                window.InitWindow();
                window.Show();
            }
        }

        private void OnGUI()
        {
            InitStyle();
            if (!BaseHeader("VketPrefab")) 
                return;

            // SettingWindowごとに独自の処理をオーバーライドして追記する想定
            
            BaseFooter("VketPrefab");
        }

        /// <summary>
        /// デフォルトのヘッダー
        /// </summary>
        /// <param name="targetName">VketPrefab名</param>
        protected bool BaseHeader(in string targetName)
        {
            /* "{0}の設定を行います。" */
            GUILayout.Label(LocalizedUtility.Get("VketPrefabSettingWindow.Header.Title", targetName), _descriptionStyle);
            
            if (!_vketPrefabInstance)
            {
                /* "設定対象の{0}が存在しません。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketPrefabSettingWindow.Header.InstanceNotFound", targetName), MessageType.Error);
                return false;
            }

            GUILayout.Space(5);
            return true;
        }

        /// <summary>
        /// デフォルトのフッター
        /// </summary>
        /// <param name="targetName">VketPrefab名</param>
        /// <param name="visualObject">見た目にしている3Dオブジェクトがある場合は指定</param>
        protected void BaseFooter(in string targetName, GameObject visualObject = null)
        {
            GUILayout.Space(5);
            
            /* "設定対象を選択" */
            if (GUILayout.Button(LocalizedUtility.Get("VketPrefabSettingWindow.Footer.SelectButton")))
            {
                Selection.activeTransform = _vketPrefabInstance;
                EditorGUIUtility.PingObject(_vketPrefabInstance.gameObject);
            }
            
            /* "現在設定している{0}を選択します。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketPrefabSettingWindow.Footer.SelectButton.Help", targetName), MessageType.Info);

            GUILayout.Space(10);
            
            if (_isShowCreateCancelButton)
            {
                /* "生成をキャンセルして削除" */
                if (GUILayout.Button(LocalizedUtility.Get("VketPrefabSettingWindow.Footer.CancelButton")))
                {
                    // 見た目の3Dオブジェクトが設定されている場合は外に移動
                    if (visualObject)
                        visualObject.transform.parent = null;
                    
                    // インスタンスを削除してクローズ
                    DestroyImmediate(_vketPrefabInstance.gameObject);
                    Close();
                }

                /* "設定と生成された{0}を破棄します。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketPrefabSettingWindow.Footer.CancelButton.Help", targetName), MessageType.Info);
                
                GUILayout.Space(10);
            }

            /* "設定を終了" */
            if (GUILayout.Button(LocalizedUtility.Get("VketPrefabSettingWindow.Footer.CloseButton")))
            {
                Close();
            }
        }

        /// <summary>
        /// Texture2Dのフィールドを表示
        /// </summary>
        /// <param name="texture">対象のテクスチャを指定</param>
        /// <returns></returns>
        protected static Texture2D TextureField(Texture2D texture)
        {
            return TextureField("", texture);
        }
        
        /// <summary>
        /// Texture2Dのフィールドを表示
        /// </summary>
        /// <param name="name">ラベル名</param>
        /// <param name="texture">対象のテクスチャを指定</param>
        /// <returns></returns>
        protected static Texture2D TextureField(string name, Texture2D texture)
        {
            Texture2D result;
            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.BeginVertical();
                var style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.UpperCenter;
                style.fixedWidth = 70;
                GUILayout.Label(name, style);
            }
            
            result = EditorGUILayout.ObjectField(texture, typeof(Texture2D), false) as Texture2D;
            result = EditorGUILayout.ObjectField(result, typeof(Texture2D), false, GUILayout.Width(120), GUILayout.Height(120)) as Texture2D;

            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.EndVertical();
            }
            
            return result;
        }
        
        /// <summary>
        /// Spriteのフィールドを表示
        /// </summary>
        /// <param name="sprite">対象のテクスチャを指定</param>
        /// <returns></returns>
        protected static Sprite SpriteField(Sprite sprite)
        {
            return SpriteField("", sprite);
        }
        
        /// <summary>
        /// Spriteのフィールドを表示
        /// </summary>
        /// <param name="name">ラベル名</param>
        /// <param name="sprite">対象のテクスチャを指定</param>
        /// <returns></returns>
        protected static Sprite SpriteField(string name, Sprite sprite)
        {
            Sprite result;
            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.BeginVertical();
                var style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.UpperCenter;
                style.fixedWidth = 70;
                GUILayout.Label(name, style);
            }
            
            result = EditorGUILayout.ObjectField(sprite, typeof(Sprite), false) as Sprite;
            result = EditorGUILayout.ObjectField(result, typeof(Sprite), false, GUILayout.Width(120), GUILayout.Height(120)) as Sprite;

            if (!string.IsNullOrEmpty(name))
            {
                GUILayout.EndVertical();
            }
            
            return result;
        }
    }
}
