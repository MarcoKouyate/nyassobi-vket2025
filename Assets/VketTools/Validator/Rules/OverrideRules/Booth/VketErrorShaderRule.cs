#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// エラーシェーダーの検出ルール
    /// </summary>
    /// <remarks>シェーダーエラーの存在するオブジェクトを検出する。</remarks>
    public class VketErrorShaderRule : BaseRule
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルール名</param>
        public VketErrorShaderRule(string name) : base(name) { }

        protected override void Logic(ValidationTarget target)
        {
            foreach (var obj in target.GetAllObjects())
            {
                var renderer = obj.GetComponent<Renderer>();
                
                if (renderer == null)
                    continue;

                if(renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                {
                    continue;
                }
                
                var materials = renderer.sharedMaterials;

                foreach (var material in materials)
                {
                    if (!material)
                    {
                        AddIssue(new Issue(obj, IssueLevel.Error, AssetUtility.GetValidator("Booth.ErrorShaderRule.noMaterialError"), string.Empty, string.Empty));
                        continue;
                    }

                    if (material.shader.name == "Hidden/InternalErrorShader")
                    {
                        AddIssue(new Issue(obj, IssueLevel.Error, AssetUtility.GetValidator("Booth.ErrorShaderRule.shaderReferenceError"), string.Empty, string.Empty));
                        continue;
                    }

                    if (string.IsNullOrEmpty(material.shader.name))
                    {
                        AddIssue(new Issue(obj, IssueLevel.Error, AssetUtility.GetValidator("Booth.ErrorShaderRule.shaderError"), string.Empty, string.Empty));
                    }
                }
            }
        }
    }
}
#endif