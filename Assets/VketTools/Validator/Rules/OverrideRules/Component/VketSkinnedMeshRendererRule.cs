#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    internal class VketSkinnedMeshRendererRule : ComponentBaseRule<SkinnedMeshRenderer>
    {
        public VketSkinnedMeshRendererRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(SkinnedMeshRenderer component)
        {
            if (component.updateWhenOffscreen)
            {
                AddIssue(new Issue(
                    component,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("SkinnedMeshRendererRule.MustTurnOffUpdateWhenOffscreen")));
            }

            if (component.sharedMaterials.Length == 0)
            {
                AddIssue(new Issue(
                       component,
                       IssueLevel.Error,
                       AssetUtility.GetValidator("SkinnedMeshRendererRule.MustAttachMaterial")));
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }
    }
}
#endif