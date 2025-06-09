using UnityEditor;
using UnityEngine;
using VketTools.Networking;
#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using VitDeck.Main;
#endif
using VketTools.Utilities;

namespace VketTools.Main
{
    public class VketPackagePopup : PopupWindowContent
    {
        private static ToolsApi.SubPackages _packages;
        private Vector2 _scrollPos;

        public override async void OnOpen()
        {
            var versionInfo = AssetUtility.VersionInfoData;
            _packages = await ToolsApi.GetPackage(versionInfo.EventVersion, versionInfo.Type.ToString());
        }

        public override void OnClose()
        {
            _packages = null;
        }

        public override void OnGUI(Rect rect)
        {
            var box = new GUIStyle(GUI.skin.box);
            var b1 = new GUIStyle(GUI.skin.button);
            var t1 = new GUIStyle(GUI.skin.label);
            float buttonHeight = 30;

            b1.fontSize = 12;
            t1.fontSize = 12;
            t1.alignment = TextAnchor.MiddleCenter;

            if (Event.current.type == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(new Rect(0, 0, EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight), false, true, true, false);
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Packages", t1, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, box);
            {
                EditorGUILayout.Space();

                if (_packages == null || _packages.packages == null)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("- No packages -", t1);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    foreach (var package in _packages.packages)
                    {
                        // ShaderPackはVketShaderPackageImporterからインポートするのでスキップ
                        if(package.package_name == "VketShaders") continue;
                        EditorGUILayout.BeginHorizontal();
                        {
                            var content = new GUIContent($"{package.package_name}-{package.package_version}");
#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
                            if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, b1, EditorGUIUtility.currentViewWidth), GUILayout.Height(buttonHeight)))
                            {
                                var downloader = new PackageDownloader();
                                downloader.Download(package.download_url, package.package_name);
                                downloader.Import(package.package_name);
                                downloader.Settlement(package.package_name);
                            }
#endif
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
