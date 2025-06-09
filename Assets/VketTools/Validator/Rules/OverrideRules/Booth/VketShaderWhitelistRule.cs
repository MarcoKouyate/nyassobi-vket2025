#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 使用可能なシェーダーをホワイトリスト検証します。
    /// </summary>
    public class VketShaderWhitelistRule : BaseRule
    {
        private readonly IDictionary<string, string> shaderNameGUIDPairs;
        private readonly string[] shaderName;
        private readonly string solution;
        private readonly string solutionURL;

        /// <param name="name">ルール名</param>
        /// <param name="shaderNameGUIDPairs">キーにシェーダー名、値にシェーダーのGUIDを持つ連想配列。ビルトインシェーダーは常に許可。</param>
        /// <param name="shaderName">許可するShaderのパスを指定。このパスで始まる全てのシェーダーは使用を許可される。</param>
        public VketShaderWhitelistRule(
            string name,
            IDictionary<string, string> shaderNameGUIDPairs,
            string[] shaderName,
            string solution = "",
            string solutionURL = ""
        ) : base(name)
        {
            this.shaderNameGUIDPairs = shaderNameGUIDPairs;
            this.solution = solution;
            this.solutionURL = solutionURL;
            this.shaderName = shaderName;
        }

        protected override void Logic(ValidationTarget target)
        {
            foreach (var gameObject in target.GetAllObjects())
            {
                foreach (var shader in GetShaders(gameObject))
                {
                    if (shader.name == "Hidden/InternalErrorShader")
                    {
                        continue;
                    }

                    var path = AssetDatabase.GetAssetPath(Shader.Find(shader.name));

                    if (path == "Resources/unity_builtin_extra" && shader.name != "Particles/Standard Unlit")
                    {
                        continue;
                    }

                    bool IsAllowShaderPath()
                    {
                        foreach (string s in shaderName)
                        {
                            if (shader.name.StartsWith(s))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                    
                    if(IsAllowShaderPath())
                        continue;

                    // 許可されていないシェーダーを検知
                    if (!shaderNameGUIDPairs.ContainsKey(shader.name))
                    {
                        AddIssue(new Issue(
                            gameObject,
                            IssueLevel.Error,
                            AssetUtility.GetValidator("Booth.ShaderWhiteListRule.DisallowedShader", shader.name),
                            solution,
                            solutionURL
                        ));
                        continue;
                    }

                    // 許可されているシェーダーのGUIDが変更されていないかチェック
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    if (guid != shaderNameGUIDPairs[shader.name])
                    {
                        AddIssue(new Issue(gameObject, IssueLevel.Error,
                            AssetUtility.GetValidator("Booth.ShaderWhiteListRule.MismatchGUID", shader.name, guid, shaderNameGUIDPairs[shader.name]),
                            solution, solutionURL));
                    }
                }
            }
        }

        /// <summary>
        /// 指定されたオブジェクトが参照するシェーダーを取得します。
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private IEnumerable<Shader> GetShaders(GameObject gameObject)
        {
            return gameObject.GetComponents<Renderer>()
                .SelectMany(renderer => renderer.sharedMaterials) // RendererのMaterialを列挙
                .Where(material => material) // nullを排除
                .Select(material => material.shader); // 使用しているシェーダーを選択
        }
    }
}
#endif