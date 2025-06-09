using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VketAssets.Utilities.Language.Runtime;

namespace VketTools.Utilities
{
    public static class AssetUtility
    {
        const string LanguageMainTag = "VketTools.Main";
        const string LanguageValidateTag = "VketTools.Validator";
        const string LanguageTutorialTag = "VketTools.Tutorial";
        private static VersionInfo _versionInfoData;
        private static LoginInfo _loginInfoData;
        private static EditorPlayInfo _editorPlayInfoData;
        private static SequenceInfo _sequenceInfoData;
        private static Texture2D _noImage;

        public static string GetMain(string messageID, params object[] args)
        {
            return LocalizedMessage.Get(messageID, LanguageMainTag, args);
        }
        
        public static string GetValidator(string messageID, params object[] args)
        {
            return LocalizedMessage.Get(messageID, LanguageValidateTag, args);
        }
        
        public static string GetTutorial(string messageID, params object[] args)
        {
            return LocalizedMessage.Get(messageID, LanguageTutorialTag, args);
        }

        public static VersionInfo VersionInfoData
        {
            get
            {
                if (_versionInfoData) return _versionInfoData;
                _versionInfoData = AssetLoad<VersionInfo>(ConfigFolderPath + "/VersionInfo.asset");
                return _versionInfoData;
            }
        }
        public static LoginInfo LoginInfoData
        {
            get
            {
                if (_loginInfoData) return _loginInfoData;
                _loginInfoData = AssetLoad<LoginInfo>(ConfigFolderPath + "/LoginInfo.asset");
                return _loginInfoData;
            }
        }
        public static EditorPlayInfo EditorPlayInfoData
        {
            get
            {
                if (_editorPlayInfoData) return _editorPlayInfoData;
                _editorPlayInfoData = AssetLoad<EditorPlayInfo>(ConfigFolderPath + "/EditorPlayInfo.asset");
                return _editorPlayInfoData;
            }
        }
        public static SequenceInfo SequenceInfoData
        {
            get
            {
                if (_sequenceInfoData) return _sequenceInfoData;
                _sequenceInfoData = AssetLoad<SequenceInfo>(ConfigFolderPath + "/SequenceInfo.asset");
                return _sequenceInfoData;
            }
        }

        public static void SaveAsset<T>(T t) where T : UnityEngine.Object
        {
            EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GetAssetPath(t)));
        }

        /// <summary>
        /// InfoScriptableObject群をエディタ上で編集不可に変更
        /// </summary>
        public static void SetHideFlags()
        {
            var loginInfo = LoginInfoData;
            var editorPlayInfo = EditorPlayInfoData;
            var versionInfo = VersionInfoData;
            var sequenceInfo = SequenceInfoData;
            bool needRefresh = false;
            if (loginInfo && (loginInfo.hideFlags & HideFlags.NotEditable) != HideFlags.NotEditable)
            {
                loginInfo.hideFlags |= HideFlags.NotEditable;
                EditorUtility.SetDirty(loginInfo);
                needRefresh = true;
            }
            if (editorPlayInfo && (editorPlayInfo.hideFlags & HideFlags.NotEditable) != HideFlags.NotEditable)
            {
                editorPlayInfo.hideFlags |= HideFlags.NotEditable;
                EditorUtility.SetDirty(editorPlayInfo);
                needRefresh = true;
            }
            if (versionInfo && (versionInfo.hideFlags & HideFlags.NotEditable) != HideFlags.NotEditable)
            {
                versionInfo.hideFlags |= HideFlags.NotEditable;
                EditorUtility.SetDirty(versionInfo);
                needRefresh = true;
            }
            if (sequenceInfo && (sequenceInfo.hideFlags & HideFlags.NotEditable) != HideFlags.NotEditable)
            {
                sequenceInfo.hideFlags |= HideFlags.NotEditable;
                EditorUtility.SetDirty(sequenceInfo);
                needRefresh = true;
            }
            if(needRefresh)
                AssetDatabase.Refresh();
        }

        public static GameObject SSCapturePrefab => AssetDatabase.LoadAssetAtPath<GameObject>(GetAssetFolderPath("Utilities/SS") + "/SSCapture.prefab");

        public static GameObject SSCameraPrefab => AssetDatabase.LoadAssetAtPath<GameObject>(GetAssetFolderPath("Utilities/SS") + "/SSCamera.prefab");

        private static string MainFolderPath => AssetDatabase.FindAssets("l:VketToolsMainFolder").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p != null);

        public static string ConfigFolderPath => GetAssetFolderPath("Config");

        private static string ImageFolderPath => GetAssetFolderPath("Main/Image");

        private static string SSFolderPath => GetFolderPath("Assets/Vket_SS");

        private static string MaterialFolderPath => GetAssetFolderPath("Material");
        
        public static Texture2D GetNoImage()
        {
            if (_noImage) return _noImage;
            _noImage = GetTexture2D("NoImage.png");
            return _noImage;
        }

        public static Texture2D GetTexture2D(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ImageFolderPath + "/" + fileName);
        }

        public static Material GetMaterial(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolderPath + "/" + fileName);
        }

        public static T AssetLoad<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (!asset)
            {
                // インポート持にファイルが存在してもLoadAssetAtPathで取得できない可能性がある。
                // その場合、空のデータで上書きする可能性が高いためここでファイルの存在チェックをしている。
                if (!File.Exists(assetPath))
                {
                    var scriptableObj = ScriptableObject.CreateInstance(typeof(T));
                    AssetDatabase.CreateAsset(scriptableObj, assetPath);
                }
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
            
            return asset;
        }

        public static string GetAssetFolderPath(string folder)
        {
            return GetFolderPath(MainFolderPath + "/" + folder);
        }

        private static string GetFolderPath(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
            return path;
        }

        public static async UniTask<string> SaveSS(Texture2D texture)
        {
            string path = SSFolderPath + "/{SHA-1}.png";
            await File.WriteAllBytesAsync(path, texture.EncodeToPNG());
            AssetDatabase.Refresh();
            if (!File.Exists(path))
                return null;

            string hash = "";
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                hash = BitConverter.ToString(SHA1.Create().ComputeHash(fs)).Replace("-", "").ToLower();
            string newPath = path.Replace("{SHA-1}", hash);
            if (!File.Exists(newPath))
                File.Move(path, newPath);
            else
                File.Delete(path);
            AssetDatabase.Refresh();
            return newPath;
        }

        public static float ForceRebuild(BuildTarget targetPlatform)
        {
            string sceneAssetPath = SceneManager.GetActiveScene().path;
            AssetBundleManifest result;
            try
            {
                string bundleName = "vketscene.vrcw";
                if (string.IsNullOrEmpty(sceneAssetPath))
                {
                    EditorUtility.DisplayDialog("Error", /* シーンファイルを開いてください。 */LocalizedMessage.Get("AssetUtility.ForceRebuild.OpenSceneFile", LanguageMainTag), "OK");
                    return -1;
                }

                string outPath = Application.temporaryCachePath;
                if (!Directory.Exists(outPath))
                {
                    Directory.CreateDirectory(outPath);
                }
                
                AssetBundleBuild[] builds = {
                    new()
                    {
                        assetNames = new[]{ sceneAssetPath },
                        assetBundleName = bundleName
                    }
                };

                if (targetPlatform == BuildTarget.Android)
                {
                    EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                }

                result = BuildPipeline.BuildAssetBundles(outPath, builds, BuildAssetBundleOptions.ForceRebuildAssetBundle, targetPlatform);
                AssetDatabase.RemoveUnusedAssetBundleNames();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return -1;
            }
            return result != null ? GetFileSize() : -1;
        }

        private static float GetFileSize()
        {
            try
            {
                string path = Application.temporaryCachePath + "/vketscene.vrcw";
                if (!File.Exists(path))
                {
                    return -1;
                }

                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.Length / 1024f / 1024f;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return -1;
            }
        }

        public static string TimestampToDateString(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
            var tzone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ja-JP");
                return TimeZoneInfo.ConvertTime(date, tzone).ToString("yyyy/MM/dd HH:mm");
            }
            else
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                return TimeZoneInfo.ConvertTime(date, tzone).ToString("MMM dd, yyyy HH:mm");
            }
        }
        
        public static string DateTimeToDateString(DateTime date)
        {
            var tzone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ja-JP");
                return TimeZoneInfo.ConvertTime(date, tzone).ToString("yyyy/MM/dd HH:mm");
            }
            else
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                return TimeZoneInfo.ConvertTime(date, tzone).ToString("MMM dd, yyyy HH:mm");
            }
        }

        /// <summary>
        /// NetworkingDLLから呼ばれる想定
        /// ログインができなかった場合に表示するテキストを返す。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetLabelString(int index)
        {
            switch (index)
            {
                case 51:
                    /* サーバーエラーが発生しています。 */
                    return LocalizedMessage.Get("NetworkUtility.ServerErrorMessage", LanguageMainTag);
                case 52:
                    /* ログインできませんでした。 */
                    return LocalizedMessage.Get("NetworkUtility.FailedLoginMessage", LanguageMainTag);
            }
            return "";
        }
    }
}