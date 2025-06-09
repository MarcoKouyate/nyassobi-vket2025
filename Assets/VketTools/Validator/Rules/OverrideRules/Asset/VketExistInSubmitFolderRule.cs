#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
	/// <summary>
	/// 配布物以外のすべてのオブジェクトが入稿フォルダ内にあるか検出するルール
	/// <summary>
    public class VketExistInSubmitFolderRule : BaseRule
    {
        // 入稿フォルダに含めることを許可されていないGUID
        private readonly HashSet<string> unauthorizedIDSet;
        private readonly VketTargetFinder targetFinder;
        public VketExistInSubmitFolderRule(string name, string[] guids, VketTargetFinder targetFinder) : base(name)
        {
            unauthorizedIDSet = new HashSet<string>(guids);
            this.targetFinder = targetFinder;
        }

        protected override void Logic(ValidationTarget target)
        {
            var referenceDictionary = targetFinder.ReferenceDictionary;
            
            // 除外する配布物アセットのパス
            var excludePaths = unauthorizedIDSet
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid));

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

            foreach (var assetPath in allAssetPaths)
            {
                // 運営からの配布物を検証対象から除外
                if (excludePaths.Contains(assetPath))
                    continue;

                // 入稿フォルダ内のサブディレクトリ(アセット含まない)を検証対象から除外
                if (folderPathInSubmitDirectory.Contains(assetPath) || assetPath == submitDirectory)
                    continue;

                // 入稿フォルダ内にパスがあるアセットなのか検証
                if (filePathsInSubmitDirectory.Contains(assetPath)) 
                    continue;

                // CyanTrigger: CyanTriggerSerialized アセットを除外
                // 検証の結果、入稿データに含めなくてもよさそうなので。
                if (assetPath.StartsWith("Assets/CyanTriggerSerialized/CyanTrigger_"))
                    continue;
                if (assetPath.StartsWith("Assets/CyanTrigger/CyanTriggerSerialized/CyanTrigger_"))
                    continue;

                //#if VRC_SDK_VRCSDK3
                //                // VRCSDK3: SerializedUdonPrograms の中を除外
                //                if(assetPath.StartsWith("Assets/SerializedUdonPrograms/"))
                //                    continue;
                //#endif

                // エラー対象である事が確定したためIssue発行
                var targetAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                referenceDictionary.Reverse.TryGetValue(targetAsset, out var referrerAssets);

                var referrerMessage = 
                    AssetUtility.GetValidator("ExistInSubmitFolderRule.HasOutOfPackageReference", assetPath);
                var referrerSolution =
                    AssetUtility.GetValidator("ExistInSubmitFolderRule.HasOutOfPackageReference.Solution");
                
                if (referrerAssets != null)
                {
                    foreach (var referrerAsset in referrerAssets)
                    {
                        AddIssue(new Issue(referrerAsset, IssueLevel.Error, referrerMessage, referrerSolution));
                    }
                }
                
                var message = AssetUtility.GetValidator("ExistInSubmitFolderRule.AssetOutOfPackage", assetPath);
                
                AddIssue(new Issue(targetAsset, IssueLevel.Error, message));
            }
        }
    }
}
#endif