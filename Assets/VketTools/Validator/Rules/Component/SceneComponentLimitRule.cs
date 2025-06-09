#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class SceneComponentLimitRule : BaseRule
    {
        private readonly System.Type type;
        private int count;
        private readonly int limit;
        private readonly HashSet<string> excludedAssetGUIDs;

        public SceneComponentLimitRule(string name, System.Type type, int limit, string[] excludedAssetGUIDs) : base(name)
        {
            this.type = type;
            this.limit = limit;
            this.excludedAssetGUIDs = new HashSet<string>(excludedAssetGUIDs);
        }

        protected override void Logic(ValidationTarget target)
        {
            var assets = target.GetScenes()[0].GetRootGameObjects();

            count = 0;
            
            foreach (var asset in assets)
            {
                if (asset.name != "ReferenceObjects")
                {
                    FindComponent(asset);
                }
            }
            
            if (count > limit)
            {
                var message = AssetUtility.GetValidator("SceneComponentLimitRule.Overuse", type.Name, limit, count);
                var solution = AssetUtility.GetValidator("SceneComponentLimitRule.Overuse.Solution", type.Name);

                AddIssue(new Issue(
                    null,
                    IssueLevel.Error,
                    message,
                    solution));
            }
        }

        private void FindComponent(GameObject obj)
        {

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj.transform.GetChild(i).gameObject);
                if (excludedAssetGUIDs.Contains(GetGUID(prefab)))
                {
                    continue;
                }

                var components = obj.transform.GetChild(i).gameObject.GetComponents(type);
                count += components.Length;
                
                FindComponent(obj.transform.GetChild(i).gameObject);
            }

        }

        private static string GetGUID(Object asset)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
        }

    }
}
#endif