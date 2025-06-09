#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;

namespace VketTools.Validator
{
    /// <summary>
    /// VRCObjectPoolを探してそれぞれバリデーションするためのクラス
    /// 詳細な規約はクラスを継承して ComponentLogic に記述する
    /// </summary>
    internal class BaseVRCObjectPoolRule : ComponentBaseRule<VRC.SDK3.Components.VRCObjectPool>
    {
        public BaseVRCObjectPoolRule(string name) : base(name)
        {
        }

        protected override void ComponentLogic(VRC.SDK3.Components.VRCObjectPool component)
        {

        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }

    }
}
#endif
