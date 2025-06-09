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
    /// 使用可能なシェーダーをブラックリスト検証します。
    /// </summary>
    public class VketShaderBlacklistRule : BaseRule
    {
        private readonly HashSet<string> _disallowedShaderList;

        /// <param name="name">ルール名</param>
        /// <param name="disallowedShaderList">入稿を許可しないシェーダー名のリスト。ビルトインシェーダーは常に許可。</param>
        public VketShaderBlacklistRule(
            string name,
            IList<string> disallowedShaderList
        ) : base(name)
        {
            _disallowedShaderList = disallowedShaderList.ToHashSet();
        }

        protected override void Logic(ValidationTarget target)
        {
            // 入稿シェーダー
            var draftShaders = AssetDatabase.FindAssets("t:Shader", new[] {target.GetBaseFolderPath()})
                                       .Select(guid => AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid)));

            // Blackリストのシェーダーが入稿フォルダに含まれているか確認
            foreach (var shader in draftShaders)
            {
                if (_disallowedShaderList.Contains(shader.name))
                {
                    AddIssue(new Issue(shader, IssueLevel.Error,
                        /* "使用できないシェーダー「{0}」が入稿フォルダに含まれています。" */
                        AssetUtility.GetValidator("VketShaderBlacklistRule.DisallowedShaderInFolder", shader.name),
                        /* "入稿フォルダから対象のシェーダーを除いてください。" */
                        AssetUtility.GetValidator("VketShaderBlacklistRule.DisallowedShaderInFolder.Solution")));
                }
            }

            // Blackリストのシェーダーがマテリアルに使用されているか確認
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

                    if (_disallowedShaderList.Contains(shader.name))
                    {
                        AddIssue(new Issue(
                            gameObject,
                            IssueLevel.Error,
                            /* "使用できないシェーダー「{0}」がシーンに含まれています。" */ AssetUtility.GetValidator(
                                "VketShaderBlacklistRule.DisallowedShaderInGameObject",
                                shader.name),
                            /* "使用できないシェーダーのマテリアルを変更してください。" */
                            AssetUtility.GetValidator(
                                "VketShaderBlacklistRule.DisallowedShaderInGameObject.Solution")));
                    }
                }
            }
        }

        /// <summary>
        /// 指定されたオブジェクトが参照するシェーダーを取得します。
        /// </summary>
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