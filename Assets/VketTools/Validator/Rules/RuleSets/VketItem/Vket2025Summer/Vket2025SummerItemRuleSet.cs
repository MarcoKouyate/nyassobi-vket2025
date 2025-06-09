#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;

namespace VketTools.Validator.RuleSets
{
    public class Vket2025SummerItemRuleSet : VketItemRuleSetBase
    {
        public Vket2025SummerItemRuleSet() : base(new VketItemOfficialAssetData())
        {
        }
        public override string RuleSetName => "Vket2025Summer - Item";

        protected override long FolderSizeLimit => 20 * MegaByte;

        protected override Vector3 BoothSizeLimit => new(1, 2, 1);

        protected override int MaterialUsesLimit => 0;

        // public override IRule[] GetRules()
        // {
        //     var rules = base.GetRules().ToList();
        //     // rules.Add() 等行うならここで操作する。今回このルールセットでは追加がない
        //     return rules.ToArray();
        // }
    }
}
#endif