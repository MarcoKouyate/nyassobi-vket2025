using UnityEditor;
using UnityEngine;
using VketTools.Utilities;

namespace VketTools.Main
{
    public class Vket_InfoWindow : EditorWindow
    {
        private const string ReleaseNoteUrl = "https://event.vket.com/docs/2024winter/vkettools_release_notes";
        private VersionInfo versionInfo;

        [MenuItem("VketTools/Info")]
        public static void ShowInfoWindow()
        {
            EditorWindow window = GetWindow<Vket_InfoWindow>(false, "Info Window - VketTools -");
            window.minSize = new Vector2(368, 320);
            window.maxSize = new Vector2(368.184f, 320.184f);
            window.Show();
        }

        private void OnEnable()
        {
            versionInfo = AssetUtility.VersionInfoData;
        }

        private void OnGUI()
        {
            GUIStyle box = new GUIStyle(GUI.skin.box);
            GUIStyle l1 = new GUIStyle(GUI.skin.label);
            GUIStyle l2 = new GUIStyle(GUI.skin.label);
            GUIStyle l3 = new GUIStyle(GUI.skin.label);

            l1.fontSize = 30;
            l1.fixedHeight = 35;
            l2.fontSize = 15;
            l2.fixedHeight = 25;
            l3.fontSize = 11;
            l3.fixedHeight = 16;

            GUIStyle l4 = new GUIStyle(l3);
            l4.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginArea(new Rect(10, 10, 348, 300), box);
            {
                Material mat = AssetUtility.GetMaterial("Texture.mat");
                EditorGUI.DrawPreviewTexture(new Rect(5, 10, 165, 60), AssetUtility.GetTexture2D("vket_logo.png"), mat, ScaleMode.ScaleToFit, 0);
                EditorGUI.DrawPreviewTexture(new Rect(115, 120, 125, 132), AssetUtility.GetTexture2D("vketsystem_logo.png"), mat, ScaleMode.ScaleToFit, 0);
                EditorGUI.DrawPreviewTexture(new Rect(225, 75, 184, 232), AssetUtility.GetTexture2D("vket-chan.png"), mat, ScaleMode.ScaleToFit, 0);
                EditorGUI.DrawPreviewTexture(new Rect(-70, 60, 226, 250), AssetUtility.GetTexture2D("vket-chan2.png"), mat, ScaleMode.ScaleToFit, 0);

                GUIContent content = new GUIContent("VketTools");
                GUI.Label(new Rect(184, 10, l1.CalcSize(content).x, l1.CalcSize(content).y), content, l1);
                var width = l1.CalcSize(content).x + 20;
                content = new GUIContent("Version:" + versionInfo.Version);
                GUI.Label(new Rect(184 - 10, 50, width, l4.CalcSize(content).y), content, l4);

                var releaseRect = new Rect(184 - 10, 50 + l4.fixedHeight, width, 0);
                var releaseContent = new GUIContent("Release notes");
                releaseRect.width = releaseRect.width == 0 ? l4.CalcSize(releaseContent).x : releaseRect.width;
                releaseRect.height = releaseRect.height == 0 ? l4.CalcSize(releaseContent).y : releaseRect.height;
                
                if (GUI.Button(releaseRect, releaseContent, GetReleaseNoteStyle(l4)))
                {
                    PopupWindow.Show(new Rect(Event.current.mousePosition, new Vector2(1000,1000)), new VketToolsReleaseNotePopup());
                }
                
                content = new GUIContent("Official Site:");
                GUI.Label(new Rect(5, 280, l3.CalcSize(content).x, l3.CalcSize(content).y), content, l3);
                UIUtility.GUILink("https://event.vket.com/", new Rect(3 + l3.CalcSize(content).x, 280, 0, 0), l3);
            }
            GUILayout.EndArea();
        }
        
        private static GUIStyle GetReleaseNoteStyle(GUIStyle style)
        {
            var ul = new GUIStyle(style);
            var state = new GUIStyleState
            {
                textColor = Color.green
            };
            ul.normal = state;
            ul.active = state;
            ul.focused = state;
            ul.hover = state;
            return ul;
        }
    }
}