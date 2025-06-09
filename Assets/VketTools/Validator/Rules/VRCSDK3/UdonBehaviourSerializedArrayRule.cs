#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VketTools.Validator.Utilities;
using VRC.Udon;

namespace VketTools.Validator
{
    /// <summary>
    /// U#で非プリミティブ型のSerialize配列が初期化されていないかをチェックするルール
    /// </summary>
    public class UdonBehaviourSerializeArrayRule : VketBaseUdonBehaviourRule
    {
        private readonly HashSet<string> _ignoreUdonPrograms;
        private static readonly HashSet<string> PrimitiveTypes = new(){
            "int", "float", "double", "bool", "char",
            "byte", "sbyte", "short", "ushort", "long", "ulong",
            "uint", "decimal", "string"
        };
        
        public UdonBehaviourSerializeArrayRule(string name, string[] ignoreUdonProgramGUIDs) : base(name)
        {
            if (ignoreUdonProgramGUIDs == null)
            {
                ignoreUdonProgramGUIDs = Array.Empty<string>();
            }
            _ignoreUdonPrograms = new HashSet<string>(ignoreUdonProgramGUIDs);
        }
        
        private bool IsIgnoredUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            ScriptableObject program = udonBehaviour.programSource;
            if (program == null) {
                program = new SerializedObject(udonBehaviour)
                          .FindProperty("serializedProgramAsset")
                          .objectReferenceValue as AbstractSerializedUdonProgramAsset;
            }
            var path = AssetDatabase.GetAssetPath(program);
            var guid = AssetDatabase.AssetPathToGUID(path);
            
            return _ignoreUdonPrograms.Contains(guid);
        }

        protected override void ComponentLogic(UdonBehaviour udonBehaviour)
        {
            if(IsIgnoredUdonBehaviour(udonBehaviour))
                return;
            
            // プログラムファイルパスを取得する
            var csPath = string.Empty;
            if (udonBehaviour.programSource is UdonSharpProgramAsset programAsset)
            {
                if (programAsset.sourceCsScript)
                {
                    csPath = AssetDatabase.GetAssetPath(programAsset.sourceCsScript);
                }
            }
            
            if(string.IsNullOrEmpty(csPath))
                return;

            var code = File.ReadAllText(csPath);
            var resultSb = new StringBuilder();

            var serializedArrays = UdonSerializeArrayCheck.Validate(code);
            
            foreach (var (type, variableName, isInitialized) in serializedArrays)
            {
                // プリミティブ型はスキップ
                if(PrimitiveTypes.Contains(type.TrimEnd('[', ']')))
                    continue;
                
                resultSb.Append($"Found Serialized Array: {variableName} ({type}) - Initialized: {isInitialized}");
                resultSb.Append(Environment.NewLine);
                if (isInitialized)
                {
                    AddIssue(new Issue(
                        udonBehaviour,
                        IssueLevel.Warning,
                        AssetUtility.GetValidator("UdonBehaviourSerializeArrayRule.HasInitializeSerializeArray"),
                        AssetUtility.GetValidator("UdonBehaviourSerializeArrayRule.HasInitializeSerializeArray.Solution", variableName, type)));
                }
            }
            
            // Debug.Log(resultSb.ToString().TrimEnd('\r', '\n'));
        }
    }
}
#endif