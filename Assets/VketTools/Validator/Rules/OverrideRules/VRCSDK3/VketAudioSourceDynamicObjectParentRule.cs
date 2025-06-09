#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// VRCSpatialAudioSourceを含むオブジェクトは全てDynamicオブジェクトの階層下に入れてください
    /// </summary>
    internal class VketAudioSourceDynamicObjectParentRule : BaseRule
    {
        public VketAudioSourceDynamicObjectParentRule(string name) : base(name)
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
            
            var audioSources = rootObject.transform.GetComponentsInChildren<AudioSource>(true);
            // AudioSourceが無い場合は帰る
            if (audioSources == null || audioSources.Length == 0) return;

            // AudioSourceがあるのにdynamicRootが無いのはエラー
            if (dynamicRoot == null)
            {
                AddIssue(new Issue(
                    rootObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketAudioSourceDynamicObjectParentRule.noDynamicObject"),
                    AssetUtility.GetValidator("VketAudioSourceDynamicObjectParentRule.noDynamicObject.Solution")
                ));
                return;
            }

            foreach (var audioSource in audioSources)
            {
                // AudioSource がDynamicの子かどうかの検証
                if (!audioSource.transform.IsChildOf(dynamicRoot))
                {
                    AddIssue(new Issue(
                        audioSource,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("VketAudioSourceDynamicObjectParentRule.IsNotChildOfDynamic", audioSource.name),
                        AssetUtility.GetValidator("VketAudioSourceDynamicObjectParentRule.IsNotChildOfDynamic.Solution")
                    ));
                }
            }
        }
    }
}
#endif