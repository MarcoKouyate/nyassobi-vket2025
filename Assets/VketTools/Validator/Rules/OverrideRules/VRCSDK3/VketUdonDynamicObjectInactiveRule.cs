#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 全てのUdonBehaviourオブジェクトの親であるDynamicオブジェクトは初期でInactive状態にしてください
    /// </summary>
    internal class VketUdonDynamicObjectInactiveRule : BaseRule
    {
        private bool isEnabled;
        public VketUdonDynamicObjectInactiveRule(string name,bool isEnabled) : base(name)
        {
            this.isEnabled = isEnabled;
        }

        protected override void Logic(ValidationTarget target)
        {
            if (isEnabled)
            {
                var rootObjects = target.GetRootObjects();

                foreach (var rootObject in rootObjects)
                {
                    LogicForRootObject(rootObject);
                }
            }
        }

        private void LogicForRootObject(GameObject rootObject)
        {
            GameObject dynamicRoot = null;

            foreach (Transform child in rootObject.transform)
            {
                if (child.name == "Dynamic" && dynamicRoot == null)
                {
                    dynamicRoot = child.gameObject;
                }
            }

            CheckIsActive("Dynamic", dynamicRoot, !isEnabled);
        }

        private void CheckIsActive(string instanceName, GameObject instance, bool isActive)
        {
            if (instance.activeSelf != isActive)
            {
                AddIssue(new Issue(
                    instance,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("UdonDynamicObjectInactiveRule.isActive", instanceName),
                    AssetUtility.GetValidator("UdonDynamicObjectInactiveRule.isActive.Solution", instanceName)
                ));
            }
        }
    }
}
#endif