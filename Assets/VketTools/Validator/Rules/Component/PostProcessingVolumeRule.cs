#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    internal class PostProcessingVolumeRule : ComponentBaseRule<PostProcessVolume>
    {

        public PostProcessingVolumeRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(PostProcessVolume component)
        {
            if (component.isGlobal)
            {
                // IsGlobalはオフにすること
                AddIssue(new Issue(component, IssueLevel.Error, AssetUtility.GetValidator("PostProcessingRule.IsGlobal")));
            }

            PostProcessProfile profile = component.sharedProfile;

            if (profile != null)
            {
                if (profile.HasSettings<AmbientOcclusion>())
                {
                    // AmbientOcclusionは使用しないこと
                    AddIssue(new Issue(component, IssueLevel.Error, AssetUtility.GetValidator("PostProcessingRule.AmbientOcclusion")));
                }

                if (profile.HasSettings<ScreenSpaceReflections>())
                {
                    // ScreenSpaceReflectionsは使用しないこと
                    AddIssue(new Issue(component, IssueLevel.Error, AssetUtility.GetValidator("PostProcessingRule.ScreenSpaceReflections")));
                }

                if (profile.HasSettings<DepthOfField>())
                {
                    // DepthOfFieldは使用しないこと
                    AddIssue(new Issue(component, IssueLevel.Error, AssetUtility.GetValidator("PostProcessingRule.DepthOfField")));
                }

                if (profile.HasSettings<MotionBlur>())
                {
                    // MotionBlurは使用しないこと
                    AddIssue(new Issue(component, IssueLevel.Error, AssetUtility.GetValidator("PostProcessingRule.MotionBlur")));
                }

                if (profile.HasSettings<LensDistortion>())
                {
                    // LensDistortionは使用しないこと
                    AddIssue(new Issue(component, IssueLevel.Error, AssetUtility.GetValidator("PostProcessingRule.LensDistortion")));
                }
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }
    }
}
#endif