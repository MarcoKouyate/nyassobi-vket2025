#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class VketAssetTypeLimitRule : BaseRule
    {
        private readonly System.Type type;
        private readonly int limit;
        private readonly HashSet<string> excludedAssetGUIDs;

        public VketAssetTypeLimitRule(string name, System.Type type, int limit, string[] excludedAssetGUIDs) : base(name)
        {
            this.type = type;
            this.limit = limit;
            this.excludedAssetGUIDs = new HashSet<string>(excludedAssetGUIDs);
        }

        protected override void Logic(ValidationTarget target)
        {
            // 設定でLimitが0の場合はチェックしない
            if(limit == 0)
                return;
            
            var assets = target.GetAllAssets();

            List<Object> foundAssets = new List<Object>();

            foreach (var asset in assets)
            {
                if (asset.GetType() == type &&
                    !excludedAssetGUIDs.Contains(GetGUID(asset)))
                {
                    foundAssets.Add(asset);
                }
            }

            if (foundAssets.Count > limit)
            {
                var message = AssetUtility.GetValidator("AssetTypeLimitRule.Overuse", type.Name, limit,
                    foundAssets.Count);
                var solution = AssetUtility.GetValidator("AssetTypeLimitRule.Overuse.Solution", type.Name);

                AddIssue(new Issue(
                    null,
                    IssueLevel.Error,
                    message,
                    solution));
            }
        }

        private static string GetGUID(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }
    }
}
#endif
