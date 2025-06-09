#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// ContributeGIにチェックが入っているオブジェクト(ScalesInLightmapの値が0のものは除く)の中で、ライトマップがオーバーラップ状態になっているものは入稿できない
    /// </summary>
    public class LightMapOverlapsRule : BaseRule
    {

        public LightMapOverlapsRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            var rootObjects = target.GetRootObjects();

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

        private void LogicForStaticRoot(Transform staticRoot)
        {
            var children = staticRoot.GetComponentsInChildren<Transform>(true);
            
            foreach(var child in children)
            {
                var gameObject = child.gameObject;
                var flag = GameObjectUtility.GetStaticEditorFlags(child.gameObject);
                
                if ((flag & StaticEditorFlags.ContributeGI) != 0)
                {
                    foreach (var renderer in gameObject.GetComponents<Renderer>())
                    {
                        if (renderer == null) 
                            continue;
                        
                        var lightmapScaleProp = new SerializedObject(renderer).FindProperty("m_ScaleInLightmap");
                        var lightmapScale = lightmapScaleProp.floatValue;

                        var layout = typeof(Lightmapping);
                        var method = layout.GetMethod("HasUVOverlaps", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new[] {typeof(Renderer)}, null);
                        if (method == null)
                        {
                            Debug.LogError("Not Found HasUVOverlaps Method");
                            return;
                        }
                        
                        if (lightmapScale != 0 && (bool)method.Invoke(null, new object[] {renderer}))
                        {
                            var message = AssetUtility.GetValidator("LightMapOverlapsRule.Message");
                            var solution = AssetUtility.GetValidator("LightMapOverlapsRule.LightmapStaticMeshAssetShouldGenerateLightmap.Solution");

                            // UV2展開していなかったりUV2の時点で重なっている場合はOverlap判定になるし、それらがベイク負荷の高いメッシュであるのは確か。
                            // しかし、問題メッシュ以外も検出してしまうのが実際のところなので、ErrorからWarningsに変える
                            AddIssue(new Issue(gameObject, IssueLevel.Warning, message, solution));
                        }
                    }
                }
            }
        }
    }
}
#endif