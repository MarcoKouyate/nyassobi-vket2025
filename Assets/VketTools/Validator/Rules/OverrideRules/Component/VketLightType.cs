namespace VketTools.Validator
{
    /// <summary>
    /// AreaLight が LightType.Area → .Rectangle, .Disc に分かれてしまっているので、「エリアライト」概念とのずれを吸収するための enum
    /// </summary>
    public enum VketLightType
    {
        /// <summary>
        ///   <para>The light is a spot light.</para>
        /// </summary>
        Spot = 0,
        /// <summary>
        ///   <para>The light is a directional light.</para>
        /// </summary>
        Directional = 1,
        /// <summary>
        ///   <para>The light is a point light.</para>
        /// </summary>
        Point = 2,
        /// <summary>
        ///   <para>The light is a rectangle or disc shaped area light. It affects only baked lightmaps and lightprobes.</para>
        /// </summary>
        Area = 3,
    }

    public static class VketLightTypeExt
    {
        public static bool MatchesUnityLightType(this VketLightType vitDeckLightType, UnityEngine.LightType unityEngineLightType)
        {
            if (vitDeckLightType == VketLightType.Area)
            {
                return unityEngineLightType == UnityEngine.LightType.Rectangle ||
                       unityEngineLightType == UnityEngine.LightType.Disc;
            }

            return (int) vitDeckLightType == (int) unityEngineLightType;
        }
    }
}