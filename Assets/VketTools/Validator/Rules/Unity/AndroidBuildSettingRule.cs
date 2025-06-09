#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEditor;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{

    public class AndroidBuildSettingRule : BaseRule
    {
        public AndroidBuildSettingRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            // Texture Compression
            if (EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC)
            {
                var message = AssetUtility.GetValidator("AndroidBuildSettingRule.TextureCompressionError.Message");
                var solution = AssetUtility.GetValidator("AndroidBuildSettingRule.TextureCompressionError.Solution");
                
                AddIssue(new Issue(null, IssueLevel.Error, message, solution, resolver:() =>
                {
                    EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                    return ResolverResult.Resolved("SUCCESS");
                }));
            }
        }
    }
}
#endif
