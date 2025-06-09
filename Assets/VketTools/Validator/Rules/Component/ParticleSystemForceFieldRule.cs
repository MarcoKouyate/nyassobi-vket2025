#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    internal class ParticleSystemForceFieldRule : ComponentBaseRule<ParticleSystemForceField>
    {
        public ParticleSystemForceFieldRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(ParticleSystemForceField component)
        {
            switch (component.shape)
            {
                case ParticleSystemForceFieldShape.Box:
                case ParticleSystemForceFieldShape.Sphere:
                    break;
                default:
                {
                    AddIssue(new Issue(
                        component,
                        IssueLevel.Error,
                        /* "ParticleSystemForceFieldのShapeはBoxまたはSphereを使用してください。" */ AssetUtility.GetValidator("ParticleSystemForceFieldRule.ShapeError")));
                }
                    break;
            }
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }
    }
}
#endif