#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator.BoundsIndicators;

namespace VketTools.Validator.BoundsIndicators
{
    public class VketLightProbeVolumeBoundsSource : IBoundsSource
    {
        private readonly LightProbeProxyVolume volume;
        private readonly Transform probeTransform;

        public VketLightProbeVolumeBoundsSource(LightProbeProxyVolume volume)
        {
            this.volume = volume;
            probeTransform = volume.transform;
        }

        public Bounds Bounds => volume.boundsGlobal;

        public Bounds LocalBounds => new();

        public Matrix4x4 LocalToWorldMatrix => Matrix4x4.identity;

        public bool IsRemoved => probeTransform == null || volume == null;
    }
}
#endif