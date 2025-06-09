#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// アセット名の使用禁止文字を検出するルール
    /// </summary>
    /// <remarks>
    /// フォルダ名またはアセット名(拡張子含む)をチェックする。
    /// </remarks>
    public class VketAssetNamingRule : BaseRule
    {
        private readonly string permissionPattern;
        private readonly HashSet<string> excludedAssetGUIDs;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ルール名</param>
        /// <param name="permissionPattern">使用許可文字列の正規表現</param>
        public VketAssetNamingRule(string name, string[] excludedAssetGUIDs, string permissionPattern = "[\x21-\x7e ]+") : base(name)
        {
            this.permissionPattern = permissionPattern;
            this.excludedAssetGUIDs = new HashSet<string>(excludedAssetGUIDs);
        }

        protected override void Logic(ValidationTarget target)
        {
            var paths = target.GetAllAssetPaths();
            var matchPattern = string.Format("^{0}$", permissionPattern);
            var rootFolderPath = target.GetBaseFolderPath();

            foreach (var path in paths)
            {
                if (!path.StartsWith(rootFolderPath))
                {
                    continue;
                }
                
                if (excludedAssetGUIDs.Contains(AssetDatabase.AssetPathToGUID(path)))
                {
                    continue;
                }

                var assetName = Path.GetFileName(path);
                if (!Regex.IsMatch(assetName, matchPattern))
                {
                    string prohibition = GetProhibitionPattern(assetName, permissionPattern);
                    var reference = AssetDatabase.LoadMainAssetAtPath(path);
                    var message = AssetUtility.GetValidator("AssetNamingRule.UnauthorizedTextDetected", path, prohibition);
                    AddIssue(new Issue(reference, IssueLevel.Error, message, string.Empty));
                }
            }
        }

        private string GetProhibitionPattern(string assetName, string permissionPattern)
        {
            string prohibition = "";
            foreach (char c in assetName)
            {
                if (Regex.IsMatch(c.ToString(), permissionPattern))
                    continue;
                else
                    prohibition += c;
            }

            return prohibition;
        }
    }
}
#endif
