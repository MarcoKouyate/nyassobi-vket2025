using System.IO;
using UnityEditor;
using UnityEngine;

namespace VketTools.Utilities
{
    public static class NonAsciiPathChecker
    {
        private const string ALERT_DONE_KEY = "NonAsciiPathChecker.alertDone";
        private const string ALERT_DONE = "Done";
        private static bool ContainsNonAscii(string path) =>
            System.Text.Encoding.Default.GetByteCount(path) != path.Length;
        public static string GetProjectDirectory() =>
            Path.GetDirectoryName(Application.dataPath);

        //[InitializeOnLoadMethod]
        public static void CheckPath()
        {
            var projectDir = GetProjectDirectory();
            if (!ContainsNonAscii(projectDir)) return;

            var title = "パスにマルチバイト文字が含まれています";
            var message = $"現在のプロジェクトフォルダ {projectDir} は使用できません。"
                          + "日本語等のマルチバイト文字が含まれないようにしてください";
            Debug.LogError(message);

            // ビルドのたびに呼ばれるのでアラートは一回だけ出す。このとき static 変数の値は失われているのでEditorUserSettingsを使う
            if (EditorUserSettings.GetConfigValue(ALERT_DONE_KEY) == ALERT_DONE) return;
            EditorUtility.DisplayDialog(title, message, "OK");
            EditorUserSettings.SetConfigValue(ALERT_DONE_KEY, ALERT_DONE);
        }

        /// <summary>
        /// マルチバイト文字が含まれているかチェックする
        /// </summary>
        /// <returns>trueで日本語が含まれている</returns>
        public static bool IsErrorDataPath()
        {
            return ContainsNonAscii(GetProjectDirectory());
        }
        
    }
}