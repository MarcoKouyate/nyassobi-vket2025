using System;
using System.Collections.Generic;

namespace VketTools.Validator.RuleSets
{
    public class VketItemOfficialAssetData : IOfficialAssetData
    {
        private string[] GetVketItemAssetGUIDs()
        {
            return new[]
            {
                "a0086194d47c39746bef068e81345207" // Assets/VketTools/Utilities/VketItemInfo.cs
            };
        }

        private string[] GetExhibitTemplateGUIDs()
        {
            // "Assets/VitDeck/Templates/"以下に素材がある場合はバリデート許可するために追加する
            return new string[]
            {
            };
        }

        /// <summary>
        /// 公式配布されたアセット及び前提となるアセットのGUID
        /// </summary>
        protected virtual string[] GetGUIDs()
        {
            var guids = new List<string>();
            guids.AddRange(GuidDatabase.VrcSdkGUIDs);
            guids.AddRange(GuidDatabase.VitDeckGUIDs);
            guids.AddRange(GuidDatabase.VketShaderPackGUIDs);
            guids.AddRange(GetVketItemAssetGUIDs());
            guids.AddRange(GetExhibitTemplateGUIDs());
            guids.AddRange(GuidDatabase.VketWorldScriptGUIDs);
            guids.AddRange(GuidDatabase.VketAudioSystemGUIDs);
            return guids.ToArray();
        }

        protected virtual string[] GetSizeIgnorePrefabGUIDs()
        {
            return new[]
            {
                "d19ff96a19f6fdb4cb57095e22e5ba37", // Assets/VketAssets/Assets/VketPrefabs/VketStringDownloader/VketStringDownloader.prefab
                "73b0727ab433c3140929fbf088cd8b88", // Assets/VketAssets/Assets/VketPrefabs/VketVideoPlayer/VketVideoPlayer.prefab
                "b291170635bff9841bbd09d362a0d170" // Assets/VketAssets/Assets/VketPrefabs/VketVideoPlayer/VketVideoUrlTrigger.prefab
            };
        }
        
        protected virtual string[] GetStarshipTreasurePrefabGUIDs()
        {
            return new[]
            {
                "dc2f05a9ff39c964886c7b5638799687" // Assets/VketStarshipTreasure/VketStarshipTreasure.prefab
            };
        }
        
        protected virtual string[] GetDeniedShaderNames() => BuiltinShaderDatabase.UnityBuiltinShaderNames;
        
        public string[] GUIDs => GetGUIDs();
        public string[] MaterialGUIDs => GuidDatabase.MaterialGUIDs;
        public string[] PickupObjectSyncPrefabGUIDs => GuidDatabase.PickupObjectSyncPrefabGUIDs;
        public string[] AvatarPedestalPrefabGUIDs => Array.Empty<string>();
        public string[] VideoPlayerPrefabGUIDs => GuidDatabase.VideoPlayerPrefabGUIDs;
        public string[] ImageDownloaderPrefabGUIDs => GuidDatabase.ImageDownloaderPrefabGUIDs;
        public string[] StringDownloaderPrefabGUIDs => GuidDatabase.StringDownloaderPrefabGUIDs;
        public string[] VketMirrorPrefabGUIDs => GuidDatabase.VketMirrorPrefabGUIDs;
        public string[] StarshipTreasurePrefabGUIDs => GetStarshipTreasurePrefabGUIDs();
        public string[] UdonBehaviourPrefabGUIDs => Array.Empty<string>();
        public string[] UdonBehaviourGlobalLinkGUIDs => Array.Empty<string>();
        public string[] SizeIgnorePrefabGUIDs => GetSizeIgnorePrefabGUIDs();
        public string[] ContinueIgnoreUdonGUIDs => Array.Empty<string>();
        public string[] AudioSourcePrefabGUIDs => Array.Empty<string>();
        public string[] CanvasPrefabGUIDs => GuidDatabase.CanvasPrefabGUIDs;
        public string[] PointLightProbeGUIDs => Array.Empty<string>();
        public string[] VRCSDKForbiddenPrefabGUIDs => GuidDatabase.VrcsdkForbiddenPrefabGUIDs;
        public string[] DisabledCallback => Array.Empty<string>();
        public Dictionary<string, string> DisabledDirectives => new();
        public string[] DeniedShaderNames => GetDeniedShaderNames();
    }
}