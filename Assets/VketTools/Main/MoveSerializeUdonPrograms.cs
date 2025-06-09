using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace VketTools.Main
{
    public static class MoveSerializeUdonPrograms
    {
#if VRC_SDK_VRCSDK3
        /// <summary>
        /// 入稿シーンが参照しているSerializedUdonPrograms全てを入稿フォルダに移動する
        /// </summary>
        [MenuItem("VketTools/MoveSerializedUdonPrograms")]
        public static void MoveSerializedUdonPrograms()
        {
            var scene = SceneManager.GetActiveScene();
            var baseFolder = Path.GetDirectoryName(scene.path)?.Replace('\\', '/');
            var udonProgramFolder = Path.Combine(baseFolder, "UdonScripts/SerializedUdonPrograms").Replace('\\', '/');

            // Create SerializedUdonPrograms Asset Folder
            if (!Directory.Exists(udonProgramFolder))
            {
                Directory.CreateDirectory(udonProgramFolder);
            }

            // hierarchy 内の Dynamic 以下の UdonBehaviour を取得 (ReferenceObject の下は見ません)
            var udonBehaviours = Array.Empty<UdonBehaviour>();
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                var transformDynamic = rootGameObject.transform.Find("Dynamic");
                if (transformDynamic == null) continue;

                // Dynamicが見つかった場合、UdonBehaviourを取得
                udonBehaviours = transformDynamic.GetComponentsInChildren<UdonBehaviour>(true);
                break;
            }

            if (udonBehaviours.Length == 0)
            {
                Debug.Log("[<color=green>VketTools</color>] No SerializedUdonProgramAssets to move was found.");
                return;
            }
            foreach (var udonBehaviour in udonBehaviours)
            {
                if (udonBehaviour.programSource == null) continue;

                // Get UdonProgramAsset
                var programAsset = udonBehaviour.programSource.SerializedProgramAsset;
                var udonProgramAssetPath = AssetDatabase.GetAssetPath(programAsset);

                // 公式 Prefabのguidはスキップする
                var guid = AssetDatabase.AssetPathToGUID(udonProgramAssetPath);
                var filename = Path.GetFileName(udonProgramAssetPath);
                if (guid == null || Array.IndexOf(ExcludeGUIDs, guid) != -1 || Array.IndexOf(ExcludeFileNames, filename) != -1) continue;

                // baseFolder 内の うどん置き場にない場合は移動してくる
                if (!udonProgramAssetPath.StartsWith(udonProgramFolder))
                {
                    Debug.Log($"[<color=green>VketTools</color>] SerializedUdonProgramAsset Moved!\nFrom: {udonProgramAssetPath}\nTo: {udonProgramFolder}/{filename}");
                    AssetDatabase.MoveAsset(udonProgramAssetPath, $"{udonProgramFolder}/{filename}");
                }
            }
        }
        
        // TODO: VketAssetsのスクリプト追加
        private static readonly string[] ExcludeGUIDs = 
        {
            "2f916e008aa8f294c991c22b42ea5944",  // 73398b290b7c5ec43a2e158bfc1c45db Assets/VketAssets/UdonPrefabs/Udon_PickupObjectSync/Scripts/AutoResetPickup.asset
            "2595ee2141e0fc4408caf75680ef9eb5",  // 0d2cf2386895ff5499a1660a4327ad75 Assets/VketAssets/UdonPrefabs/Udon_AvatarPedestal/Scripts/AvatarPedestal.asset
            "cb458453c6b8c4a4884ba5d3b2f9de56",  // 6ea1e8fa7533f9647996810212fa976e Assets/VketAssets/UdonPrefabs/Udon_CirclePageOpener/Scripts/CirclePageOpener.asset
            "4c879ab359f0cc54984884027c8a015e",  // fe9c6cf6073f2514e82259a4142d6932 Assets/VitDeck/Templates/07_UC/SharedAssets/UC_WorldSetting.asset
            "07ddc9396861b7c4cad40254456b1f9b",  // 6280669e97dfe384988401d2c650a08f Assets/VitDeck/Templates/G00_Gamv0/SharedAssets/UdonScript/WorldSetting(Gamv0).asset
            "a5e90617334978d4dbb5340101839c55",  // 361d89e8564f96c4ea161a47f9fb2871 Assets/VitDeck/Templates/G00_Gamv0/SharedAssets/UdonScript/WorldSettingNode(Gamv0).asset
            "f7ce97192c1898c47a87f949b891d91f",  // 414a471b5fafc3246a7c33b2ae29f2b9 Assets/GamVGimmicks/VideoPlayer_system/BoothVideoPlayer.prefab
            "5a528a40ac7004845bcc072f255cd626",  // 6abc392e5dd9c1848b09205e404a5adf Assets/GamVGimmicks/VideoPlayer_system/BoothVideoPlayerMaster.prefab

            "3692e13500377f04380bd63d295d7fbc",  // 26e911c1e4e64964ea73100994e7c984 Assets/VketAssets/Assets/VketPrefabs/VketLanguageSwitcher/Scripts/VketLanguageSwitcher.asset
            "7fdd6e0e6a66ddb40afafdfe7cbc0e89",  // 62a7876d06fb1d645ab6cb81d87d0a3a Assets/VketAssets/Assets/VketPrefabs/VketAvatarPedestal/Scripts/VketAvatarPedestal.asset
            "7683222f972ff444c910f4920ce600dd",  // fc11049e6474c5e47bc42f47d1a8efca Assets/VketAssets/Assets/VketPrefabs/VketPickup/Scripts/VketPickup.asset
            "22cb79620122c3046a9d91c51d720baa",  // ab981b08fcfada8458fc2ec950e16e17 Assets/VketAssets/Assets/VketPrefabs/VketPickup/Scripts/VketFollowPickup.asset
            "725962251625c26438771124296958f6",  // d6755e37e53268542aae9bd79954a6ab Assets/VketAssets/Assets/VketPrefabs/VketSoundFade/Scripts/VketSoundFade.asset
            "6da6d496ffb0b2c438bf36868f1c313d",  // c7b58c5d2d42fb643b222ef67f6f4e46 Assets/VketAssets/Assets/VketPrefabs/VketVideoPlayer/Scripts/VketVideoUrlTrigger.asset
            "e1ebd5b1f825bdf499c380c43453e0d6",  // 5426b85d610dd5a4990a6965e3716f2d Assets/VketAssets/Assets/VketPrefabs/VketVideoPlayer/Scripts/VketVideoPlayer.asset
            "90a112a9a649fba4d8d442e1d75e7b69",  // 7704391c33fb5e44a9759bcae27b38a8 Assets/VketAssets/Assets/VketPrefabs/VketWebPageOpener/Scripts/VketWebPageOpener.asset
        };

        // TODO: VketAssetsのスクリプト追加
        private static readonly string[] ExcludeFileNames =
        {
            "26e911c1e4e64964ea73100994e7c984.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketLanguageSwitcher/Scripts/VketLanguageSwitcher.asset
            "62a7876d06fb1d645ab6cb81d87d0a3a.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketAvatarPedestal/Scripts/VketAvatarPedestal.asset
            "fc11049e6474c5e47bc42f47d1a8efca.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketPickup/Scripts/VketPickup.asset
            "ab981b08fcfada8458fc2ec950e16e17.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketPickup/Scripts/VketFollowPickup.asset
            "d6755e37e53268542aae9bd79954a6ab.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketSoundFade/Scripts/VketSoundFade.asset
            "c7b58c5d2d42fb643b222ef67f6f4e46.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketVideoPlayer/Scripts/VketVideoUrlTrigger.asset
            "5426b85d610dd5a4990a6965e3716f2d.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketVideoPlayer/Scripts/VketVideoPlayer.asset
            "7704391c33fb5e44a9759bcae27b38a8.asset",  // Assets/VketAssets/Assets/VketPrefabs/VketWebPageOpener/Scripts/VketWebPageOpener.asset
        };
#endif
    }
}