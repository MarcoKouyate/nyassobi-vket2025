#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    internal class ParticleSystemShapeModuleRule : ComponentBaseRule<ParticleSystem>
    {
        public ParticleSystemShapeModuleRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(ParticleSystem component)
        {
            var shapeModule = component.shape;
            switch (shapeModule.shapeType)
            {
                case ParticleSystemShapeType.MeshRenderer:
                {
                    AddIssue(new Issue(
                        component,
                        IssueLevel.Error,
                        /* "ParticleSystemのShapeモジュールのShapeはMeshRenderer以外を使用してください。" */
                        AssetUtility.GetValidator("ParticleSystemShapeModuleRule.ShapeErrorMessage")));
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
