using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VketAssets.Utilities.Language.Runtime
{
    [CreateAssetMenu]
    public class LanguageSettings : ScriptableObject
    {
        /// <summary>
        /// システム名
        /// </summary>
        [SerializeField]
        private string _systemName;

        /// <summary>
        /// タグ一覧
        /// </summary>
        [SerializeField] private List<string> _tags = new List<string>();

        /// <summary>
        /// 言語情報
        /// </summary>
        [SerializeField]
        private List<LanguageDictionary> _languageDictionaryList = new List<LanguageDictionary>();
        
        public string SystemName => _systemName;
        public List<string> Tags => _tags;
        public List<LanguageDictionary> LanguageDictionaryList => _languageDictionaryList;

#if UNITY_EDITOR
        public void UpdateDictionary(string tag, List<LanguageDictionary> dictionaries)
        {
            if (!_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
            
            var removeDictionaryList = _languageDictionaryList.Where(s => s.Tag == tag).ToList();
            foreach (var removeDictionary in removeDictionaryList)
            {
                _languageDictionaryList.Remove(removeDictionary);
            }
            _languageDictionaryList.AddRange(dictionaries);

            SetUpDictionary();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
        
        public void SetUpDictionary()
        {
            foreach (var tag in Tags)
            {
                LanguageDictionary dictionary = null;
                var currentLanguage = LocalizedSetting.Instance.Language;

                var dictionaries = LanguageDictionaryList.Where(lang => lang.SystemLanguage == currentLanguage && lang.Tag == tag).ToList();
                if (dictionaries.Any())
                {
                    dictionary = dictionaries.First();
                }
                else
                {
                    dictionaries = LanguageDictionaryList.Where(lang => lang.SystemLanguage == LocalizedSetting.DefaultLanguage && lang.Tag == tag).ToList();
                    if(dictionaries.Any())
                        dictionary = dictionaries.First();
                }

                LocalizedMessage.SetDictionary(dictionary, SystemName, tag);
            }
        }
    }
}