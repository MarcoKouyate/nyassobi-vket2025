#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 特定のLightの個数が制限を超えていることを検出するルール
    /// </summary>
    public class VketLightCountLimitRule : BaseRule
    {
        private VketLightType type;
        private int limit;

        public VketLightCountLimitRule(string name, VketLightType type, int limit) : base(name)
        {
            this.type = type;
            this.limit = limit;
        }

        protected override void Logic(ValidationTarget target)
        {
            var objs = target.GetAllObjects();

            var foundLights = new List<Light>();

            foreach (var obj in objs)
            {
                var light = obj.GetComponent<Light>();
                if (light != null && type.MatchesUnityLightType(light.type))
                {
                    foundLights.Add(light);
                }
            }

            if (foundLights.Count > limit)
            {
                var message = AssetUtility.GetValidator("LightCountLimitRule.Overuse", type, limit, foundLights.Count);
                var solution = AssetUtility.GetValidator("LightCountLimitRule.Overuse.Solution");

                foreach (var light in foundLights)
                {

                    AddIssue(new Issue(
                        light,
                        IssueLevel.Error,
                        message,
                        solution));
                }
            }
        }
    }
}
#endif