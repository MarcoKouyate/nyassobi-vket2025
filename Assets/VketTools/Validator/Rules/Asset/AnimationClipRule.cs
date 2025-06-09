#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class AnimationClipRule : BaseRule
    {
        public AnimationClipRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            var allAssetPaths = target.GetAllAssetPaths();
            var clips = allAssetPaths.Select(AssetDatabase.LoadAssetAtPath<AnimationClip>).Where(a => a != null).Distinct();
            foreach (var clip in clips)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.type == typeof(AudioSource) && binding.propertyName == "m_Enabled")
                    {
                        AddIssue(new Issue(clip, IssueLevel.Error, AssetUtility.GetValidator("AnimationClipRule.AudioEnable.Message"), AssetUtility.GetValidator("AnimationClipRule.AudioEnable.Solution")));
                    }
                }
            }
        }
    }
}
#endif