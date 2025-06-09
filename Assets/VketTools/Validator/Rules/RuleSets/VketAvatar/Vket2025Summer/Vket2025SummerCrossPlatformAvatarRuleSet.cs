#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator.RuleSets
{
    public class Vket2025SummerCrossPlatformAvatarRuleSet : VketAvatarRuleSetBase
    {
        public Vket2025SummerCrossPlatformAvatarRuleSet() : base(new VketAvatarOfficialAssetData()) { }
        
        public override string RuleSetName => "Vket2025Summer - Avatar - CrossPlatform";
        
        protected override long FolderSizeLimit => 100 * MegaByte;

        protected override Vector3 BoothSizeLimit => new(3, 3, 3);

        protected override int UdonBehaviourCountLimit => 0;

        protected override int VRCObjectSyncCountLimit => 0;

        protected override int VRCObjectPoolCountLimit => 0;

        protected override int VRCObjectPoolPoolLimit => 0;

        protected override int VRCPickupCountLimit => 0;

        protected override int UdonBehaviourSynchronizePositionCountLimit => 0;

        protected override int UdonScriptSyncedVariablesLimit => 0;

        protected override int MaterialUsesLimit => 0;

        protected override int LightmapCountLimit => 1;

        protected override int LightmapSizeLimit => 512;

        protected override int VRCStationCountLimit => 0;

        protected override int ClothCountLimit => 1;

        protected override int AnimatorCountLimit => 50;

        protected override int AudioSourceCountLimit => 0;
        
        protected override float AudioSourceMaxDistance => 10;

        protected override int VketImageDownloaderUsesLimit => 0;

        protected override int VketStringDownloaderUsesLimit => 0;
        
        protected override int VketStarshipTreasureUsesLimit => 0;

        protected override int VketVideoPlayerUsesLimit => 0;
        
        protected override int VketMirrorUsesLimit => 0;

        protected override int CameraCountLimit => 0;

        protected override int RenderTextureCountLimit => 0;

        protected override Vector2 RenderTextureSizeLimit => new Vector2(1024, 1024);

        protected override float RayCastLength => 10.0f;

        protected override bool AllowIsKinematic => false;

        protected override VketLightConfigRule.LightConfig ApprovedPointLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 7,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes: new[] { LightShadows.Soft, LightShadows.Hard });

        protected override VketLightConfigRule.LightConfig ApprovedSpotLightConfig
            => new VketLightConfigRule.LightConfig(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 7,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes: new[] { LightShadows.Soft, LightShadows.Hard });

        protected override VketLightConfigRule.LightConfig ApprovedAreaLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 7,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes:null,
                castShadows:true,
                new VketLightConfigRule.AreaLightConfig(
                    minAreaWidth: 0, maxAreaWidth: 5,
                    minAreaHeight: 0, maxAreaHeight: 5,
                    minAreaRadius: 0, maxAreaRadius: 2.5f));

        protected override int AreaLightUsesLimit => 3;
        
        protected override int PickupObjectSyncUsesLimit => 0;

        protected override bool UdonInactiveRuleIsEnabled => false;

        protected override bool ExhibitStructureRuleIsEnabled => true;

        protected override bool ExhibitStructureRuleOnlyDynamic => false;

        protected override bool UdonDynamicObjectParentRuleIsEnabled => true;

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