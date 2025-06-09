#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VketAssets.Utilities.Language.Runtime
{
    public static class LanguageLoader
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            OnChangeLanguage();
        }

        public static void OnChangeLanguage()
        {
            var settingsList = AssetDatabase.FindAssets("t:VketAssets.Utilities.Language.Runtime.LanguageSettings")
                                            .Select(LoadAssetAtGuid<LanguageSettings>)
                                            .ToList();
            
            T LoadAssetAtGuid<T>(string guid) where T : Object
            {
                return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (var settings in settingsList)
            {
                if (settings)
                {
                    settings.SetUpDictionary();
                }
            }
        }
    }
}
#endif