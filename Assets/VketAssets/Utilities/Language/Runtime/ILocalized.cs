namespace VketAssets.Utilities.Language.Runtime
{
    public interface ILocalized
    {
        /// <summary>
        /// 言語切り替え時の処理
        /// </summary>
        void OnChangeLanguage();
    }
}