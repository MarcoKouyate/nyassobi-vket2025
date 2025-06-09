#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 特定のLightの設定が制限に従っていることを検証するルール
    /// </summary>
    public class VketLightConfigRule : BaseRule
    {
        public const float NO_LIMIT = -1;

        private VketLightType type;
        private readonly LightmapBakeType[] approvedLightmapBakeTypes;
        private readonly float minRange;
        private readonly float maxRange;
        private readonly float minIntensity;
        private readonly float maxIntensity;
        
        /// <summary>
        /// InspectorはIndirectMultipller表記
        /// </summary>
        private readonly float minBounceIntensity;
        
        /// <summary>
        /// InspectorはIndirectMultipller表記
        /// </summary>
        private readonly float maxBounceIntensity;
        
        /// <summary>
        /// Point または Spotのみ
        /// </summary>
        private readonly LightShadows[] approvedShadowTypes;
        
        /// <summary>
        /// Areaのみ使用
        /// </summary>
        private readonly bool castShadows;
        
        private readonly AreaLightConfig areaLightConfig;

        private string bakeTypeListString;
        private string bakeShadowTypeListString;

        public VketLightConfigRule(
                string name,
                VketLightType type,
                LightmapBakeType[] approvedLightmapBakeTypes,
                float minRange = NO_LIMIT, float maxRange = NO_LIMIT,
                float minIntensity = NO_LIMIT, float maxIntensity = NO_LIMIT,
                float minBounceIntensity = NO_LIMIT, float maxBounceIntensity = NO_LIMIT,
                LightShadows[] approvedShadowTypes = null,
                bool castShadows = true,
                AreaLightConfig areaLightConfig = new AreaLightConfig()
        ) : base(name)
        {
            this.type = type;
            this.approvedLightmapBakeTypes = approvedLightmapBakeTypes;
            this.minRange = minRange;
            this.maxRange = maxRange;
            this.minIntensity = minIntensity;
            this.maxIntensity = maxIntensity;
            this.minBounceIntensity = minBounceIntensity;
            this.maxBounceIntensity = maxBounceIntensity;
            this.approvedShadowTypes = approvedShadowTypes;
            this.castShadows = castShadows;
            this.areaLightConfig = areaLightConfig;
        }

        public VketLightConfigRule(
            string name,
            VketLightType type,
            LightConfig approvedConfig
        ) : base(name)
        {
            this.type = type;
            this.approvedLightmapBakeTypes = approvedConfig.bakeTypes;
            this.minRange = approvedConfig.minRange;
            this.maxRange = approvedConfig.maxRange;
            this.minIntensity = approvedConfig.minIntensity;
            this.maxIntensity = approvedConfig.maxIntensity;
            this.minBounceIntensity = approvedConfig.minBounceIntensity;
            this.maxBounceIntensity = approvedConfig.maxBounceIntensity;
            this.approvedShadowTypes = approvedConfig.approvedShadowTypes;
            this.castShadows = approvedConfig.castShadows;
            this.areaLightConfig = approvedConfig.areaLightConfig;
        }

        protected override void Logic(ValidationTarget target)
        {
            var objs = target.GetAllObjects();

            bakeTypeListString = string.Join(", ", approvedLightmapBakeTypes.Select(x => x.ToString()).ToArray());
            
            // 許可されたShadowTypeを文字列でキャッシュ
            if (approvedShadowTypes != null)
            {
                bakeShadowTypeListString = string.Join(", ", approvedShadowTypes.Select(x => x.ToString()).ToArray());
            }

            foreach (var obj in objs)
            {
                var light = obj.GetComponent<Light>();
                if (light != null && type.MatchesUnityLightType(light.type))
                {
                    LogicForLight(light);

                    switch (light.type)
                    {
                        case UnityEngine.LightType.Directional:
                            break;
                        case UnityEngine.LightType.Spot:
                        case UnityEngine.LightType.Point:
                            ShadowTypeCheckLogic(light);
                            break;
                        case UnityEngine.LightType.Area:// == (Rectangle)
                            CastShadowsCheckLogic(light);
                            LogicForAreaLightRectangle(light);
                            break;
                        case UnityEngine.LightType.Disc:
                            CastShadowsCheckLogic(light);
                            LogicForAreaLightDisc(light);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private void LogicForLight(Light light)
        {
            if (approvedLightmapBakeTypes.Length <= 0)
            {
                AddIssue(new Issue(
                    light.gameObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("LightConfigRule.UnauthorizedLightType", light.type),
                    AssetUtility.GetValidator("LightConfigRule.UnauthorizedLightType.Solution")));

                return;
            }

            if (!approvedLightmapBakeTypes.Contains(light.lightmapBakeType))
            {
                AddIssue(new Issue(
                    light.gameObject, 
                    IssueLevel.Error,
                    AssetUtility.GetValidator("LightConfigRule.UnauthorizedLightMode", 
                        type, bakeTypeListString, light.lightmapBakeType),
                    AssetUtility.GetValidator("LightConfigRule.UnauthorizedLightMode.Solution", bakeTypeListString)
                    ));
            }

            if (minRange != NO_LIMIT && maxRange != NO_LIMIT)
            {
                // light.range だとエリアライトで取得される値が狂う（内部で areaSize も用いた何らかの計算が行われる）ので m_Range を見る
                var range = new SerializedObject(light).FindProperty("m_Range").floatValue;
                if (range < minRange || range > maxRange)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.OverRange", 
                            type, minRange, maxRange, range),
                        AssetUtility.GetValidator("LightConfigRule.OverRange.Solution")
                        ));
                }
            }

            if (minIntensity != NO_LIMIT && maxIntensity != NO_LIMIT)
            {
                if (light.intensity < minIntensity || light.intensity > maxIntensity)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.OverIntensity", 
                            type, minIntensity, maxIntensity, light.intensity),
                        AssetUtility.GetValidator("LightConfigRule.OverIntensity.Solution")
                        ));
                }
            }

            if (minBounceIntensity != NO_LIMIT && maxBounceIntensity != NO_LIMIT)
            {
                if (light.bounceIntensity < minBounceIntensity || light.bounceIntensity > maxBounceIntensity)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.OverIndirectMultiplier",
                            type, minBounceIntensity, maxBounceIntensity, light.bounceIntensity),
                        AssetUtility.GetValidator("LightConfigRule.OverIndirectMultiplier.Solution")
                        ));
                }
            }
        }
        
        private void LogicForAreaLightRectangle(Light light)
        {
            if (areaLightConfig.minAreaWidth != NO_LIMIT && areaLightConfig.maxAreaWidth != NO_LIMIT)
            {
                if (light.areaSize.x < areaLightConfig.minAreaWidth || light.areaSize.x > areaLightConfig.maxAreaWidth)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.OverAreaWidth",
                            areaLightConfig.minAreaWidth, areaLightConfig.maxAreaWidth, light.areaSize.x),
                        AssetUtility.GetValidator("LightConfigRule.OverAreaWidth.Solution")
                    ));
                }
            }

            if (areaLightConfig.minAreaHeight != NO_LIMIT && areaLightConfig.maxAreaHeight != NO_LIMIT)
            {
                if (light.areaSize.y < areaLightConfig.minAreaHeight || light.areaSize.y > areaLightConfig.maxAreaHeight)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.OverAreaHeight",
                            areaLightConfig.minAreaHeight, areaLightConfig.maxAreaHeight, light.areaSize.y),
                        AssetUtility.GetValidator("LightConfigRule.OverAreaHeight.Solution")
                    ));
                }
            }
        }

        private void LogicForAreaLightDisc(Light light)
        {
            if (areaLightConfig.minAreaRadius != NO_LIMIT && areaLightConfig.maxAreaRadius != NO_LIMIT)
            {
                // どうもログに出してみると radius は areaSize.x に格納されてる
                var radius = light.areaSize.x;
                if (radius < areaLightConfig.minAreaRadius || radius > areaLightConfig.maxAreaRadius)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.OverAreaRadius",
                            areaLightConfig.minAreaRadius, areaLightConfig.maxAreaRadius, radius),
                        AssetUtility.GetValidator("LightConfigRule.OverAreaRadius.Solution")
                    ));
                }
            }
        }
        
        private void ShadowTypeCheckLogic(Light light)
        {
            if (approvedShadowTypes == null || approvedShadowTypes.Length <= 0)
            {
                
                AddIssue(new Issue(
                    light.gameObject,
                    IssueLevel.Error,
                    "ShadowTypeが設定されていません。"
                    /*AssetUtility.Get("LightConfigRule.UnauthorizedShadowType"),
                    AssetUtility.Get("LightConfigRule.UnauthorizedLightType.Solution")*/
                ));
                Debug.LogWarning("ShadowTypeが設定されていません。");
                return;
            }
            
            if (!approvedShadowTypes.Contains(light.shadows))
            {
                AddIssue(new Issue(
                    light.gameObject, 
                    IssueLevel.Error,
                    AssetUtility.GetValidator("LightConfigRule.UnauthorizedShadowType", 
                        type, bakeShadowTypeListString, light.shadows),
                    AssetUtility.GetValidator("LightConfigRule.UnauthorizedShadowType.Solution", bakeShadowTypeListString)
                ));
            }
        }
        
        private void CastShadowsCheckLogic(Light light)
        {
            if (castShadows)
            {
                if (light.shadows == LightShadows.None)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.EnableCastShadows"),
                        AssetUtility.GetValidator("LightConfigRule.EnableCastShadows.Solution")
                    ));
                }
            }
            else
            {
                if (light.shadows != LightShadows.None)
                {
                    AddIssue(new Issue(
                        light.gameObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("LightConfigRule.DisableCastShadows"),
                        AssetUtility.GetValidator("LightConfigRule.DisableCastShadows.Solution")
                    ));
                }
            }
        }

        public class LightConfig
        {
            public LightmapBakeType[] bakeTypes { get; private set; }
            public float minRange { get; private set; }
            public float maxRange { get; private set; }
            public float minIntensity { get; private set; }
            public float maxIntensity { get; private set; }
            public float minBounceIntensity { get; private set; }
            public float maxBounceIntensity { get; private set; }
            
            public LightShadows[] approvedShadowTypes { get; private set; }
            
            public bool castShadows{ get; private set; }
            
            public AreaLightConfig areaLightConfig { get; private set; }

            public LightConfig(
                LightmapBakeType[] bakeTypes,
                float minRange = NO_LIMIT, float maxRange = NO_LIMIT, 
                float minIntensity = NO_LIMIT, float maxIntensity = NO_LIMIT, 
                float minBounceIntensity = NO_LIMIT, float maxBounceIntensity = NO_LIMIT,
                LightShadows[] approvedShadowTypes = null,
                bool castShadows = true,
                AreaLightConfig areaLightConfig = new AreaLightConfig()
            )
            {
                this.bakeTypes = bakeTypes;
                this.minRange = minRange;
                this.maxRange = maxRange;
                this.minIntensity = minIntensity;
                this.maxIntensity = maxIntensity;
                this.minBounceIntensity = minBounceIntensity;
                this.maxBounceIntensity = maxBounceIntensity;
                this.approvedShadowTypes = approvedShadowTypes;
                this.castShadows = castShadows;
                this.areaLightConfig = areaLightConfig;
            }
        }

        public readonly struct AreaLightConfig
        {
            public readonly float minAreaWidth;
            public readonly float maxAreaWidth;
            public readonly float minAreaHeight;
            public readonly float maxAreaHeight;
            public readonly float minAreaRadius;
            public readonly float maxAreaRadius;

            public AreaLightConfig(
                float minAreaWidth = NO_LIMIT,
                float maxAreaWidth = NO_LIMIT,
                float minAreaHeight = NO_LIMIT,
                float maxAreaHeight = NO_LIMIT,
                float minAreaRadius = NO_LIMIT,
                float maxAreaRadius = NO_LIMIT
            )
            {
                this.minAreaWidth = minAreaWidth;
                this.maxAreaWidth = maxAreaWidth;
                this.minAreaHeight = minAreaHeight;
                this.maxAreaHeight = maxAreaHeight;
                this.minAreaRadius = minAreaRadius;
                this.maxAreaRadius = maxAreaRadius;
            }
        }
    }
}
#endif
