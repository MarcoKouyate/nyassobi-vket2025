#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEditor;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// アセットの最大パス長を制限するルール。
    /// </summary>
    public class VketAssetPathLengthRule : BaseRule
    {
        private readonly int limit;
        private readonly HashSet<string> excludedAssetGUIDs;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルール名</param>
        /// <param name="limit">パス長の上限</param>
        public VketAssetPathLengthRule(string name, string[] excludedAssetGUIDs, int limit = 180) : base(name)
        {
            this.limit = limit;
            this.excludedAssetGUIDs = new HashSet<string>(excludedAssetGUIDs);
        }

        protected override void Logic(ValidationTarget target)
        {
            var paths = target.GetAllAssetPaths();
            foreach (var path in paths)
            {
                if (excludedAssetGUIDs.Contains(AssetDatabase.AssetPathToGUID(path)))
                    continue;
                
                var excess = path.Length - limit;
                if (excess > 0)
                {
                    var referenceObject = AssetDatabase.LoadMainAssetAtPath(path);
                    var message = AssetUtility.GetValidator("AssetPathLengthRule.Overlength", limit, excess, path);
                    AddIssue(new Issue(referenceObject, IssueLevel.Error, message, string.Empty));
                }
            }
        }
    }
}
#endif