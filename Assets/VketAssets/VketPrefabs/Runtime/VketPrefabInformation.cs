
using UnityEngine;

namespace Vket.VketPrefabs
{
    /// <summary>
    /// 設定が必要なPrefabごとに固有のタグを設定する
    /// </summary>
    public enum VketPrefabSettingWindowTag
    {
        None,
        VideoPlayer,
        VideoUrlTrigger,
        SoundFade,
        AvatarPedestal,
        Chair,
        FittingChair,
        Pickup,
        FollowPickup,
        WebPageOpener,
        URLOpenr,
        LanguageSwitcher,
        GroupPageOpener,
        StorePreviewOpener,
        AttachItem,
        VketBGMFader,
    }

    [IgnoreBuild, DisallowMultipleComponent]
    public class VketPrefabInformation : MonoBehaviour
    {
        [SerializeField]
        private string _prefabName;
        
        [SerializeField]
        private string _descriptionKey;

        [SerializeField]
        private string _repletionKey;
        
        [SerializeField] 
        private VketPrefabSettingWindowTag _settingWindowWindowTag;

        public string PrefabName => _prefabName;
        public string DescriptionKey => _descriptionKey;
        public string RepletionKey => _repletionKey;

        public VketPrefabSettingWindowTag SettingWindowWindowTag => _settingWindowWindowTag;
    }
}