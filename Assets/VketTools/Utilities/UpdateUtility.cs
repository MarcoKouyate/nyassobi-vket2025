using UnityEditor;
#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using VitDeck.Main;
using VketTools.Networking;
#endif

namespace VketTools.Utilities
{
    public static class UpdateUtility
    {
        // 初回インポート時(VKET_TOOLS未定義時)にはチェックしないようにする
#if VKET_TOOLS
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            var versionInfo = AssetUtility.VersionInfoData;
            if (!Hiding.HidingUtil.DebugMode && versionInfo && versionInfo.Type != VersionInfo.PackageType.develop)
            {
                UpdateCheck();
            }
        }
#endif
        private static readonly bool IsUpdateCheck = true;
        
        public static bool UpdateCheck()
        {
            if(!IsUpdateCheck)
                return true;
#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
            VersionInfo versionInfo = AssetUtility.VersionInfoData;
            if (!versionInfo)
            {
                return false;
            }
            
            JsonReleaseInfo.FetchInfo(ToolsApi.GetReleaseInfoUri(versionInfo.EventVersion, versionInfo.Type.ToString()));
            string latestVersion = JsonReleaseInfo.GetVersion();
            if (string.IsNullOrEmpty(latestVersion))
                return true;
            
            if (latestVersion != versionInfo.Version)
            {
                if (!EditorUtility.DisplayDialog("Update", AssetUtility.GetMain("UpdateUtility.UpdateCheck.Message", latestVersion), AssetUtility.GetMain("Yes"), AssetUtility.GetMain("No")))
                {
                    return false;
                }

                VitDeck.Main.UpdateCheck.UpdatePackage(latestVersion);
                // UpdatePackage() の完了時点ではまだアップデートし終えていないので、後続の処理を止めたいので false を返す
                return false;
            }
#endif
            return true;
        }
    }
}