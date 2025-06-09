#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 禁止リストの文字列が含まれるシェーダーを禁止するルール
    /// <summary>
    public class ShaderDeclarationMethodRule : BaseRule
    {
        static readonly string[] ShaderFilePatterns = {".shader",".cginc",".hlsl", ".glsl", ".lilcontainer" };
        private readonly string[] _shaderFilePatterns;
        static readonly string[] BlockList = { "#pragma geometry", "pragma.geom", "pragma.domain", "GrabPass" };
        private readonly HashSet<string> _blockList;
        public ShaderDeclarationMethodRule(string name, string[] blockList = null, string[] shaderFilePatterns = null) : base(name)
        {
            _blockList = new HashSet<string>(blockList ?? BlockList);
            _shaderFilePatterns = shaderFilePatterns ?? ShaderFilePatterns;
        }

        protected override void Logic(ValidationTarget target)
        {
            var shaderFilePaths = Directory.GetFiles(target.GetBaseFolderPath(), "*.*", SearchOption.AllDirectories)
                                           .Where(file => _shaderFilePatterns.Any(pattern => file.ToLower().EndsWith(pattern)))
                                           .Select(path => path.Replace("\\", "/"));

            foreach (var shaderPath in shaderFilePaths)
            {
                using StreamReader sr = new StreamReader(shaderPath);
                try
                {
                    string text = sr.ReadToEnd();
                    HashSet<string> errorList = new HashSet<string>();
                    foreach (var blockString in _blockList)
                    {
                        if (text.Contains(blockString))
                        {
                            errorList.Add(blockString);
                        }
                    }

                    if (errorList.Any())
                    {
                        AddIssue(new Issue(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shaderPath), IssueLevel.Info, 
                            /* "シェーダーの非推奨な定義を検知しました。対象:{0}, 文字列:{1}" */ 
                            AssetUtility.GetValidator("ShaderDeclarationMethodRule.Message", shaderPath, string.Join(", ", errorList)), 
                            /* このシェーダーは描画に問題が起こる可能性があります。入稿後、androidビルドでの確認を推奨します。 */
                            AssetUtility.GetValidator("ShaderDeclarationMethodRule.Solution")));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
}
#endif