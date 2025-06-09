#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.SDK3.Video.Components.AVPro;

namespace VketTools.Validator
{
    internal class VRCAVProVideoScreenRule : ComponentBaseRule<VRCAVProVideoScreen>
    {
        public VRCAVProVideoScreenRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(VRCAVProVideoScreen component)
        {
            if (component.UseSharedMaterial)
            {
                var message = /* "VRCAVProVideoPlayerはUse Shared Materialを無効にすること" */ AssetUtility.GetValidator("VRCAVProVideoScreenRule.EnabledAutoPlay");
                var solution = /* "Use Shared Materialを無効にしてください。" */ AssetUtility.GetValidator("VRCAVProVideoScreenRule.EnabledAutoPlay.Solution");

                AddIssue(new Issue(
                    component,
                    IssueLevel.Error,
                    message,
                    solution));
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }
    }
}
#endif