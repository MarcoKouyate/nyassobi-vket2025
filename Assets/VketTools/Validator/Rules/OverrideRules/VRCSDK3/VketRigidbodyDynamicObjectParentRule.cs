#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// VRCSpatialAudioSourceを含むオブジェクトは全てDynamicオブジェクトの階層下に入れてください
    /// </summary>
    internal class VketRigidbodyDynamicObjectParentRule : BaseRule
    {
        public VketRigidbodyDynamicObjectParentRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            var rootObjects = target.GetRootObjects();

            foreach (var rootObject in rootObjects)
            {
                LogicForRootObject(rootObject);
            }
        }

        private void LogicForRootObject(GameObject rootObject)
        {
            Transform dynamicRoot = null;

            foreach (Transform child in rootObject.transform)
            {
                if (child.name == "Dynamic" && dynamicRoot == null)
                {
                    dynamicRoot = child.gameObject.transform;
                }
            }

            var rbs = rootObject.transform.GetComponentsInChildren<Rigidbody>(true);
            // AudioSourceが無い場合は帰る
            if (rbs == null || rbs.Length == 0) return;

            // AudioSourceがあるのにdynamicRootが無いのはエラー
            if (dynamicRoot == null)
            {
                AddIssue(new Issue(
                    rootObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketRigidbodyDynamicObjectParentRule.noDynamicObject"),
                    AssetUtility.GetValidator("VketRigidbodyDynamicObjectParentRule.noDynamicObject.Solution")
                ));
                return;
            }

            foreach (var rb in rbs)
            {
                // AudioSource がDynamicの子かどうかの検証
                if (!rb.transform.IsChildOf(dynamicRoot))
                {
                    AddIssue(new Issue(
                        rb,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("VketRigidbodyDynamicObjectParentRule.IsNotChildOfDynamic", rb.name),
                        AssetUtility.GetValidator("VketRigidbodyDynamicObjectParentRule.IsNotChildOfDynamic.Solution")
                    ));
                }
            }
        }
    }
}
#endif