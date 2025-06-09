using System;
using System.Collections.Generic;

namespace VketTools.Validator.RuleSets
{
    public class VketAvatarOfficialAssetData : IOfficialAssetData
    {
        private string[] GetVketAvatarAssetGUIDs()
        {
            return new[]
            {
                "46920d683d79ba849ad7dc546ca1e00c" // Assets/VketTools/ExhibitionResources/VketAvatarInfo.cs
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
            guids.AddRange(GetVketAvatarAssetGUIDs());
            guids.AddRange(GetExhibitTemplateGUIDs());
            guids.AddRange(GuidDatabase.VketWorldScriptGUIDs);
            guids.AddRange(GuidDatabase.VketAudioSystemGUIDs);
            return guids.ToArray();
        }
        
        protected virtual string[] GetUdonBehaviourPrefabGUIDs()
        {
            var guids = new List<string>();
            guids.AddRange(GuidDatabase.ExhibitorBoothManagerPrefabs);
            guids.AddRange(GuidDatabase.VketPrefabGUIDs);
            return guids.ToArray();
        }
        
        protected virtual string[] GetStarshipTreasurePrefabGUIDs()
        {
            return new[]
            {
                "dc2f05a9ff39c964886c7b5638799687" // Assets/VketStarshipTreasure/VketStarshipTreasure.prefab
            };
        }

        // Continuesを許可するUdonを指定
        protected virtual string[] GetContinueIgnoreUdonGUIDs()
        {
            return new string[]
            {
                "ca4989657f75ca243ba5e291e0f83507", // Assets/VketAssets/VketPrefabs/Runtime/VketAttachItem/UDON/VketAttachItem.asset
            };
        }

        protected virtual string[] GetUdonBehaviourGlobalLinkGUIDs()
        {
            var guids = new List<string>();
            guids.AddRange(GuidDatabase.VketWebLauncherGUIDs);
            guids.AddRange(GuidDatabase.VketAssetsGUIDs);
            return guids.ToArray();
        }

        protected virtual string[] GetPointLightProbeGUIDs() => Array.Empty<string>();

        protected virtual string[] GetDisabledCallbak()
        {
            return new[]
            {
                "_start",
                "_update",
                "_lateUpdate",
                "_fixedUpdate",
                "_postLateUpdate",
                "_onAnimatorMove",
                "_onCollisionStay",
                "_onRenderObject",
                "_onTriggerStay",
                "_onWillRenderObject",
                "_onPlayerJoined",
                "_onPlayerLeft",
                "_inputJump",
                "_inputUse",
                "_inputGrab",
                "_inputDrop",
                "_inputMoveVertical",
                "_inputMoveHorizontal",
                "_inputLookVertical",
                "_inputLookHorizontal",
                "_onPlayerRespawn",
                "_onPlayerTriggerStay",
                "_onPlayerCollisionStay",
                "_onPlayerDataUpdated",
                "_onPlayerRestored"
            };
        }

        protected virtual Dictionary<string, string> GetDisabledDirectives()
        {
            return new Dictionary<string, string>
            {
                { "(#[ \t]*if)[ ,\n,\t]*", "#if" },
                { "(#[ \t]*elif)[ ,\n,\t]*", "#elif" }
            };
        }
        protected virtual string[] GetDeniedShaderNames() => BuiltinShaderDatabase.UnityBuiltinShaderNames;

        public string[] GUIDs => GetGUIDs();
        public string[] MaterialGUIDs => GuidDatabase.MaterialGUIDs;
        public string[] PickupObjectSyncPrefabGUIDs => GuidDatabase.PickupObjectSyncPrefabGUIDs;
        public string[] AvatarPedestalPrefabGUIDs => GuidDatabase.AvatarPedestalPrefabGUIDs;
        public string[] VideoPlayerPrefabGUIDs => GuidDatabase.VideoPlayerPrefabGUIDs;
        public string[] ImageDownloaderPrefabGUIDs => GuidDatabase.ImageDownloaderPrefabGUIDs;
        public string[] StringDownloaderPrefabGUIDs => GuidDatabase.StringDownloaderPrefabGUIDs;
        public string[] VketMirrorPrefabGUIDs => GuidDatabase.VketMirrorPrefabGUIDs;
        public string[] StarshipTreasurePrefabGUIDs => GetStarshipTreasurePrefabGUIDs();
        public string[] UdonBehaviourPrefabGUIDs => GetUdonBehaviourPrefabGUIDs();
        public string[] UdonBehaviourGlobalLinkGUIDs => GetUdonBehaviourGlobalLinkGUIDs();
        public string[] SizeIgnorePrefabGUIDs => Array.Empty<string>();
        public string[] ContinueIgnoreUdonGUIDs => GetContinueIgnoreUdonGUIDs();
        public string[] AudioSourcePrefabGUIDs => Array.Empty<string>();
        public string[] CanvasPrefabGUIDs => GuidDatabase.CanvasPrefabGUIDs;
        public string[] PointLightProbeGUIDs => GetPointLightProbeGUIDs();
        public string[] VRCSDKForbiddenPrefabGUIDs => GuidDatabase.VrcsdkForbiddenPrefabGUIDs;
        public string[] DisabledCallback => GetDisabledCallbak();
        public Dictionary<string, string> DisabledDirectives => GetDisabledDirectives();
        public string[] DeniedShaderNames => GetDeniedShaderNames();
    }
}