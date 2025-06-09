#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    internal class AudioSourceRule : ComponentBaseRule<AudioSource>
    {
        public AudioSourceRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(AudioSource component)
        {

        }


        private void DefaultDisabledLogic(AudioSource component)
        {
            if (component.enabled)
            {
                var message = AssetUtility.GetValidator("AudioSourceRule.DefaultDisabled");
                var solution = AssetUtility.GetValidator("AudioSourceRule.DefaultDisabled.Solution");

                AddIssue(new Issue(
                    component,
                    IssueLevel.Error,
                    message,
                    solution));
            }
        }


        private void SpatialBlendCheckLogic(AudioSource component)
        {
            if (component.spatialBlend != 1.0f)
            {
                AddIssue(new Issue(component,IssueLevel.Error,AssetUtility.GetValidator("AudioSourceRuleRule.SpatialBlend")));
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
            var audioSource = hasComponentObject.GetComponent<AudioSource>();
            DefaultDisabledLogic(audioSource);
            
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(hasComponentObject);
            if (prefab != null)
            {
                if (AssetDatabase.GetAssetPath(prefab).StartsWith("Assets/VketAssets/"))
                {
                    return;
                }
            }
            SpatialBlendCheckLogic(audioSource);
        }
    }
}
#endif