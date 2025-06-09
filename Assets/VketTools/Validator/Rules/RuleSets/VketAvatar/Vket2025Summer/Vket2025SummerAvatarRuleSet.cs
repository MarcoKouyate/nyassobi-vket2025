#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;

namespace VketTools.Validator.RuleSets
{
    public class Vket2025SummerAvatarRuleSet : VketAvatarRuleSetBase
    {
        public Vket2025SummerAvatarRuleSet() : base(new VketAvatarOfficialAssetData()) { }
        
        public override string RuleSetName => "Vket2025Summer - Avatar";
        
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
    }
}
#endif