#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
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
    /// テクスチャ使用制限に関するルール
    /// <summary>
    public class TextureLimitRule : BaseRule
    {
        readonly HashSet<string> excludedAssetGUIDs;
        readonly int? countLimit;
        readonly int? areaLimit;
        public TextureLimitRule(string name, string[] excludedAssetGUIDs, int? countLimit, int? areaLimit) : base(name)
        {
            this.excludedAssetGUIDs = new HashSet<string>(excludedAssetGUIDs);
            this.countLimit = countLimit;
            this.areaLimit = areaLimit;
        }

        protected override void Logic(ValidationTarget target)
        {
            // 入稿フォルダ内および出展物から参照されるすべてのアセットのパス
            var allAssetPaths = target.GetAllAssetPaths();
            var submitDirectory = target.GetBaseFolderPath();

            // 入稿フォルダ内に配置されているファイルのパス
            // GetFilesで取得したパス名にはバックスラッシュが混在するためスラッシュに変換
            var filePathsInSubmitDirectory = Directory
                .GetFiles(submitDirectory, "*", SearchOption.AllDirectories)
                .Select(x => x.Replace("\\", "/"));

            // 入稿フォルダ内のディレクトリのパス
            var folderPathInSubmitDirectory = Directory
                .GetDirectories(submitDirectory, "*", SearchOption.AllDirectories)
                .Select(x => x.Replace("\\", "/"));

            var texCount = 0;
            var pixCount = 0;
            var renderTexCount = 0;

            foreach (var assetPath in allAssetPaths.Distinct())
            {
                // 運営からの配布物を検証対象から除外
                if (excludedAssetGUIDs.Contains(AssetDatabase.AssetPathToGUID(assetPath)))
                    continue;
                if (assetPath.Contains("Lightmap"))
                    continue;
                if (assetPath.Contains("ReflectionProbe"))
                    continue;
                if (assetPath.StartsWith("Assets/VketAssets"))
                    continue;
                if (assetPath.StartsWith("Assets/VRChat Examples"))
                    continue;

                // テクスチャの計上
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex != null)
                {
                    texCount++;
                    pixCount += tex.width * tex.height;
                }

                // RenderTextureの計上
                var renderTex = AssetDatabase.LoadAssetAtPath<RenderTexture>(assetPath);
                if (renderTex != null)
                    renderTexCount++;
            }

            // テクスチャ枚数制限
            if (countLimit.HasValue && texCount > countLimit.Value)
            {
                var message = AssetUtility.GetValidator("TextureLimitRule.CountLimit", countLimit.Value);
                var solution = AssetUtility.GetValidator("TextureLimitRule.CountLimit.Solution");
                AddIssue(new Issue(null, IssueLevel.Error, message, solution));
            }

            // テクスチャ総解像度制限
            if (areaLimit.HasValue && pixCount > areaLimit.Value)
            {
                // TODO: 圧縮後のサイズを基準に計算するようになっているか？
                var message = AssetUtility.GetValidator("TextureLimitRule.PixelsLimit", areaLimit.Value);
                var solution = AssetUtility.GetValidator("TextureLimitRule.PixelsLimit.Solution");
                AddIssue(new Issue(null, IssueLevel.Error, message, solution));
            }
        }
    }
}
#endif