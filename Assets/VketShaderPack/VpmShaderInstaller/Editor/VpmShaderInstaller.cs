using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace VketShaderPack.VpmShaderInstaller.Editor
{
    internal static class VpmShaderInstaller
    {
        private const string PackagePath = "Assets/VketShaderPack/VpmShaderInstaller/installer.unitypackage";
        private const string LilToonKey = "jp.lilxyzw.liltoon";
        private const string DependenceLilToonVersion = "1.9.0";
        private const string UnlitWfShaderKey = "jp.whiteflare.unlitwf";
        private const string DependenceUnlitWfVersion = "2.8.1";
        
        [InitializeOnLoadMethod]
        public static void InstallVpmShadersIfNecessary()
        {
            string jsonFilePath = ReplaceLast(Application.dataPath, "Assets", "Packages/vpm-manifest.json");
            if (!File.Exists(jsonFilePath))
            {
                Debug.LogError("vpm-manifest.jsonが存在しません。");
                return;
            }
            
            string jsonString;
            using (var reader = new StreamReader(jsonFilePath))
            {
                jsonString = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                Debug.LogError("vpm-manifest.jsonのデータが存在しません。");
                return;
            }
            
            var jsonObject = JObject.Parse(jsonString);
            if (jsonObject["dependencies"] is JObject dependencies)
            {
                bool installedLilToon = false;
                bool installedUnlitWfShader = false;
                if (dependencies.TryGetValue(LilToonKey, out var dependencyLilToon))
                {
                    if (dependencyLilToon is JObject lilObj)
                    {
                        Debug.Log(lilObj["version"].ToString());
                        installedLilToon = lilObj["version"].ToString() == DependenceLilToonVersion;
                    }
                        
                }
                if (dependencies.TryGetValue(UnlitWfShaderKey, out var dependencyUnlitWf))
                {
                    if (dependencyUnlitWf is JObject unlitWfObj)
                    {
                        Debug.Log(unlitWfObj["version"].ToString());
                        installedUnlitWfShader = unlitWfObj["version"].ToString() == DependenceUnlitWfVersion;
                    }
                        
                }

                if (!installedLilToon || !installedUnlitWfShader)
                {
                    AssetDatabase.ImportPackage(PackagePath, false);
                }
            }
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
