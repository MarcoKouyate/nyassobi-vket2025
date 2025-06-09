#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class VketCameraComponentRule : ComponentBaseRule<Camera>
    {
        public VketCameraComponentRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(Camera component)
        {
            DefaultDisabledLogic(component);
            NeedRenderTextureLogic(component);
        }

        private void DefaultDisabledLogic(Camera component)
        {
            if (component.enabled)
            {
                var message = AssetUtility.GetValidator("CameraComponentRule.DefaultDisabled");
                var solution = AssetUtility.GetValidator("CameraComponentRule.DefaultDisabled.Solution");

                // コンポーネントは初期状態でDisabledにする必要があります。
                AddIssue(new Issue(
                    component,
                    IssueLevel.Error,
                    message,
                    solution));
            }
        }

        private void NeedRenderTextureLogic(Camera component)
        {
            if (component.targetTexture == null)
            {
                var message = AssetUtility.GetValidator("CameraComponentRule.NeedRenderTexture");

                // TargetTextureには必ずRenderTextureを指定してください
                AddIssue(new Issue(
                    component,
                    IssueLevel.Error,
                    message));
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }
    }
}
#endif