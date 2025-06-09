using System;
using UnityEditor;
using UnityEngine;

namespace VketTools.Utilities
{
    [CreateAssetMenu]
    public class VketWorldDefinition : ScriptableObject
    {
        [Serializable]
        public class CategorySetting
        {
            public string ValidatorRuleSet;
            public string ExportSetting;
            public int TemplateIndex;
            public int SetPassMaxSize;
            public int BatchesMaxSize;
            public float BuildMaxSize;
            public float AndroidBuildMaxSize;
            public string RuleUrlJa;
            public string RuleUrlEn;
        }
        
        public void Init(bool isItem)
        {
            IsItem = isItem;
            Save();
        }
        
        private bool IsItem { get; set; }
        
        [Header("ワールド名")]
        [SerializeField]
        private string _worldName;

        [Header("ブース入稿設定")] public CategorySetting SpaceSetting;
        [Header("アイテム入稿設定")] public CategorySetting ItemSetting;
        
        public string ValidatorRuleSet => IsItem ? ItemSetting.ValidatorRuleSet : SpaceSetting.ValidatorRuleSet;
        public string ExportSetting => IsItem ? ItemSetting.ExportSetting : SpaceSetting.ExportSetting;
        public int TemplateIndex => IsItem ? ItemSetting.TemplateIndex : SpaceSetting.TemplateIndex;
        public int SetPassMaxSize => IsItem ? ItemSetting.SetPassMaxSize : SpaceSetting.SetPassMaxSize;
        public int BatchesMaxSize => IsItem? ItemSetting.BatchesMaxSize : SpaceSetting.BatchesMaxSize;
        public float BuildMaxSize => IsItem ? ItemSetting.BuildMaxSize : SpaceSetting.BuildMaxSize;
        public float AndroidBuildMaxSize => IsItem ? ItemSetting.AndroidBuildMaxSize : SpaceSetting.AndroidBuildMaxSize;
        private string RuleUrlJa => IsItem ? ItemSetting.RuleUrlJa : SpaceSetting.RuleUrlJa;
        private string RuleUrlEn => IsItem ? ItemSetting.RuleUrlEn : SpaceSetting.RuleUrlEn;
        public string WorldName => _worldName;
        // アバター入稿か？
        public bool IsAvatarSubmission => SpaceSetting.TemplateIndex == 5;
        
        public string GetRuleURL()
        {
            return Application.systemLanguage == SystemLanguage.Japanese
                ? RuleUrlJa
                : RuleUrlEn;
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// Inspectorから変更時になぜか反映されないので保存ボタンを設置
    /// </summary>
    [CustomEditor(typeof(VketWorldDefinition))]
    public class VketWorldDefinitionInspector : Editor
    {
        private VketWorldDefinition _worldDefinition;
        private void OnEnable()
        {
            _worldDefinition = target as VketWorldDefinition;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Save"))
            {
                _worldDefinition.Save();
            }
        }
    }
}
