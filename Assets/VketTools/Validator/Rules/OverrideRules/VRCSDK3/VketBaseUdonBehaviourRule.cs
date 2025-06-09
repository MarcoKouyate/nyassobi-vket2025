#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Editor.ProgramSources.UdonGraphProgram;
using VRC.Udon.Graph;
using VRC.Udon.UAssembly.Disassembler;

namespace VketTools.Validator
{
    /// <summary>
    /// UdonBehaviourを探してそれぞれバリデーションするためのクラス
    /// 詳細な規約はクラスを継承して ComponentLogic に記述する
    /// </summary>
    public class VketBaseUdonBehaviourRule : ComponentBaseRule<UdonBehaviour>
    {
        public VketBaseUdonBehaviourRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            foreach (var gameObject in target.GetAllObjects())
            {
                var components = gameObject.GetComponents<UdonBehaviour>();

                if (components.Length == 0)
                    continue;

                // (VCC対応) U# 1.x 系ではUdonBehaviourは隠されてしまうので対応
                // if (!HasVisibleComponents(components))
                //     continue;

                HasComponentObjectLogic(gameObject);

                foreach (var component in components)
                {
                    ComponentLogic(component);
                }
            }
        }

        protected override void ComponentLogic(UdonBehaviour component)
        {
            // UdonProgramName
            var udonProgramName = component.programSource.name;
            Debug.Log(udonProgramName);

            // ノード一覧
            var graph = GetGraphData(component);
            if (graph != null)
            {
                List<string> nodeNames = new List<string>();
                foreach (var node in graph.nodes)
                {
                    nodeNames.Add(node.fullName);
                }
                Debug.Log(String.Join("\n", nodeNames));
            }

            // コード
            Debug.Log(GetDisassembleCode(component));
        }

        protected override void HasComponentObjectLogic(GameObject hasComponentObject)
        {
        }

        /// <summary>
        /// SerializedAssemblyからプログラムを取得
        /// </summary>
        /// <param name="component">VRC.Udon.UdonBehaviour</param>
        /// <returns>IUdonProgram</returns>
        protected static IUdonProgram GetUdonProgram(UdonBehaviour component)
        {
            return (
                new SerializedObject(component)
                    .FindProperty("serializedProgramAsset")
                    .objectReferenceValue as AbstractSerializedUdonProgramAsset
                )?.RetrieveProgram();
        }

        /// <summary>
        /// プログラムからのディスアセンブル
        /// </summary>
        /// <param name="program">VRC.Udon.Common.Interfaces.IUdonProgram</param>
        /// <returns>string</returns>
        protected static string GetDisassembleCode(IUdonProgram program)
        {
            var disasm = new UAssemblyDisassembler();
            if (program != null)
            {
                return String.Join("\n", disasm.DisassembleProgram(program));
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// バイトコードからのディスアセンブル
        /// </summary>
        /// <param name="component">VRC.Udon.UdonBehaviour</param>
        /// <returns>string</returns>
        protected static string GetDisassembleCode(UdonBehaviour component)
        {
            if (component != null)
            {
                return GetDisassembleCode(GetUdonProgram(component));

            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// UdonGraphの取り出し
        /// </summary>
        /// <param name="component">VRC.Udon.UdonBehaviour</param>
        /// <returns>VRC.Udon.Graph.UdonGraphData</returns>
        protected static UdonGraphData GetGraphData(UdonBehaviour component)
        {
            var programAsset = component.programSource as UdonGraphProgramAsset;
            return programAsset?.GetGraphData();
        }
    }
}
#endif
