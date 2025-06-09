#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// VRCObjectPoolのPoolに登録するオブジェクトはn個以内にすること
    /// </summary>
    internal class VRCObjectPoolPoolLimitRule : BaseVRCObjectPoolRule
    {
        private readonly int limit;

        public VRCObjectPoolPoolLimitRule(string name,int limit) : base(name)
        {
            this.limit = limit;
        }

        protected override void ComponentLogic(VRC.SDK3.Components.VRCObjectPool component)
        {
            if (component.Pool.Length > limit)
            {
                AddIssue(new Issue(
                        component, 
                        IssueLevel.Error,
                        AssetUtility.GetValidator("VRCObjectPoolPoolLimitRule.Overuse", limit, component.Pool.Length),
                        AssetUtility.GetValidator("VRCObjectPoolPoolLimitRule.Overuse.Solution")
                    )
                );
            }
        }
    }
}
#endif
