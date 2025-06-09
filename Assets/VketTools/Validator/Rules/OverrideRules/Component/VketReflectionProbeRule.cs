#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    internal class VketReflectionProbeRule : ComponentBaseRule<ReflectionProbe>
    {
        public VketReflectionProbeRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(ReflectionProbe component)
        {
            if (component.mode != UnityEngine.Rendering.ReflectionProbeMode.Custom && component.mode != UnityEngine.Rendering.ReflectionProbeMode.Baked)
            {
                AddIssue(new Issue(
                    component,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("ReflectionProbeRule.MustUseCustomTexture", component.mode)));
            }

            if (component.resolution > 128)
            {
                AddIssue(new Issue(
                    component,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("ReflectionProbeRule.Resolutin", component.resolution)));
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }
    }
}
#endif