#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.SDK3.Video.Components.AVPro;

namespace VketTools.Validator
{
    internal class VRCAVProVideoPlayerRule : ComponentBaseRule<VRCAVProVideoPlayer>
    {
        public VRCAVProVideoPlayerRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(VRCAVProVideoPlayer component)
        {
            if (component.AutoPlay)
            {
                var message = /* "VRCAVProVideoPlayerはAutoPlayを無効にすること。" */
                    AssetUtility.GetValidator("VRCAVProVideoPlayerRule.EnabledAutoPlay");
                var solution = /* "AutoPlayを無効にしてください。" */ AssetUtility.GetValidator("VRCAVProVideoPlayerRule.EnabledAutoPlay.Solution");

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