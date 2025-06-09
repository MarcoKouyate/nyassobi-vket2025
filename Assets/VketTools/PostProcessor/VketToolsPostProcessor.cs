
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VketTools.PostProcessor
{
    public class VketToolsPostProcessor : AssetPostprocessor
    {
        private static readonly string VketToolsRootFolderGuid = "8680a0fe8e1365f4698937ac2ef31522";
        private const string PrefixName = "VketTools-";
        private const string DefineName = "VKET_TOOLS";
        private static readonly string UnityTimeLineVersion = "1.7.6";
        private static readonly string VitDeckCoreUrl = "https://github.com/VitDeck/VitDeck.git?path=Packages/com.vitdeck.core#v2.0.0-exp.5";
        private static readonly string VitDeckExhibitorUrl = "https://github.com/VitDeck/VitDeck.git?path=Packages/com.vitdeck.exhibitor#v2.0.0-exp.5";

        public override int GetPostprocessOrder()
        {
            return int.MaxValue;
        }
        
        /// <summary>
        /// Unity起動時orこのファイルのインポート時に初期化
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // UnityTimeLinePackageの更新
            //UpdateUnityTimeLineVersion();
            
            // VitDeckの導入
            //ImportVitDeckPackageSetting();
            
            // VketShaderPackのインポート
            VketShaderPackageImporter.ImportVketShaders().Forget();
                
            // TMPの必須アセット導入
            ImportTMPEssentialResources();
            
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        }

        private static void UpdateUnityTimeLineVersion()
        {
            string jsonFilePath = ReplaceLast(Application.dataPath, "Assets", "Packages/manifest.json");
            if (!File.Exists(jsonFilePath))
            {
                return;
            }
            string jsonString;
            using (var reader = new StreamReader(jsonFilePath))
            {
                jsonString = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                return;
            }
            
            var jsonObject = JObject.Parse(jsonString);
            bool isUpdate = false;
            
            if (jsonObject["dependencies"] is JObject dependencies)
            {
                if (!dependencies.ContainsKey("com.unity.timeline") ||
                    dependencies["com.unity.timeline"].Path != UnityTimeLineVersion)
                {
                    isUpdate = true;
                }
                dependencies["com.unity.timeline"] = UnityTimeLineVersion;
            }

            if (isUpdate)
            {
                using (var writer = new StreamWriter(jsonFilePath, false))
                {
                    writer.WriteLine(jsonObject.ToString());
                }
                AssetDatabase.Refresh();
            }
        }

        private static void ImportTMPEssentialResources()
        {
            if(!Directory.Exists("Assets/TextMesh Pro"))
                AssetDatabase.ImportPackage("Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage", false);
        }
        
        private static void ImportVitDeckPackageSetting()
        {
            string jsonFilePath = ReplaceLast(Application.dataPath, "Assets", "Packages/manifest.json");
            if (!File.Exists(jsonFilePath))
            {
                Debug.LogError("Packages/manifest.jsonが存在しません。VitDeckの導入に失敗しました。");
                return;
            }
            string jsonString;
            using (var reader = new StreamReader(jsonFilePath))
            {
                jsonString = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                Debug.LogError("Packages/manifest.jsonが存在しません。VitDeckの導入に失敗しました。");
                return;
            }
            
            var jsonObject = JObject.Parse(jsonString);
            bool isUpdate = false;
            
            if (jsonObject["dependencies"] is JObject dependencies)
            {
                if (!dependencies.ContainsKey("com.vitdeck.core") || !dependencies.ContainsKey("com.vitdeck.exhibitor") ||
                    dependencies["com.vitdeck.core"].Path != VitDeckCoreUrl ||
                    dependencies["com.vitdeck.exhibitor"].Path != VitDeckExhibitorUrl)
                {
                    isUpdate = true;
                }
                dependencies["com.vitdeck.core"] = VitDeckCoreUrl;
                dependencies["com.vitdeck.exhibitor"] = VitDeckExhibitorUrl;
            }

            if (isUpdate)
            {
                using (var writer = new StreamWriter(jsonFilePath, false))
                {
                    writer.WriteLine(jsonObject.ToString());
                }
                AssetDatabase.Refresh();
            }
        }
        
        /// <summary>
        /// // パッケージのインポートが完了した時に呼び出される
        /// </summary>
        /// <param name="packageName">パッケージ名</param>
        private static void OnImportPackageCompleted(string packageName)
        {

            if (packageName.Contains(PrefixName))
            {
#if !VKET_TOOLS
                AddSymbol(DefineName);
                
                // このタイミングでは言語ファイルが正しく読み込めないので直書き。
                var title = Application.systemLanguage == SystemLanguage.Japanese
                    ? "VketToolsがインポートされました。Unityを再起動します。"
                    : "VketTools has been imported. Restart Unity.";
                
                var message = Application.systemLanguage == SystemLanguage.Japanese
                    ? "※初回インポート時にUnityの再起動が必要です。"
                    : "※Unity needs to be restarted when importing for the first time.";

                EditorUtility.DisplayDialog(title, message, "OK");
                DelayRestartUnity();
#else
                // 自動更新時、VketToolsパネルが出なくなるので一度コンパイルを走らせる
                CompilationPipeline.RequestScriptCompilation();
#endif
            }
        }

        /// <summary>
        /// Unityの再起動
        /// </summary>
        private static void RestartUnity()
        {
            var projectPath = FileUtil.GetProjectRelativePath("");
            Process.Start(EditorApplication.applicationPath, $"-projectPath {projectPath}");
            EditorApplication.Exit(0);
        }
        
        private static void DelayRestartUnity()
        {
            string batPath = Path.Combine(Application.dataPath, "VketTools/Plugins/RestartUnity.bat").Replace("\\", "/");
            if (!File.Exists(batPath))
            {
                Debug.LogError("Not Found RestartUnity.bat: " + batPath);
                return;
            }
            string unityExe = EditorApplication.applicationPath.Replace("/", "\\");
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            
            int pid = Process.GetCurrentProcess().Id;
            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                Arguments = $"\"{unityExe}\" \"{projectPath}\" {pid}",
                UseShellExecute = true,
                CreateNoWindow = true
            });
            EditorApplication.Exit(0);
        }
        
        private static void KillAndRestartUnity()
        {
            string batPath = Path.Combine(Application.dataPath, "VketTools/Plugins/ForceKillAndRestartUnity.bat").Replace("\\", "/");
            if (!File.Exists(batPath))
            {
                Debug.LogError("Not Found ForceKillAndRestartUnity.bat: " + batPath);
                return;
            }
            string unityExe = EditorApplication.applicationPath.Replace("/", "\\");
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            
            int pid = Process.GetCurrentProcess().Id;
            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                Arguments = $"\"{unityExe}\" \"{projectPath}\" {pid}",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var vketToolsRootFolderPath = AssetDatabase.GUIDToAssetPath(VketToolsRootFolderGuid);
            foreach (var deletedAssetPath in deletedAssets)
            {
                if (deletedAssetPath == vketToolsRootFolderPath)
                {
                    DeleteSymbol(DefineName);
                    break;
                }
            }
        }

        /// <summary>
        /// シンボルの追加
        /// </summary>
        /// <param name="defineName">シンボル名</param>
        private static void AddSymbol(string defineName)
        {
            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var split = currentDefines.Split(';').ToList();
            if(!split.Contains(defineName))
                split.Add(defineName);
            
            var resultDefines = string.Join(";", split);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, resultDefines);
        }

        /// <summary>
        /// シンボルの削除
        /// </summary>
        /// <param name="defineName">シンボル名</param>
        private static void DeleteSymbol(string defineName)
        {
            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            var split = currentDefines.Split(';').ToList();
            if (split.Contains(defineName))
                split.Remove(defineName);
            
            var resultDefines = string.Join(";", split);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, resultDefines);
        }
        
        private static string ReplaceLast(string target, string oldValue, string newValue)
        {
            int startIndex = target.LastIndexOf(oldValue, StringComparison.Ordinal);

            if (startIndex == -1)
            {
                return target;
            }

            return target.Remove(startIndex, oldValue.Length).Insert(startIndex, newValue);
        }
    }
}