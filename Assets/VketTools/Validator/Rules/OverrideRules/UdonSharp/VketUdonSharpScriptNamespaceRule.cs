#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using UnityEditor;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 特定ディレクトリ /UdonScripts/ 以下の MonoScript(*.cs) はUdonSharpのスクリプトと見なす
    /// UdonSharpのスクリプトは UdonSharpBehaviour を継承していなければならない
    /// U#においては、全てのクラスは運営よりブース毎に指定するnamespaceに所属させてください
    /// </summary>
    public class VketUdonSharpScriptNamespaceRule : BaseRule
    {
        // TODO: VitDeckは"UdonScript/"なので合わせる場合は削除可能
        private const string _scriptAssetPath = "UdonScripts/";
        private const string _baseClassName = "UdonSharp.UdonSharpBehaviour";
        private readonly string namespaceString;
        public VketUdonSharpScriptNamespaceRule(string name, string namespaceString) : base(name)
        {
            this.namespaceString = namespaceString;
        }

        protected override void Logic(ValidationTarget target)
        {
            var rootObjects = target.GetRootObjects();
            var rootPath = target.GetBaseFolderPath();
            var assetObjects = target.GetAllAssets();
            var assetPaths = target.GetAllAssetPaths();
            for (var i = 0; i < assetObjects.Length; i++)
            {
                var asset = assetObjects[i];
                var path = assetPaths[i];
                if (!(path.IndexOf(rootPath, StringComparison.Ordinal) == 0 && path.Length > rootPath.Length))
                {
                    continue;
                }
                var localPath = path.Substring(rootPath.Length + 1);
                // UdonScripts以下のみ探索
                if (localPath.IndexOf(_scriptAssetPath, StringComparison.Ordinal) == 0 &&  asset is MonoScript src)
                {
                    // U# スクリプトは UdonSharpBehaviour を直接継承していなければならない
                    Type srcType = src.GetClass();
                    var baseType = srcType.UnderlyingSystemType.BaseType; 
                    if (baseType == null || baseType.FullName != _baseClassName)
                    {
                        AddIssue(new Issue(asset, IssueLevel.Error, AssetUtility.GetValidator("UdonSharpScriptNamespaceRule.NotUdonSharp", src.GetClass())));
                    }
                    // U# スクリプトは 正しい名前空間で定義されなければならない
                    if (srcType.Namespace == null || srcType.Namespace.IndexOf(namespaceString, StringComparison.Ordinal) == -1)
                    {
                        AddIssue(
                            new Issue(
                                asset, 
                                IssueLevel.Error, 
                                AssetUtility.GetValidator("UdonSharpScriptNamespaceRule.InvalidNamespace"),
                                AssetUtility.GetValidator("UdonSharpScriptNamespaceRule.InvalidNamespace.solution", src.GetClass())
                                )
                            );
                    }
                }
            }
        }
    }
}
#endif
