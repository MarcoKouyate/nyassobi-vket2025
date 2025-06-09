#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;

namespace VketTools.Validator.RuleSets
{
    /// <summary>
    /// 特別製の超大型ブース
    /// </summary>
    public class Vket2025SummerHugeConceptWorldRuleSet : VketUdonRuleSetBase
    {
        public Vket2025SummerHugeConceptWorldRuleSet() : base(new Vket2025SummerOfficialAssetData())
        {
        }
        public override string RuleSetName => "Vket2025Summer - HugeConceptWorld";
        
        protected override long FolderSizeLimit => 500 * MegaByte;

        protected override Vector3 BoothSizeLimit => new(50, 50, 50);

        protected override int UdonBehaviourCountLimit => 100;

        protected override int VRCObjectSyncCountLimit => 50;

        protected override int VRCObjectPoolCountLimit => 0;

        protected override int VRCObjectPoolPoolLimit => 0;

        protected override int VRCPickupCountLimit => 50;

        protected override int UdonBehaviourSynchronizePositionCountLimit => 50;

        protected override int UdonScriptSyncedVariablesLimit => 50;

        protected override int MaterialUsesLimit => 0;

        protected override int LightmapCountLimit => 1;

        protected override int LightmapSizeLimit => 2048;

        protected override int VRCStationCountLimit => 20;

        protected override int ClothCountLimit => 1;

        // 実質無制限
        protected override int AnimatorCountLimit => 4096;

        // 実質無制限
        protected override int AudioSourceCountLimit => 4096;
        
        protected override float AudioSourceMaxDistance => 100;

        protected override int VketImageDownloaderUsesLimit => 1;

        protected override int VketStringDownloaderUsesLimit => 1;
        
        protected override int VketStarshipTreasureUsesLimit => 0;

        protected override int VketVideoPlayerUsesLimit => 1;
        
        protected override int VketMirrorUsesLimit => 1;

        protected override int CameraCountLimit => 4;

        protected override int RenderTextureCountLimit => 2;

        protected override Vector2 RenderTextureSizeLimit => new Vector2(2048, 2048);

        protected override bool AllowIsKinematic => true;

        protected override VketLightConfigRule.LightConfig ApprovedPointLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 60,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes: new[] { LightShadows.Soft, LightShadows.Hard });

        protected override VketLightConfigRule.LightConfig ApprovedSpotLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 60,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes: new[] { LightShadows.Soft, LightShadows.Hard });

        protected override VketLightConfigRule.LightConfig ApprovedAreaLightConfig
            => new(
                new[] { LightmapBakeType.Baked },
                minRange: 0, maxRange: 60,
                minIntensity: 0, maxIntensity: 10,
                minBounceIntensity: 0, maxBounceIntensity: 15,
                approvedShadowTypes:null,
                castShadows:true,
                new VketLightConfigRule.AreaLightConfig(
                    minAreaWidth: 0, maxAreaWidth: 50,
                    minAreaHeight: 0, maxAreaHeight: 50,
                    minAreaRadius: 0, maxAreaRadius: 25));

        protected override int AreaLightUsesLimit => 30;
        
        protected override int PickupObjectSyncUsesLimit => 50;

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