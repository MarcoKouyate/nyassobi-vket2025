
using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketLanguageSwitcher : UdonSharpBehaviour
    {
        [SerializeField]
        private GameObject[] jpSwitchObjects;
        [SerializeField]
        private GameObject[] enSwitchObjects;
        [SerializeField]
        private Sprite switchToEnglishSprite;
        [SerializeField]
        private Sprite switchToJapaneseSprite;

        [SerializeField]
        private SpriteRenderer spriteRenderer;
        [SerializeField]
        private UdonBehaviour udonManager;

        public override void Interact()
        {
            udonManager.SendCustomEvent("_SwitchLanguage");
        }

        public void _SwitchToEn()
        {
            spriteRenderer.sprite = switchToJapaneseSprite;

            foreach (var go in jpSwitchObjects)
            {
                if (go != null)
                    go.SetActive(false);
            }

            foreach (var go in enSwitchObjects)
            {
                if (go != null)
                    go.SetActive(true);
            }
        }

        public void _SwitchToJp()
        {
            spriteRenderer.sprite = switchToEnglishSprite;

            foreach (var go in jpSwitchObjects)
            {
                if (go != null)
                    go.SetActive(true);
            }

            foreach (var go in enSwitchObjects)
            {
                if (go != null)
                    go.SetActive(false);
            }
        }
    }
}