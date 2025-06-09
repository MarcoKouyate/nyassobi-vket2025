#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class ParticleSystemForceFieldMaxDistanceRule : BaseRule
    {
        private readonly Vector3 _boothSize;
        private readonly Vector3 _margin;

        /// <param name="boothSize">ブースの幅</param>
        /// <param name="height">ブースの高さ</param>
        /// <param name="margin">はみ出し許容距離</param>
        public ParticleSystemForceFieldMaxDistanceRule(string name, Vector3 boothSize, Vector3? margin = null) : base(name)
        {
            _boothSize = boothSize;
            _margin = margin ?? Vector3.zero;
        }

        protected override void Logic(ValidationTarget target)
        {
            var assets = target.GetScenes()[0].GetRootGameObjects();
            
            foreach (var asset in assets)
            {
                if (asset.name != "ReferenceObjects")
                {
                    FindParticleSystemForceField(asset);
                }
            }
        }

        private void FindParticleSystemForceField(GameObject obj)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var particleSystemForceField = obj.transform.GetChild(i).GetComponent<ParticleSystemForceField>();
                if (particleSystemForceField)
                {
                    var bounds = new Bounds(particleSystemForceField.transform.position, Vector3.one * (particleSystemForceField.endRange * 2f));
                    float x = Math.Abs(bounds.center.x) + bounds.size.x/2f;
                    float y = Math.Abs(bounds.center.y - _boothSize.y/2f) + bounds.size.y/2f;
                    float z = Math.Abs(bounds.center.z) + bounds.size.z/2f;
                    
                    if (x > (_boothSize.x + _margin.x)/2f || y >  (_boothSize.y + _margin.y)/2f || z > (_boothSize.z + _margin.z)/2f)
                    {
                        var limitSize = (_boothSize + _margin).ToString();
                        var solution = /* "ParticleSystemForceFieldの範囲は{0}以内に収まるようにしてください。対象={1}" */ AssetUtility.GetValidator("ParticleSystemForceFieldMaxDistanceRule.Solution", limitSize, particleSystemForceField.gameObject.name);
                        AddIssue(new Issue(
                            particleSystemForceField,
                            IssueLevel.Error,
                            solution));
                    }
                    //Debug.Log($"{x},{y},{z}");
                    //Debug.Log($"{(_boothSize.x + _margin.x)/2f},{(_boothSize.y + _margin.y)/2f},{(_boothSize.z + _margin.z)/2f}");
                }
                
                // 子オブジェクトを再帰的に呼び出す
                FindParticleSystemForceField(obj.transform.GetChild(i).gameObject);
            }
        }
    }
}
#endif