#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using VitDeck.Validator;

namespace VketTools.Validator.RuleSets
{
    public class Vket2025SummerFormulaItemRuleSet : Vket2025SummerItemRuleSet
    {
        public override string RuleSetName => "Vket2025Summer - Formula_Item";

        protected override ValidationLevel VketKartValidationLevel => ValidationLevel.ALLOW;
    }
}
#endif