using System.Collections.Generic;

namespace VketTools.Validator
{
    public interface IOfficialAssetData
    {
        string[] GUIDs { get; }
        string[] MaterialGUIDs { get; }
        string[] PickupObjectSyncPrefabGUIDs { get; }
        string[] AvatarPedestalPrefabGUIDs { get; }
        string[] VideoPlayerPrefabGUIDs { get; }
        string[] ImageDownloaderPrefabGUIDs { get; }
        string[] StringDownloaderPrefabGUIDs { get; }
        string[] VketMirrorPrefabGUIDs { get; }
        string[] StarshipTreasurePrefabGUIDs { get; }
        string[] UdonBehaviourPrefabGUIDs { get; }
        string[] UdonBehaviourGlobalLinkGUIDs { get; }
        string[] SizeIgnorePrefabGUIDs { get; }
        string[] ContinueIgnoreUdonGUIDs { get; }
        string[] AudioSourcePrefabGUIDs { get; }
        string[] CanvasPrefabGUIDs { get; }
        string[] PointLightProbeGUIDs { get; }
        string[] VRCSDKForbiddenPrefabGUIDs { get; }
        string[] DisabledCallback { get; }
        Dictionary<string, string> DisabledDirectives { get; }
        
        /// <summary>
        /// 使用不可なシェーダー群
        /// </summary>
        string[] DeniedShaderNames { get; }
    }
}