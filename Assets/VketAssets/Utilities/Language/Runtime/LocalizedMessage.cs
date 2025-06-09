using System;
using System.Collections.Generic;

namespace VketAssets.Utilities.Language.Runtime
{
    public static class LocalizedMessage
    {
        private static readonly Dictionary<string, LanguageDictionary> Dictionary = new Dictionary<string, LanguageDictionary>();

        public static string Get(string messageID, string systemName, string tag, params object[] args)
        {
            var key = string.Join(".", systemName, tag);
            return Get(messageID, key, args);
        }

        public static string Get(string messageID, string key, params object[] args)
        {
            if (!Dictionary.ContainsKey(key) || !Dictionary[key])
            {
                return messageID;
            }
            
            var found = Dictionary[key].TryGetValue(messageID, out var translated);

            if (!found)
                return messageID;

            if (string.IsNullOrEmpty(translated))
                return messageID;

            try
            {
                return string.Format(translated, args);
            }
            catch (FormatException e)
            {
                throw new InvalidOperationException($"翻訳文のフォーマットが一致しません。\nMessageID={messageID}\nMessage{translated}", e);
            }
        }

        public static void SetDictionary(LanguageDictionary dictionary, string systemName, string tag)
        {
            SetDictionary(dictionary, string.Join(".", systemName, tag));
        }
        
        public static void SetDictionary(LanguageDictionary dictionary, string key)
        {
            Dictionary[key] = dictionary;
        }
    }

    public interface ILanguage
    {
        string this[string key] { get; }
    }
}