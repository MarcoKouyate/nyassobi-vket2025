#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator.RuleSets
{
    public class Vket2025SummerCrossPlatformItemRuleSet : VketItemRuleSetBase
    {
        public Vket2025SummerCrossPlatformItemRuleSet() : base(new VketItemOfficialAssetData())
        {
        }
        public override string RuleSetName => "Vket2025Summer - Item - CrossPlatform";
        
        protected override long FolderSizeLimit => 20 * MegaByte;

        protected override Vector3 BoothSizeLimit => new(1, 2, 1);

        protected override int MaterialUsesLimit => 0;
        
        public override IRule[] GetRules()
        {
            var rules = base.GetRules().ToList();
            rules.Add(new ShaderDeclarationMethodRule( /* "使用しているシェーダーの描画に問題が起こる可能性があります。" */
                AssetUtility.GetValidator("ShaderDeclarationMethodRule.Title")));
            
            // 処理のタイミング的に追加は要検討
            rules.Add(/* クロスプラットフォームのシェーダールール。 */new VketCrossPlatformShaderRule(AssetUtility.GetValidator("VketCrossPlatformShaderRule.Title"), GetAllowedShaderHashSet(), GetDisallowedShaderHashSet()));
            
            return rules.ToArray();
        }

        HashSet<(string shaderName, string guid, VketCrossPlatformShaderRule.CheckType checkType)> GetDisallowedShaderHashSet() => new()
        {
            ("ArxCharacterShaders/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("arktoon/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("_lil/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("Silent's Cel Shading/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("VRChat/Panosphere", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("Sunao Shader/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            (".poiyomi/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("Hidden/Locked/.poiyomi/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("Mochie/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
        };

        private HashSet<(string shaderName, string guid, VketCrossPlatformShaderRule.CheckType checkType)> GetAllowedShaderHashSet() => new()
        {
            ("VRChat/Mobile/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("Video/RealtimeEmissiveGamma", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("TextMeshPro/Mobile/Distance Field", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("MMS3/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("UnityChanToonShader/Mobile/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("UniGLTF/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("VRM/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("Noriben/DepthWater", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("Noriben/noribenQuestWaterCubemap", "", VketCrossPlatformShaderRule.CheckType.StartWith),
            ("UnlitWF/", "", VketCrossPlatformShaderRule.CheckType.StartWith),
        };
    }
}
#endif