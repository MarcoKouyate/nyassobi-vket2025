#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEngine;
using VitDeck.Validator.BoundsIndicators;
using VRC.SDK3.Components;

namespace VketTools.Validator
{
    public class VRCUiShapeBoundsSource : IBoundsSource
    {
        private readonly VRCUiShape uiShape;

        public VRCUiShapeBoundsSource(VRCUiShape uiShape)
        {
            this.uiShape = uiShape;
        }

        private IEnumerable<Vector3> GetLocalCorners(RectTransform transform)
        {
            if (transform == null) yield break;
            var local2dCorners = new Vector3[4];
            transform.GetLocalCorners(local2dCorners);
            foreach (var xy in local2dCorners)
            {
                foreach (var z in new[] {-0.5f, 0.5f})
                {
                    yield return new Vector3(xy.x, xy.y, z * transform.localScale.z);
                }
            }
        }

        public Bounds Bounds
        {
            get
            {
                var bounds = new Bounds(uiShape.transform.position, Vector3.zero);
                foreach (var corner in GetLocalCorners(uiShape.transform as RectTransform))
                    bounds.Encapsulate(uiShape.transform.TransformPoint(corner));
                return bounds;
            }
        }

        public Bounds LocalBounds
        {
            get
            {
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var corner in GetLocalCorners(uiShape.transform as RectTransform))
                    bounds.Encapsulate(corner);
                return bounds;
            }
        }
        public Matrix4x4 LocalToWorldMatrix => uiShape.transform.localToWorldMatrix;
        public bool IsRemoved => uiShape == null;
    }
}
#endif