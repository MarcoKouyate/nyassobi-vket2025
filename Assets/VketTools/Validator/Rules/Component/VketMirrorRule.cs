#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.SDK3.Components;

namespace VketTools.Validator
{
    public class VketMirrorRule : ComponentBaseRule<VRCMirrorReflection>
    {
        public VketMirrorRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(VRCMirrorReflection component)
        {
            var so = new SerializedObject(component);
            // VRC_MirrorReflection.AntialiasingSamples.X8は使用不可
            if (so.FindProperty("maximumAntialiasing").intValue == 8)
            {
                AddIssue(new Issue(
                    component.gameObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketMirrorRule.AntialiasingError")));
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
            if (hasComponentObject.layer != LayerMask.NameToLayer("Water"))
            {
                AddIssue(new Issue(
                    hasComponentObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketMirrorRule.LayerChanged")));
            }
        }
    }
}
#endif