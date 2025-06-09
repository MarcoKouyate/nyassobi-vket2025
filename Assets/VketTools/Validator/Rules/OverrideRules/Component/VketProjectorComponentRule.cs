#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class VketProjectorComponentRule : ComponentBaseRule<Projector>
    {
        public VketProjectorComponentRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(Projector component)
        {
            DefaultDisabledLogic(component);
        }

        private void DefaultDisabledLogic(Projector component)
        {

        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
            if (hasComponentObject.activeSelf)
            {
                var message = AssetUtility.GetValidator("VketProjectorComponentRule.DefaultDisActive");
                var solution = AssetUtility.GetValidator("VketProjectorComponentRule.DefaultDisActive.Solution");

                AddIssue(new Issue(
                    hasComponentObject,
                    IssueLevel.Error,
                    message,
                    solution));
            }
        }
    }
}
#endif