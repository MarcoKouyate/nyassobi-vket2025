#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;

namespace VketTools.Validator.RuleSets
{
    /// <summary>
    /// 大型ブース
    /// </summary>
    public class Vket2025SummerLargeConceptWorldRuleSet : VketUdonRuleSetBase
    {
        public Vket2025SummerLargeConceptWorldRuleSet() : base(new Vket2025SummerOfficialAssetData())
        {
        }
        public override string RuleSetName => "Vket2025Summer - LargeConceptWorld";
        
        protected override long FolderSizeLimit => 100 * MegaByte;

        protected override Vector3 BoothSizeLimit => new(10, 10, 10);

        protected override int UdonBehaviourCountLimit => 20;

        protected override int VRCObjectSyncCountLimit => 10;

        protected override int VRCObjectPoolCountLimit => 0;

        protected override int VRCObjectPoolPoolLimit => 0;

        protected override int VRCPickupCountLimit => 10;

        protected override int UdonBehaviourSynchronizePositionCountLimit => 10;

        protected override int UdonScriptSyncedVariablesLimit => 10;

        protected override int MaterialUsesLimit => 0;

        protected override int LightmapCountLimit => 2;

        protected override int LightmapSizeLimit => 512;

        protected override int VRCStationCountLimit => 4;

        protected override int ClothCountLimit => 1;

        protected override int AnimatorCountLimit => 50;

        protected override int AudioSourceCountLimit => 20;
        
        protected override float AudioSourceMaxDistance => 20;

        protected override int VketImageDownloaderUsesLimit => 1;

        protected override int VketStringDownloaderUsesLimit => 1;
        
        protected override int VketStarshipTreasureUsesLimit => 0;

        protected override int VketVideoPlayerUsesLimit => 1;
        
        protected override int VketMirrorUsesLimit => 1;

        protected override int CameraCountLimit => 2;

        protected override int RenderTextureCountLimit => 2;

        protected override Vector2 RenderTextureSizeLimit => new Vector2(1024, 1024);

        protected override bool AllowIsKinematic => true;

        protected override VketLightConfigRule.LightConfig ApprovedPointLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 12,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes: new[] { LightShadows.Soft, LightShadows.Hard });

        protected override VketLightConfigRule.LightConfig ApprovedSpotLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 12,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes: new[] { LightShadows.Soft, LightShadows.Hard });

        protected override VketLightConfigRule.LightConfig ApprovedAreaLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 12,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes:null,
                castShadows:true,
                new VketLightConfigRule.AreaLightConfig(
                    minAreaWidth: 0, maxAreaWidth: 10,
                    minAreaHeight: 0, maxAreaHeight: 10,
                    minAreaRadius: 0, maxAreaRadius: 5));

        protected override int AreaLightUsesLimit => 3;
        
        protected override int PickupObjectSyncUsesLimit => 10;

        protected override bool UdonInactiveRuleIsEnabled => false;

        protected override bool ExhibitStructureRuleIsEnabled => true;
        protected override bool ExhibitStructureRuleOnlyDynamic => false;

        protected override bool UdonDynamicObjectParentRuleIsEnabled => true;

        // public override IRule[] GetRules()
        // {
        //     var rules = base.GetRules().ToList();
        //     // rules.Add() 等行うならここで操作する。今回このルールセットでは追加がない
        //     return rules.ToArray();
        // }
    }
}
#endif