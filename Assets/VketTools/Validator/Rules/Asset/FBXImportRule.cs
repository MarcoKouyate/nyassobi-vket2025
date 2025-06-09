#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class FBXImportRule : BaseRule
    {
        public FBXImportRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            var dependencePaths = EditorUtility.CollectDependencies(target.GetScenes()[0].GetRootGameObjects())
                                            .Select(AssetDatabase.GetAssetPath)
                                            .Where(path => !string.IsNullOrEmpty(path))
                                            .Distinct()
                                            .ToArray();
            
            var importers = AssetDatabase.FindAssets($"t:{nameof(GameObject)}", new[] { target.GetBaseFolderPath() })
                                     .Select(AssetDatabase.GUIDToAssetPath)
                                     .Select(AssetImporter.GetAtPath)
                                     .Select(importer => importer as ModelImporter)
                                     .Where(modelImporter => modelImporter != null);
            
            foreach (var modelImporter in importers)
            {
                // シーンに参照がある場合のみ判定
                if (dependencePaths.Contains(AssetDatabase.GetAssetPath(modelImporter)))
                {
                    if (modelImporter.materialLocation == ModelImporterMaterialLocation.External)
                    {
                        var message = /* "LocationはUse Embedded Materialsに設定すること" */ AssetUtility.GetValidator("FBXImportRule.NonExtract.Message");
                        var solution = /* "MaterialsタブのLocationをUse Embedded Materialsにしてください。" */ AssetUtility.GetValidator("FBXImportRule.NonExtract.Solution");

                        AddIssue(new Issue(modelImporter, IssueLevel.Error, message, solution, resolver: () =>
                        {
                            modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
                            modelImporter.SaveAndReimport();
                            return ResolverResult.Resolved("SUCCESS");
                        }));
                    }
                }
            }
        }
    }
}
#endif
