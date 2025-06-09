#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.Udon;

namespace VketTools.Validator
{
    /// <summary>
    /// 禁止されたコールバックが使用されていないか検出するルール
    /// </summary>
    public class UdonAssemblyCallbackRule : VketBaseUdonBehaviourRule
    {

        private readonly HashSet<string> _ignoreUdonPrograms;
        private readonly HashSet<string> _disabledEntryPoints;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルール名</param>
        public UdonAssemblyCallbackRule(string name, string[] disabledEntryPoints, string[] ignoreUdonProgramGUIDs): base(name)
        {
            _ignoreUdonPrograms = new HashSet<string>(ignoreUdonProgramGUIDs);
            _disabledEntryPoints = new HashSet<string>(disabledEntryPoints);
        }

        protected override void ComponentLogic(UdonBehaviour component)
        {
            if (IsIgnoredUdonBehaviour(component)) return;
            
            // UdonProgramName
            var programName = component.programSource?.name ?? $"UdonBehaviour ({component.gameObject.name})";
            
            // CyanTriggerはStartの使用を許可する
            var dontCareStart = false;

            if (programName.EndsWith("CyanTriggerProgram"))
            {
                dontCareStart = component.GetComponent("CyanTriggerAsset") != null;
            }

            // プログラム
            var program = GetUdonProgram(component);

            // プログラムが設定されていない場合には何もしない（警告を出したりするのはこのルールの仕事ではない）
            if (program == null) return;

            var entryPoints = program.EntryPoints.GetExportedSymbols();

            var foundEntryPoints = new HashSet<string>(_disabledEntryPoints.Intersect(entryPoints));

            if (dontCareStart)
            {
                foundEntryPoints.Remove("_start");
            }

            if (foundEntryPoints.Any())
            {
                var callbackNames = string.Join(", ", foundEntryPoints.Select(EntryPointToCallbackName));
                AddIssue(new Issue(component, IssueLevel.Error,
                    AssetUtility.GetValidator("UdonAssemblyCallbackRule.DisallowCallbacks", programName, callbackNames),
                    AssetUtility.GetValidator("UdonAssemblyCallbackRule.DisallowCallbacks.Solution", programName)));
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

        private static string EntryPointToCallbackName(string entryPoint)
        {
            // _update => Update
            return Regex.Replace(entryPoint.TrimStart('_'), "^.", s => s.Value.ToUpper());
        }
    }
}
#endif
