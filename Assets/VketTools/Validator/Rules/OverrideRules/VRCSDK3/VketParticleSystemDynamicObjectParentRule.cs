#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// ParticleSystemを含むオブジェクトは全てDynamicオブジェクトの階層下に入れてください
    /// </summary>
    internal class VketParticleSystemDynamicObjectParentRule : BaseRule
    {
        public VketParticleSystemDynamicObjectParentRule(string name) : base(name)
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

            var particleSystems = rootObject.transform.GetComponentsInChildren<ParticleSystem>(true);
            // ParticleSystemが無い場合は帰る
            if (particleSystems == null || particleSystems.Length == 0) return;

            // ParticleSystemがあるのにdynamicRootが無いのはエラー
            if (dynamicRoot == null)
            {
                AddIssue(new Issue(
                    rootObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketParticleSystemDynamicObjectParentRule.noDynamicObject"),
                    AssetUtility.GetValidator("VketParticleSystemDynamicObjectParentRule.noDynamicObject.Solution")
                ));
                return;
            }

            foreach (var particleSystem in particleSystems)
            {
                // ParticleSystem がDynamicの子かどうかの検証
                if (!particleSystem.transform.IsChildOf(dynamicRoot))
                {
                    AddIssue(new Issue(
                        particleSystem,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("VketParticleSystemDynamicObjectParentRule.IsNotChildOfDynamic", particleSystem.name),
                        AssetUtility.GetValidator("VketParticleSystemDynamicObjectParentRule.IsNotChildOfDynamic.Solution")
                    ));
                }
            }
        }
    }
}
#endif