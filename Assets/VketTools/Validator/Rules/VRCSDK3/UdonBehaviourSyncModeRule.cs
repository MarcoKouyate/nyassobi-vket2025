#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.Udon;

namespace VketTools.Validator
{
    /// <summary>
    /// UdonBehaviourSyncMode.Continuous をチェックするルール
    /// </summary>
    internal class UdonBehaviourSyncModeRule : VketBaseUdonBehaviourRule
    {
        private readonly HashSet<string> _ignoreUdonPrograms;
        
        public UdonBehaviourSyncModeRule(string name, string[] ignoreUdonProgramGUIDs) : base(name)
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
            
            // プログラムアセットを取得する
            var program = GetUdonProgram(udonBehaviour);
            // SyncMetadataTableが無ければスキップ
            if (program?.SyncMetadataTable == null) return;
            // UdonSyncMetadata取得
            var syncs = program.SyncMetadataTable.GetAllSyncMetadata();
            // 同期変数がないならスキップ
            if (!syncs.Any()) return;

            // SyncType.Continuous (≒ UdonBehaviourSyncMode.Continuous)
            if (udonBehaviour.SyncMethod == VRC.SDKBase.Networking.SyncType.Continuous)
            {
                AddIssue(new Issue(
                    udonBehaviour,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("UdonBehaviourSyncModeRule.InvalidType"),
                    AssetUtility.GetValidator("UdonBehaviourSyncModeRule.InvalidType.Solution")));
            }
        }
    }
}
#endif