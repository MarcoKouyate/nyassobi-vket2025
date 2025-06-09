using System.IO;
using VketTools.Utilities;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using VketTools.Networking;

namespace VketTools.PostProcessor
{
    public static class VketShaderPackageImporter
    {
        public const string DataBaseVketShaderName = "VketShaders";

        [FilePath("ScriptableSingleton/VketShaderPackageImporterSetting.dat", FilePathAttribute.Location.ProjectFolder)]
        public class ShaderPackageImporterSetting : ScriptableSingleton<ShaderPackageImporterSetting>
        {
            public string CurrentVersion;
        }

        public static async UniTask ImportVketShaders()
        {
            var versionInfo = AssetUtility.VersionInfoData;
            var packages = await ToolsApi.GetPackage(versionInfo.EventVersion, versionInfo.Type.ToString());
            if (packages == null || packages.packages == null)
                return;

            var shaderPackage = packages.packages.FirstOrDefault(p => p.package_name == DataBaseVketShaderName);
            if(shaderPackage == null)
                return;
            
            // インポート済みのバージョンが違う場合は更新
            if (ShaderPackageImporterSetting.instance.CurrentVersion != shaderPackage.package_version)
            {
                var directoryPath = Path.GetTempPath();
                var saveFilePath = $"{directoryPath}/{shaderPackage.package_name}";
            
                await DownloadPackage(shaderPackage.download_url, saveFilePath);
                
                if (File.Exists(saveFilePath))
                {
                    AssetDatabase.ImportPackage(saveFilePath, false);
                    File.Delete(saveFilePath);
                    ShaderPackageImporterSetting.instance.CurrentVersion = shaderPackage.package_version;
                }
            }
        }

        static async UniTask DownloadPackage(string uri, string saveFilePath)
        {
            using var req = UnityWebRequest.Get(uri);
            await req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                await File.WriteAllBytesAsync(saveFilePath, req.downloadHandler.data);
            }
            else
            {
                Debug.LogError(req.error);
            }
        }
    }
}