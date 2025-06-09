using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VketTools.ExhibitionResources
{
    public class VketItemInfo : ScriptableObject
    {
        public enum ItemType : byte
        {
            None,
            Pickup,
            AvatarPedestal,
        }
        
        [SerializeField] public ItemType SelectType;
        [SerializeField] public string BlueprintID;
        [SerializeField] public int SelectTemplateIndex;
        [SerializeField] public string ItemName;
        [SerializeField] public string Price;
        [SerializeField] public string Url;
        [SerializeField] public Texture2D Thumbnail;
        
#if UNITY_EDITOR
        private static readonly string[] AllowOpenUrls = {
            "https://booth.pm/",
            "https://gumroad.com/",
            "https://jinxxy.com/",
            "https://payhip.com/",
        };
        private static readonly string[] AllowOpenUriRegexs =
        {
            "https://.*.booth.pm/items/[0-9]*",
            "https://.*.gumroad.com/*",
        };
                
        public static bool ValidateUrl(string url)
        {
            if(string.IsNullOrEmpty(url))
                return false;
            return AllowOpenUrls.Any(url.StartsWith) || AllowOpenUriRegexs.Any(r => Regex.IsMatch(url, r));
        }
#endif
    }
}
