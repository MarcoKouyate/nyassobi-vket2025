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
    /// [UdonSynced]を付与した変数は1ブースあたり 3 まで
    /// [UdonSynced]を付与した変数は下記の型のみ使用できます bool, sbyte, byte, ushort, short, uint, int, float
    /// </summary>
    public class VketUdonBehaviourSyncedVariablesRule : BaseRule
    {
        private readonly int _limit;
        private readonly HashSet<string> _ignoreUdonPrograms;
        public VketUdonBehaviourSyncedVariablesRule(string name, int limit, string[] ignoreUdonProgramGUIDs) : base(name)
        {
            this._limit = limit;
            _ignoreUdonPrograms = new HashSet<string>(ignoreUdonProgramGUIDs);
        }

        protected override void Logic(ValidationTarget target)
        {
            var rootObjs = target.GetRootObjects();

            foreach (var rootObj in rootObjs)
            {
                LogicForRootObject(rootObj);
            }
        }

        private void LogicForRootObject(GameObject rootObject)
        {
            var udonBehaviours = rootObject.GetComponentsInChildren<UdonBehaviour>(true);
            var count = 0;
            foreach (var udonBehaviour in udonBehaviours)
            {
                // ProgramSourceが無い場合はスキップ
                if (udonBehaviour.programSource == null) continue;
                // 運営から配布したものはスキップ
                if (IsIgnoredUdonBehaviour(udonBehaviour)) continue;
                // プログラムアセットを取得する
                var program = udonBehaviour.programSource.SerializedProgramAsset.RetrieveProgram();
                if (program == null)
                    continue;
                // SyncMetadataTableが無ければスキップ
                if (program.SyncMetadataTable == null) continue;
                
                // SyncMethod == None ならスキップ
                if (udonBehaviour.gameObject.GetComponents<UdonBehaviour>().All(
                        ub => ub.SyncMethod == VRC.SDKBase.Networking.SyncType.None
                )) continue;
                
                // UdonSymcMetadata取得
                var syncs = program.SyncMetadataTable.GetAllSyncMetadata();
                foreach (var sync in syncs)
                {
                    // Sync変数の型を評価
                    var symbolType = program.SymbolTable.GetSymbolType(sync.Name);
                    if (!IsValidType(symbolType))
                    {
                        AddIssue(new Issue(
                            udonBehaviour,
                            IssueLevel.Error,
                            AssetUtility.GetValidator("UdonBehaviourSyncedVariablesRule.InvalidType", sync.Name, symbolType.Name),
                            AssetUtility.GetValidator("UdonBehaviourSyncedVariablesRule.InvalidType.Solution")));
                    }
 
                    count++;
                }
            }

            if (count > _limit)
            {
                AddIssue(new Issue(
                    rootObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("UdonBehaviourSyncedVariablesRule.Overuse", _limit, count),
                    AssetUtility.GetValidator("UdonBehaviourSyncedVariablesRule.Overuse.Solution")));
            }
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

        private bool IsValidType(Type variableType)
        {
            // bool, sbyte, byte, ushort, short, uint, int, float のいずれかの型判定
            return (
                variableType == typeof(bool) ||
                variableType == typeof(sbyte) ||
                variableType == typeof(byte) ||
                variableType == typeof(ushort) ||
                variableType == typeof(short) ||
                variableType == typeof(uint) ||
                variableType == typeof(int) ||
                variableType == typeof(float)||
                variableType == typeof(char)||
                variableType == typeof(double)||
                variableType == typeof(long)||
                variableType == typeof(ulong)
                );
        }
    }
}
#endif
