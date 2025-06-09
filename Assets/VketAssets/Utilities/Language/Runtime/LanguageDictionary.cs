using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VketAssets.Utilities.Language.Runtime
{
    [CreateAssetMenu(menuName = "VketAssets/LanguageDictionary", fileName = "LanguageDictionary")]
    public class LanguageDictionary : ScriptableObject, ILanguage, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct Pair
        {
            [SerializeField]
            private string _key;
            [SerializeField]
            private string _value;

            public string Key => _key;
            public string Value
            {
                get => _value;
                set => _value = value;
            }

            public Pair(KeyValuePair<string, object> jsonData)
            {
                _key = jsonData.Key;
                _value = jsonData.Value.ToString();
                _value = Regex.Replace(_value, @"\\r\\n|\\r|\\n", Environment.NewLine);
            }
            
            public Pair(string key, string value)
            {
                _key = key;
                _value = value;
            }
        }

        [SerializeField] private string _tag;
        [SerializeField] private SystemLanguage _systemLanguage;
        [SerializeField] private Pair[] _language = Array.Empty<Pair>();
        
        public string Tag => _tag;
        public SystemLanguage SystemLanguage => _systemLanguage;
        
        private Dictionary<string, string> dictionary;
        public string this[string key] => dictionary[key];

        public Pair[] Language
        {
            get => _language;
            set
            {
                _language = value;
                OnAfterDeserialize();
            }
        }

#if UNITY_EDITOR
        public void SetTranslations(string tag, SystemLanguage systemLanguage, Dictionary<string, object> dictionaryData)
        {
            var pairs = new Pair[dictionaryData.Count];
            int i = 0;
            foreach (var dataPair in dictionaryData)
            {
                pairs[i] = new Pair(dataPair);
                i++;
            }
            
            _tag = tag;
            _systemLanguage = systemLanguage;
            _language = pairs;
            OnAfterDeserialize();
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif

        public bool TryGetValue(string key, out string value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public void OnBeforeSerialize() { }
        
        public void OnAfterDeserialize()
        {
            dictionary = new Dictionary<string, string>();
            foreach (var text in _language)
            {
                dictionary.Add(text.Key, text.Value);
            }
        }
    }
}