#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator.BoundsIndicators;
using VRC.SDK3.Components;

namespace VketTools.Validator.BoundsIndicators
{
    public class VRCStationBoundsSource : IBoundsSource
    {
        private readonly VRCStation station;

        private Collider collider => station.GetComponent<Collider>();

        public VRCStationBoundsSource(VRCStation station)
        {
            this.station = station;
        }

        public Bounds Bounds
        {
            get
            {
                if (collider != null) return collider.bounds;

                var transform = station.gameObject.transform;
                var bounds = new Bounds(transform.position, Vector3.zero);

                foreach (var x in new[] {-0.5f, 0.5f})
                {
                    foreach (var y in new[] {-0.5f, 0.5f})
                    {
                        foreach (var z in new[] {-0.5f, 0.5f})
                        {
                            bounds.Encapsulate(transform.TransformPoint(x, y, z));
                        }
                    }
                }

                return bounds;
            }
        }
        public Bounds LocalBounds => collider
            ? new ColliderBoundsSource(collider).LocalBounds
            : new Bounds(Vector3.zero, Vector3.one);
        public Matrix4x4 LocalToWorldMatrix => collider
            ? new ColliderBoundsSource(collider).LocalToWorldMatrix
            : station.gameObject.transform.localToWorldMatrix;
        public bool IsRemoved => station == null;
    }
}
#endif