
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VketTools.Utilities;

namespace VketTools.Main
{
    public class VketTutorialWindow : EditorWindow
    {
        // 見出し
        private static readonly string[] HeaderLabelKey = {
            "CommonHeader", // 共通
            "SpaceHeader", // スペース出展
            "ItemHeader", //アイテム出展
        };

        // 共通チュートリアル
        private static readonly List<(string label, string url, int indentLevel)> CommonTutorialList = new()
        {
            ("・入稿準備", "", 1),
            ("・VketToolsにログインする", "a", 2),
            ("・入稿するワールドを選択する", "a", 2),
            ("・入稿シーンの作成", "a", 2),
            ("・入稿シーンをUnityで開く", "a", 2),
            ("・入稿チェック", "", 1),
            ("・入稿データをチェックする", "a", 2),
            ("・容量をチェックする", "a", 2),
            ("・SetPassCalls、Batchesをチェックする", "a", 2),
            ("・VRChatで動作を確認する", "a", 2),
            ("・パッケージのダウンロード", "", 1),
            ("・シェーダーをインポートする", "a", 2),
            ("・入稿方法", "", 1),
            ("・入稿する", "a", 2),
        };
        
        // ブースチュートリアル
        private static readonly List<(string label, string url, int indentLevel)> BoothTutorialList = new()
        {
            ("・入稿シーンの設定", "", 1),
            ("・シーンにブースを作成する", "a", 2),
            ("・VketPrefabsの使い方", "", 1),
            ("・VketPrefabs一覧を表示する", "a", 2),
            ("・アバター切り替えのボタンを設置する", "a", 2),
            ("・座れる椅子を配置する", "a", 2),
            ("・言語切り替えボタンを設置する", "a", 2),
            ("・持ち運べるアイテムを設置する", "a", 2),
            ("・ブース内でBGMを停止、変更する", "a", 2),
            ("・ビデオを再生する", "a", 2),
            ("・ビデオを変更する", "a", 2),
            ("・サークルページを開くボタンを表示する", "a", 2),
            ("・アイテムページ(VketStore)を開くボタンを表示する", "a", 2),
            ("・Webから画像を取得、表示する", "a", 2),
            ("・Webから文字列を取得、表示する", "a", 2),
        };
        
        // アイテムチュートリアル
        private static readonly List<(string label, string url, int indentLevel)> ItemTutorialList = new()
        {
            ("・入稿シーンの設定", "", 1),
            ("・シーンにアイテムを設定する", "a", 2),
            ("・アイテムの種類を選択する", "a", 2),
            ("・台座を選択する", "a", 2),
            ("・キャプションボードの表示文言を設定する", "a", 2),
        };

        private readonly List<string> _headingLabels = new();
        private readonly List<string> _commonLabelKeyList = new();
        private readonly List<string> _boothLabelKeyList = new();
        private readonly List<string> _itemLabelKeyList = new();
        
        private bool _commonFoldout = true;
        private bool _boothFoldout = true;
        private bool _itemFoldout = true;
        private Vector2 _scrollPos;
        private GUIStyle _titleLabelStyle;
        private GUIStyle _foldoutStyle;
        private GUIStyle[] _indentLabelStyleArray;
        
        //[MenuItem("VketTools/Tutorial", false, 700)]
        static void Open()
        {
            var window = GetWindow<VketTutorialWindow>("VketTools - Tutorial");
            window.minSize = new Vector2(350, 350);
        }

        private void OnEnable()
        {
            _titleLabelStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 16,
            };
            _indentLabelStyleArray = new GUIStyle[]
            {
                new(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 16,
                },
                new(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 14,
                },
                new(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 12,
                },
            };
        }

        void CreateHeaderString()
        {
            foreach (var key in HeaderLabelKey)
            {
                _headingLabels.Add(AssetUtility.GetTutorial(key));
            }
        }
        
        void OnGUI()
        {
            CreateHeaderString();
            
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("VketTools チュートリアル", _titleLabelStyle);
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox("青い文字をクリックすると、ブラウザが開きます。", MessageType.Info);

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPos, GUI.skin.box))
            {
                _scrollPos = scrollView.scrollPosition;
                
                _commonFoldout = EditorGUILayout.Foldout(_commonFoldout, _headingLabels[0], _foldoutStyle);
                if (_commonFoldout)
                {
                    EditorGUILayout.Space(5);
                    DrawTutorialGUI(CommonTutorialList);
                }
                
                EditorGUILayout.Space(10);
                _boothFoldout = EditorGUILayout.Foldout(_boothFoldout, _headingLabels[1], _foldoutStyle);
                if (_boothFoldout)
                {
                    EditorGUILayout.Space(5);
                    DrawTutorialGUI(BoothTutorialList);
                }
                
                EditorGUILayout.Space(10);
                _itemFoldout = EditorGUILayout.Foldout(_itemFoldout, _headingLabels[2], _foldoutStyle);
                if (_itemFoldout)
                {
                    EditorGUILayout.Space(5);
                    DrawTutorialGUI(ItemTutorialList);
                }
                
            }
        }

        void DrawTutorialGUI(List<(string label, string url, int indentLevel)> tutorialList)
        {
            foreach (var tutorialPair in tutorialList)
            {
                EditorGUI.indentLevel += tutorialPair.indentLevel;
                if (string.IsNullOrEmpty(tutorialPair.url))
                {
                    EditorGUILayout.LabelField(tutorialPair.label, _indentLabelStyleArray[tutorialPair.indentLevel]);
                }
                else
                {
                    var urlStyle = new GUIStyle(_indentLabelStyleArray[tutorialPair.indentLevel])
                    {
                        margin = new RectOffset(EditorGUI.indentLevel * 15, 0, 0, 0)
                    };
                    var state = urlStyle.normal;
                    state.textColor = Color.blue;
                    urlStyle.normal = state;
                    if (GUILayout.Button(tutorialPair.label, urlStyle))
                    {
                        Application.OpenURL(tutorialPair.url);
                    }
                }
                EditorGUI.indentLevel -= tutorialPair.indentLevel;
            }
        }
    }
}
