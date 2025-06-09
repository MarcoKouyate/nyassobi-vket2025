#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using VitDeck.Validator;

namespace VketTools.Validator.RuleSets
{
    /// <summary>
    /// Vketフォーミュラブース
    /// </summary>
    public class Vket2025SummerFormulaWorldRuleSet : Vket2025SummerConceptWorldRuleSet
    {
        public override string RuleSetName => "Vket2025Summer - Formula";
        
        protected override ValidationLevel VketKartValidationLevel => ValidationLevel.ALLOW;
    }
}
#endif