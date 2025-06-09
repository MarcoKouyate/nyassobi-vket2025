#if VRC_SDK_VRCSDK3
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.Udon;

namespace VketTools.Validator
{
    public class UdonBehaviourProgramSourceRule : ComponentBaseRule<UdonBehaviour>
    {
        public UdonBehaviourProgramSourceRule(string name) : base(name)
        {
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }

        protected override void ComponentLogic(UdonBehaviour udonBehaviour)
        {
            if (udonBehaviour.programSource == null)
            {
                AddIssue(new Issue(
                    udonBehaviour,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("UdonBehaviourProgramSourceRule.Message"),
                    AssetUtility.GetValidator("UdonBehaviourProgramSourceRule.Solution")));
            }
        }
    }
}
#endif
