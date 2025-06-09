#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// VRCObjectSyncのAllowOwnershipTransferOnCollisionは必ずFalseにすること
    /// </summary>
    internal class VRCObjectSyncAllowOwnershipTransferOnCollisionIsFalseRule : BaseVRCObjectSyncRule
    {
        public VRCObjectSyncAllowOwnershipTransferOnCollisionIsFalseRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(VRC.SDK3.Components.VRCObjectSync component)
        {
            if (component.AllowCollisionOwnershipTransfer)
            {
                AddIssue(new Issue(
                        component, 
                        IssueLevel.Error, 
                        AssetUtility.GetValidator("VRCObjectSyncAllowOwnershipTransferOnCollisionIsFalseRule.Title")
                    )
                );
            }
        }
    }
}
#endif
