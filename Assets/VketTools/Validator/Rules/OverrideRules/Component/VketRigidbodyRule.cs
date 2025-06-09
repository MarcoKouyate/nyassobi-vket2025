#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    internal class VketRigidbodyRule : ComponentBaseRule<Rigidbody>
    {
        private bool _allowIsKinematic;

        public VketRigidbodyRule(string name,bool allowIsKinematic) : base(name)
        {
            _allowIsKinematic = allowIsKinematic;
        }

        protected override void ComponentLogic(Rigidbody component)
        {
            if (!_allowIsKinematic)
            {
                if (!component.isKinematic)
                {
                    AddIssue(new Issue(
                        component,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("RigidbodyRule.UseIsKinematic")));
                }
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }
    }
}
#endif