#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// カスタムシェーダー制約
    /// <summary>
    public class VketCrossPlatformShaderRule : BaseRule
    {
        public enum CheckType
        {
            StartWith,
            Contains,
            Equals,
        }
        
        private readonly HashSet<string> _unauthorizedIDSet;
        private readonly HashSet<(string shaderName, string guid, CheckType checkType)> _allowedShaderDataHashSet;
        private readonly HashSet<(string shaderName, string guid, CheckType checkType)> _disallowedShaderDataHashSet;

        public VketCrossPlatformShaderRule(string name,
            HashSet<(string shaderName, string guid, CheckType checkType)> allowedShaderDataHashSet,
            HashSet<(string shaderName, string guid, CheckType checkType)> disallowedShaderDataHashSet
            ) : base(name)
        {
            _allowedShaderDataHashSet = allowedShaderDataHashSet;
            _disallowedShaderDataHashSet = disallowedShaderDataHashSet;
        }

        protected override void Logic(ValidationTarget target)
        {
            foreach (var obj in target.GetAllObjects())
            {
                foreach (var shader in GetShaders(obj))
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
                    
                    // ホワイトリストにあるものは無視して次のシェーダーへ
                    if (CheckShaderIncluded(shader, _allowedShaderDataHashSet))
                        continue;
                    
                    // ブラックリストに含まれている
                    if (CheckShaderIncluded(shader, _disallowedShaderDataHashSet))
                    {
                        AddIssue(new Issue(shader, IssueLevel.Error,
                            /* "使用できないシェーダー「{0}」が入稿フォルダに含まれています。" */
                            AssetUtility.GetValidator("VketCrossPlatformShaderRule.DisallowedShaderInFolder.Message", shader.name),
                            /* "入稿フォルダから対象のシェーダーを除いてください。" */
                            AssetUtility.GetValidator("VketCrossPlatformShaderRule.DisallowedShaderInFolder.Solution")));
                    }
                    // ホワイトリストにもブラックリストにも含まれていないシェーダー
                    else
                    {
                        AddIssue(new Issue(
                            shader,
                            IssueLevel.Warning,
                            AssetUtility.GetValidator("VketCrossPlatformShaderRule.UseCustomShader.Message", shader.name),
                            AssetUtility.GetValidator("VketCrossPlatformShaderRule.UseCustomShader.Solution")
                        ));
                    }
                }
            }
        }

        private bool CheckShaderIncluded(Shader shader, HashSet<(string shaderName, string guid, CheckType checkType)> shaderDataHashSet)
        {
            var shaderGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shader));
            foreach (var shaderData in shaderDataHashSet)
            {
                switch (shaderData.checkType)
                {
                    case CheckType.StartWith:
                        if (shader.name.StartsWith(shaderData.shaderName))
                        {
                            return true;
                        }
                        break;
                    case CheckType.Contains:
                        if (shader.name.Contains(shaderData.shaderName))
                        {
                            return true;
                        }
                        break;
                    case CheckType.Equals:
                        if (shader.name.Equals(shaderData.shaderName) && shaderGuid == shaderData.guid)
                        {
                            return true;
                        }
                        break;
                }
            }

            return false;
        }
        
        /// <summary>
        /// 引数に指定されたオブジェクトに使用されているシェーダーを取得
        /// </summary>
        /// <param name="gameObject">シェーダーを取得するオブジェクトを指定</param>
        /// <returns>使用しているシェーダー</returns>
        private IEnumerable<Shader> GetShaders(GameObject gameObject)
        {
            return gameObject.GetComponents<Renderer>()
                             .SelectMany(renderer => renderer.sharedMaterials)
                             .Where(material => material)
                             .Select(material => material.shader);
        }
    }
}
#endif