#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEngine;
using VitDeck.Validator.BoundsIndicators;
using VketTools.Validator.BoundsIndicators;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Components;
#endif

namespace VketTools.Validator
{
    public class VRCBoothBoundsRule : VketBoothBoundsRule
    {
        public VRCBoothBoundsRule(string name, Vector3 size, float margin) : base(name, size, margin)
        {
        }

        public VRCBoothBoundsRule(string name, Vector3 size, float margin, Vector3 pivot) : base(name, size, margin, pivot)
        {
        }

        public VRCBoothBoundsRule(string name, Vector3 size, float margin, Vector3 pivot, string[] guids) : base(name, size, margin, pivot, guids)
        {
        }
#if VRC_SDK_VRCSDK3
        protected override void InitializeIndicator(BoothRangeIndicator boundsIndicator, BoundsData exceed)
        {
            base.InitializeIndicator(boundsIndicator, exceed);

            var station = exceed.objectReference as VRCStation;
            if (station != null)
            {
                var indicator = station.gameObject.AddComponent<BoundsRangeOutIndicator>();
                indicator.hideFlags = DefaultFlagsForIndicator;
                indicator.Initialize(boundsIndicator, new VRCStationBoundsSource(station), IndicatorResetToken);
            }

            var uiShape = exceed.objectReference as VRCUiShape;
            if (uiShape != null)
            {
                var indicator = uiShape.gameObject.AddComponent<BoundsRangeOutIndicator>();
                indicator.hideFlags = DefaultFlagsForIndicator;
                indicator.Initialize(boundsIndicator, new VRCUiShapeBoundsSource(uiShape), IndicatorResetToken);
            }
        }

        protected override IEnumerable<BoundsData> GetObjectBounds(GameObject gameObject)
        {
            foreach (var boundsData in base.GetObjectBounds(gameObject))
            {
                yield return boundsData;
            }

            foreach (var station in gameObject.GetComponents<VRCStation>())
            {
                yield return new BoundsData(station, new VRCStationBoundsSource(station).Bounds);
            }
            foreach (var uiShape in gameObject.GetComponents<VRCUiShape>())
            {
                yield return new BoundsData(uiShape, new VRCUiShapeBoundsSource(uiShape).Bounds);
            }
        }
#endif
    }
}
#endif