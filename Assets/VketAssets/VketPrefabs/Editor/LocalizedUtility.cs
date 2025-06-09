using VketAssets.Utilities.Language.Runtime;

namespace VketAssets.VketPrefabs.Editor
{
    public static class LocalizedUtility
    {
        public static string Get(string messageID, params object[] args)
        {
            return LocalizedMessage.Get(messageID, "VketPrefabs.Main", args);
        }
        
        public static string GetInspectorReadme(string messageID, params object[] args)
        {
            return "** ReadMe **\n" + LocalizedMessage.Get(messageID, "VketPrefabs.Inspector", args);
        }
        
        public static string GetInspector(string messageID, params object[] args)
        {
            return LocalizedMessage.Get(messageID, "VketPrefabs.Inspector", args);
        }
    }
}
