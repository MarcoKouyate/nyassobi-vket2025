using UnityEditor;
using UnityEditor.Build;

namespace VketTools.Utilities
{
    public class PlatformUtility : IActiveBuildTargetChanged
    {
        private static bool _changeFlg;
        
        public static void CheckAndSwitchPlatform(bool isQuest = false)
        {
            if(_changeFlg)
                return;
            
            if (!isQuest)
            {
                if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
                    return;

                EditorUtility.DisplayDialog("VketTools Info", "プラットフォームがStandaloneではありません。\nプラットフォームの切り替えを行います。", "OK");
                _changeFlg = true;
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            }
            else
            {
                if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    return;

                EditorUtility.DisplayDialog("VketTools Info", "プラットフォームがAndroidではありません。\nプラットフォームの切り替えを行います。", "OK");
                _changeFlg = true;
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
                EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
            }
        }
        
        public int callbackOrder => 0;

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            EditorUtility.DisplayDialog("VketTools Info", $"アクティブなプラットフォームは{newTarget}になりました。", "OK");
            _changeFlg = false;
        }
    }
}