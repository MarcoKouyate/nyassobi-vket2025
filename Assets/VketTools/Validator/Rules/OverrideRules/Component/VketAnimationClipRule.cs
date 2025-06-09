#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class VketAnimationClipRule : BaseRule
    {
        private readonly bool _allowMaterialAnimation;

        public VketAnimationClipRule(string name, bool allowMaterialAnimation = false) : base(name)
        {
            this._allowMaterialAnimation = allowMaterialAnimation;
        }

        protected override void Logic(ValidationTarget target)
        {
            foreach (var asset in target.GetAllAssets())
            {
                var clip = asset as AnimationClip;
                if (clip == null)
                {
                    continue;
                }

                LogicForAnimationClip(clip);
            }
        }

        private void LogicForAnimationClip(AnimationClip clip)
        {
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in objectBindings)
            {
                if (!_allowMaterialAnimation)
                {
                    var keyFrames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    foreach (var curve in keyFrames)
                    {
                        if (curve.value is Material)
                        {
                            AddIssue(new Issue(
                                clip,
                                IssueLevel.Error,
                                AssetUtility.GetValidator("AnimationClipRule.DontChangeMaterialInAnimation"),
                                AssetUtility.GetValidator("AnimationClipRule.DontChangeMaterialInAnimation.Solution")
                            ));
                        }
                        // エラーは1個出せば十分なのでbreakでループを抜ける
                        break;
                    }
                }

                LogicForBinding(clip, binding);
            }
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in curveBindings)
            {
                LogicForBinding(clip, binding);
            }
        }

        private void LogicForBinding(AnimationClip clip, EditorCurveBinding binding)
        {
            if (binding.path.Contains("../"))
            {
                AddIssue(new Issue(
                    clip,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("AnimationClipRule.DontAccessParentObject"),
                    AssetUtility.GetValidator("AnimationClipRule.DontAccessParentObject.Solution")
                    ));
            }
        }
    }
}
#endif