#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.SDK3.Components;

namespace VketTools.Validator
{
    /// <summary>
    /// VRCSpatialAudioSourceを含むオブジェクトは全てDynamicオブジェクトの階層下に入れてください
    /// </summary>
    internal class VketVRCSpatialAudioSourceDynamicObjectParentRule : BaseRule
    {
        public VketVRCSpatialAudioSourceDynamicObjectParentRule(string name) : base(name)
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

            var spatialAudioSources = rootObject.transform.GetComponentsInChildren<VRCSpatialAudioSource>(true);
            // VRCSpatialAudioSourceが無い場合は帰る
            if (spatialAudioSources == null || spatialAudioSources.Length == 0) return;

            // VRCSpatialAudioSourceがあるのにdynamicRootが無いのはエラー
            if (dynamicRoot == null)
            {
                AddIssue(new Issue(
                    rootObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("SpatialAudioDynamicObjectParentRule.noDynamicObject"),
                    AssetUtility.GetValidator("SpatialAudioDynamicObjectParentRule.noDynamicObject.Solution")
                ));
                return;
            }

            foreach (var spatialAudioSource in spatialAudioSources)
            {
                // VRCSpatialAudioSource がDynamicの子かどうかの検証
                if (!spatialAudioSource.transform.IsChildOf(dynamicRoot))
                {
                    AddIssue(new Issue(
                        spatialAudioSource,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("SpatialAudioDynamicObjectParentRule.IsNotChildOfDynamic", spatialAudioSource.name),
                        AssetUtility.GetValidator("SpatialAudioDynamicObjectParentRule.IsNotChildOfDynamic.Solution")
                    ));
                }
            }

        }

    }
}
#endif