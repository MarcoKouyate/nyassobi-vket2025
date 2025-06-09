#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using VketTools.Utilities;
using VitDeck.Validator;
using VitDeck.Validator.BoundsIndicators;
using VketTools.Validator.BoundsIndicators;

namespace VketTools.Validator
{
    /// <summary>
    /// ブースのサイズ制限を調べるルール
    /// </summary>
    public class VketBoothBoundsRule : BaseRule
    {
        protected const HideFlags DefaultFlagsForIndicator = HideFlags.DontSave | HideFlags.HideInInspector;

        private readonly Bounds limit;
        private readonly float margin;
        private readonly string floatToStringArgument;

        private readonly HashSet<string> ignoreIDSet;

        // ルールをValidation毎に生成する場合indicatorResetter.Reset()が叩かれなくなってしまう為、staticに設定
        private static ResetTokenSource indicatorResetter = null;
        protected static ResetToken IndicatorResetToken => indicatorResetter.Token;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルールの名前</param>
        /// <param name="size">バウンディングボックスの大きさ</param>
        /// <param name="margin">制限に持たせる余裕</param>
        public VketBoothBoundsRule(string name, Vector3 size, float margin)
            : this(name, size, margin, pivot: Vector3.zero) { }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルールの名前</param>
        /// <param name="size">バウンディングボックスの大きさ</param>
        /// <param name="margin">制限に持たせる余裕</param>
        /// <param name="pivot">バウンディングボックスの原点（中心下）</param>
        public VketBoothBoundsRule(string name, Vector3 size, float margin, Vector3 pivot) : base(name)
        {
            var center = pivot + (Vector3.up * size.y * 0.5f);
            var limit = new Bounds(center, size);
            this.limit = limit;
            this.margin = margin;

            var maxDecimalPlaces = new float[] { size.x, size.y, size.z, margin }
                .Select(ToDecimalPlaces)
                .Max();
            floatToStringArgument = string.Format("f{0}", maxDecimalPlaces + 1);
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルールの名前</param>
        /// <param name="size">バウンディングボックスの大きさ</param>
        /// <param name="margin">制限に持たせる余裕</param>
        /// <param name="pivot">バウンディングボックスの原点（中心下）</param>
        /// <param name="guids">無視するguid</param>
        public VketBoothBoundsRule(string name, Vector3 size, float margin, Vector3 pivot, string[] guids) :this(name, size, margin, pivot)
        {
            ignoreIDSet = new HashSet<string>(guids);
        }

        private int ToDecimalPlaces(float val)
        {
            var pointIndex = val.ToString().IndexOf(".");
            if (pointIndex == -1)
                return 0;
            else
                return val.ToString().Substring(pointIndex).Length - 1;
        }

        protected override void Logic(ValidationTarget target)
        {
            Reset();

            var rootObjects = target.GetRootObjects();

            foreach (var rootObject in rootObjects)
            {
                LogicForRootObject(rootObject);
            }
        }
        
        protected void Reset()
        {
            if (indicatorResetter != null)
            {
                indicatorResetter.Reset();
            }
            indicatorResetter = new ResetTokenSource();
        }

        protected virtual void InitializeIndicator(BoothRangeIndicator boundsIndicator, BoundsData exceed)
        {
            var rectTransform = exceed.objectReference as RectTransform;
            if (rectTransform != null)
            {
                var indicator = rectTransform.gameObject.AddComponent<RectTransformRangeOutIndicator>();
                indicator.hideFlags = DefaultFlagsForIndicator;
                indicator.Initialize(boundsIndicator, rectTransform, indicatorResetter.Token);
            }
            else
            {
                var transform = exceed.objectReference as Transform;
                if (transform != null)
                {
                    var indicator = transform.gameObject.AddComponent<TransformRangeOutIndicator>();
                    indicator.hideFlags = DefaultFlagsForIndicator;
                    indicator.Initialize(boundsIndicator, indicatorResetter.Token);
                }
            }

            var renderer = exceed.objectReference as Renderer;
            if (renderer != null)
            {
                var indicator = renderer.gameObject.AddComponent<BoundsRangeOutIndicator>();
                indicator.hideFlags = DefaultFlagsForIndicator;
                indicator.Initialize(boundsIndicator, new RendererBoundsSource(renderer), indicatorResetter.Token);
            }

            var collider = exceed.objectReference as Collider;
            if (collider != null)
            {
                var indicator = collider.gameObject.AddComponent<BoundsRangeOutIndicator>();
                indicator.hideFlags = DefaultFlagsForIndicator;
                indicator.Initialize(boundsIndicator, new ColliderBoundsSource(collider), indicatorResetter.Token);
            }

            var probeGroup = exceed.objectReference as LightProbeGroup;
            if (probeGroup != null)
            {
                var indicator = probeGroup.gameObject.AddComponent<BoundsRangeOutIndicator>();
                indicator.hideFlags = DefaultFlagsForIndicator;
                indicator.Initialize(boundsIndicator, new LightProbeBoundsSource(probeGroup), indicatorResetter.Token);
            }
            
            var probeVolume = exceed.objectReference as LightProbeProxyVolume;
            if (probeVolume != null)
            {
                var indicator = probeVolume.gameObject.AddComponent<BoundsRangeOutIndicator>();
                indicator.hideFlags = DefaultFlagsForIndicator;
                indicator.Initialize(boundsIndicator, new VketLightProbeVolumeBoundsSource(probeVolume), indicatorResetter.Token);
            }
        }

        private void LogicForRootObject(GameObject rootObject)
        {
            var rootTransform = rootObject.transform;

            var rootTransformMemory = TransformMemory.SaveAndReset(rootTransform);

            var validationLimit = new Bounds(limit.center + rootTransform.position, limit.size);
            validationLimit.Expand(margin);

            List<GameObject> ignoreObjects = new List<GameObject>();
            var staticRoot = GetStaticRoot(rootObject);
            var dynamicRoot = GetDynamicRoot(rootObject);
            ignoreObjects.Add(rootObject);
            if(staticRoot)
                ignoreObjects.Add(staticRoot);
            if(dynamicRoot)
                ignoreObjects.Add(dynamicRoot);
            
            var allExceeds = rootObject
                             .GetComponentsInChildren<Transform>(true)
                             .Select(transform => transform.gameObject)
                             .Where(obj => !IsExcludeGuid(obj))
                             .Where(obj => !ignoreObjects.Contains(obj))
                             .SelectMany(GetObjectBounds);
            
            var exceeds = allExceeds
                .Where(data => IsExceeded(data.bounds, validationLimit));

            var boundsIndicator = rootObject.AddComponent<BoothRangeIndicator>();
            boundsIndicator.hideFlags = DefaultFlagsForIndicator;
            boundsIndicator.Initialize(validationLimit, indicatorResetter.Token);

            foreach (var allExceed in allExceeds)
            {
                InitializeIndicator(boundsIndicator, allExceed);
            }
            
            foreach (var exceed in exceeds)
            {
                var limitSize = limit.size.ToString();
                var message = AssetUtility.GetValidator("BoothBoundsRule.Exceeded",
                    limitSize,
                    limit.ToString(floatToStringArgument),
                    exceed.bounds.ToString(floatToStringArgument),
                    exceed.objectReference.GetType().Name);

                AddIssue(new Issue(exceed.objectReference, IssueLevel.Error, message));
            }

            rootTransformMemory.Apply(rootTransform);
        }
        
        private GameObject GetStaticRoot(GameObject rootObject)
        {
            foreach (Transform child in rootObject.transform)
            {
                if (child.name == "Static")
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private GameObject GetDynamicRoot(GameObject rootObject)
        {
            foreach (Transform child in rootObject.transform)
            {
                if (child.name == "Dynamic")
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private bool IsExceeded(Bounds bounds, Bounds limit)
        {
            return
                !limit.Contains(bounds.min) ||
                !limit.Contains(bounds.max);
        }

        private bool IsExcludeGuid(Object obj)
        {
            if (ignoreIDSet == null) return false;
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (sourceObject == null) return false;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceObject, out var guid, out long _);
            return ignoreIDSet.Contains(guid);
        }

        protected virtual IEnumerable<BoundsData> GetObjectBounds(GameObject gameObject)
        {
            var transform = gameObject.transform;
            if (transform is RectTransform)
            {
                yield return BoundsData.FromRectTransform((RectTransform)transform);
            }
            else
            {
                yield return BoundsData.FromTransform(transform);
            }

            foreach (var renderer in gameObject.GetComponents<Renderer>())
            {
                if (!(renderer is TrailRenderer) && !(renderer is ParticleSystemRenderer))
                    yield return BoundsData.FromRenderer(renderer);
            }

            foreach (var collider in gameObject.GetComponents<Collider>())
            {
                yield return BoundsData.FromCollider(collider);
            }

            foreach (var lightProbe in gameObject.GetComponents<LightProbeGroup>())
            {
                BoundsData data;
                if (!BoundsData.TryCreateFromLightProbe(lightProbe, out data))
                {
                    continue;
                }

                yield return data;
            }
            
            foreach (var lightProbeVolume in gameObject.GetComponents<LightProbeProxyVolume>())
            {
                yield return BoundsData.FromLightProbeProxyVolume(lightProbeVolume);
            }
        }

        protected struct BoundsData
        {
            public readonly Object objectReference;
            public readonly Bounds bounds;

            public BoundsData(Object objectReference, Vector3 center)
                : this(objectReference, center, Vector3.zero) { }

            public BoundsData(Object objectReference, Vector3 center, Vector3 size)
                : this(objectReference, new Bounds(center, size)) { }

            public BoundsData(Object objectReference, Bounds bounds)
            {
                if (objectReference == null)
                    throw new System.ArgumentNullException("objectReference");
                this.objectReference = objectReference;
                this.bounds = bounds;
            }

            public static BoundsData FromTransform(Transform transform)
            {
                return new BoundsData(transform, transform.position);
            }

            public static BoundsData FromRectTransform(RectTransform transform)
            {
                var bounds = new Bounds(transform.position, Vector3.zero);

                var corners = new Vector3[4];
                transform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    bounds.Encapsulate(corner);
                }

                return new BoundsData(transform, bounds);
            }

            public static BoundsData FromRenderer(Renderer renderer)
            {
                //Recalculate bounds for ParticleSystem
                var particleSystem = renderer.gameObject.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                    particleSystem.Simulate(0f);
                // //Reculculate bounds for TrailRenderer
                // if (renderer is TrailRenderer)
                // {
                //     var originalFlag = renderer.enabled;
                //     renderer.enabled = !originalFlag;
                //     renderer.enabled = originalFlag;
                // }
                return new BoundsData(renderer, renderer.bounds);
            }

            internal static bool TryCreateFromLightProbe(LightProbeGroup lightProbe, out BoundsData boundsData)
            {
                var positions = lightProbe.probePositions;
                if (positions.Length == 0)
                {
                    boundsData = default(BoundsData);
                    return false;
                }

                var transformer = lightProbe.transform.localToWorldMatrix;

                var first = transformer.MultiplyPoint3x4(positions[0]);
                Vector3 min = first;
                Vector3 max = first;
                for (int i = 1; i < positions.Length; i++)
                {
                    var worldPosition = transformer.MultiplyPoint3x4(positions[i]);
                    min = Vector3.Min(min, worldPosition);
                    max = Vector3.Max(max, worldPosition);
                }

                var center = (min + max) * 0.5f;
                var size = max - min;
                boundsData = new BoundsData(lightProbe, new Bounds(center, size));

                return true;
            }

            internal static BoundsData FromCollider(Collider collider)
            {
                return new BoundsData(collider, collider.bounds);
            }

            internal static BoundsData FromLightProbeProxyVolume(LightProbeProxyVolume lightProbeProxyVolume)
            {
                return new BoundsData(lightProbeProxyVolume, lightProbeProxyVolume.boundsGlobal);
            }
        }
    }
}
#endif