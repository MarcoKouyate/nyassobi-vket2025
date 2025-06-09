#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using UnityEditor;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class VketStaticFlagRule : BaseRule
    {
        private bool _isItem;
        
        public VketStaticFlagRule(string name, bool isItem = false) : base(name)
        {
            _isItem = isItem;
        }

        protected override void Logic(ValidationTarget target)
        {
            var rootObjects = target.GetRootObjects();

            if (_isItem)
            {
                foreach(var rootObject in rootObjects)
                {
                    LogicForStaticRootItem(rootObject.transform);
                }
            }
            else
            {
                foreach(var rootObject in rootObjects)
                {
                    var staticRoot = rootObject.transform.Find("Static");
                    if (staticRoot == null)
                    {
                        continue;
                    }

                    LogicForStaticRoot(staticRoot);
                }
            }
        }
        
        private void LogicForStaticRootItem(Transform staticRoot)
        {
            var children = staticRoot.GetComponentsInChildren<Transform>(true);

            foreach(var child in children)
            {
                var gameObject = child.gameObject;
                // Static設定がされている場合
                if (gameObject.isStatic)
                {
                    AddIssue(new Issue(
                        gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("StaticFlagRule.ItemStaticSet"),
                        AssetUtility.GetValidator("StaticFlagRule.ItemStaticSet.Solution")));
                }
            }
        }

        private void LogicForStaticRoot(Transform staticRoot)
        {
            var children = staticRoot.GetComponentsInChildren<Transform>(true);

            foreach(var child in children)
            {
                var gameObject = child.gameObject;
                var flag = GameObjectUtility.GetStaticEditorFlags(gameObject);
                
                if ((flag & StaticEditorFlags.ContributeGI) != 0)
                {
                    foreach (var filter in gameObject.GetComponents<MeshFilter>())
                    {
                        if (filter == null) 
                            continue;
                            
                        var mesh = filter.sharedMesh;
                        if (mesh == null) // メッシュが設定されていない場合はチェック対象外
                            continue;
                            
                        if (mesh.uv2.Length != 0) // uv2があればLightmapとして利用できる為問題なし
                            continue;

                        var assetPath = AssetDatabase.GetAssetPath(mesh);
                        if (string.IsNullOrWhiteSpace(assetPath)) // 対象のメッシュがアセットでない
                        {
                            AddIssueForIndependentMeshWithoutUV2(filter);
                            continue;
                        }
                            
                        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer == null) // 対象のメッシュのimporterがない（モデルインポートでないメッシュアセット）
                        {
                            AddIssueForIndependentMeshWithoutUV2(filter);
                            continue;
                        }

                        if (!importer.generateSecondaryUV) // 対象のメッシュアセットのgenerateSecondaryUVが無効になっている
                        {
                            var message = AssetUtility.GetValidator("StaticFlagRule.LightmapStaticMeshAssetShouldGenerateLightmap");
                            var solution = AssetUtility.GetValidator("StaticFlagRule.LightmapStaticMeshAssetShouldGenerateLightmap.Solution");
                            var solutionURL = AssetUtility.GetValidator("StaticFlagRule.LightmapStaticMeshAssetShouldGenerateLightmap.SolutionURL");
                        
                            AddIssue(new Issue(filter, IssueLevel.Warning, message, solution, solutionURL));
                        }
                    }
                }
                
                if ((flag & StaticEditorFlags.OccluderStatic) != 0)
                {
                    AddIssue(new Issue(gameObject, IssueLevel.Error, /* "OccluderStaticを無効にして下さい。" */ AssetUtility.GetValidator("StaticFlagRule.OccluderStaticNotAllowed")));
                }
                
                if((flag & StaticEditorFlags.OccludeeStatic) == 0)
                {
                    AddIssue(new Issue(
                        gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("StaticFlagRule.OccludeeStaticNotSet"),
                        AssetUtility.GetValidator("StaticFlagRule.OccludeeStaticNotSet.Solution")));
                }
            }
        }

        private void AddIssueForIndependentMeshWithoutUV2(MeshFilter filter)
        {
            var message = AssetUtility.GetValidator("StaticFlagRule.LightmapStaticMeshShouldHaveUV2");
            var solution = AssetUtility.GetValidator("StaticFlagRule.LightmapStaticMeshShouldHaveUV2.Solution");
            var solutionURL = AssetUtility.GetValidator("StaticFlagRule.LightmapStaticMeshShouldHaveUV2.SolutionURL");

            AddIssue(new Issue(filter, IssueLevel.Warning, message, solution, solutionURL));
        }
    }
}
#endif