#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class ParticleSystemMaxDistanceRule : BaseRule
    {
        private Vector3 boothSize;
        private Vector3 margin;

        /// <param name="boothSize">ブースの幅（広い方？）</param>
        /// <param name="height">ブースの高さ</param>
        /// <param name="margin">はみ出し許容距離</param>
        public ParticleSystemMaxDistanceRule(string name, Vector3 boothSize, Vector3 margin) : base(name)
        {
            this.boothSize = boothSize;
            this.margin = margin;
        }

        protected override void Logic(ValidationTarget target)
        {
            var assets = target.GetScenes()[0].GetRootGameObjects();
            
            foreach (var asset in assets)
            {
                if (asset.name != "ReferenceObjects")
                {
                    FindParticleSystem(asset);
                }
            }
        }

        private void FindParticleSystem(GameObject obj)
        {
            var height = boothSize.y;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var particleSystemRenderer = obj.transform.GetChild(i).GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer)
                {
                    var bounds = particleSystemRenderer.bounds;
                    float x = Math.Abs(bounds.center.x * 2) + bounds.size.x - margin.x * 2;
                    float y = Math.Abs(bounds.center.y - height / 2) * 2 + bounds.size.y - margin.y * 2;
                    float z = Math.Abs(bounds.center.z * 2) + bounds.size.z - margin.z * 2;
                    
                    if (x > boothSize.x || y > height || z > boothSize.z)
                    {
                        // ParticleSystemのBounds(範囲)は{0}以内に収まるようにしてください。対象={1}
                        var limitSize = (boothSize + margin).ToString();
                        var solution = AssetUtility.GetValidator("ParticleSystemRule.MaxDistance.Solution", limitSize, particleSystemRenderer.gameObject.name);
                        AddIssue(new Issue(
                            particleSystemRenderer,
                            IssueLevel.Error,
                            solution));
                    }
                }
                
                // 子オブジェクトを再帰的に呼び出す
                FindParticleSystem(obj.transform.GetChild(i).gameObject);
            }
        }
    }
}
#endif