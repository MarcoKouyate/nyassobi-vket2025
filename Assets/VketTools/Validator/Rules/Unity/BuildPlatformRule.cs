#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using VitDeck.Validator;
using VketTools.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VketTools.Validator
{
    public class BuildPlatformRule : BaseRule
    {
        public enum BuildPlatform
        {
            VRChat_Standalone,
            VRChat_Quest
        }

        BuildPlatform _buildTarget;

        public BuildPlatformRule(string name, BuildPlatform buildTarget) : base(name)
        {
            this._buildTarget = buildTarget;
        }

        protected override void Logic(ValidationTarget target)
        {
            var currentTarget = GetCurrentBuildTarget();
            if (currentTarget != _buildTarget)
            {
                var message = AssetUtility.GetValidator("BuildPlatformRule.InvalidBuildTarget", _buildTarget.ToString());
                var solution = AssetUtility.GetValidator("BuildPlatformRule.InvalidBuildTarget.Solution");

                AddIssue(new Issue(null, IssueLevel.Error, message, solution));
            }
        }

        BuildPlatform GetCurrentBuildTarget()
        {
#if UNITY_EDITOR
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case UnityEditor.BuildTarget.Android:
                    return BuildPlatform.VRChat_Quest;
                default:
                    return BuildPlatform.VRChat_Standalone;
            }
#else
            return BuildTarget.VRChat_Standalone;
#endif
        }
    }
}
#endif