#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Linq;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class RenderTextureMaxSizeRule : BaseRule
    {
        Vector2 _limitSize;

        public RenderTextureMaxSizeRule(string name, Vector2 limitSize) : base(name)
        {
            this._limitSize = limitSize;
        }

        protected override void Logic(ValidationTarget target)
        {
            // カメラに設定されているすべてのRenderTextureを取得する
            var cameraObjects = target.GetAllObjects().Select(o => o.GetComponent<Camera>()).Where(c => c != null);
            var renderTextures = cameraObjects.Select(c => c.targetTexture as RenderTexture).Where(r => r != null);

            foreach (var renderTexture in renderTextures)
            {
                // レンダーテクスチャのサイズが上限を超えていたらエラーを出す
                if (renderTexture.width > _limitSize.x || renderTexture.height > _limitSize.y)
                {
                    var message = AssetUtility.GetValidator("RenderTextureMaxSizeRule.Exceeded", _limitSize.x, _limitSize.y, renderTexture.width, renderTexture.height);
                    var solution = AssetUtility.GetValidator("RenderTextureMaxSizeRule.Exceeded.Solution");

                    AddIssue(new Issue(
                        renderTexture,
                        IssueLevel.Error,
                        message,
                        solution));
                }
            }
        }
    }
}
#endif