#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.Udon;

namespace VketTools.Validator
{
    /// <summary>
    /// UdonBehaviourを含むオブジェクト、UdonBehaviourによって操作を行うオブジェクトは全て入稿ルール
    /// C.Scene内階層規定におけるDynamicオブジェクトの階層下に入れてください
    /// </summary>
    internal class VketUdonDynamicObjectParentRule : BaseRule
    {

        private readonly HashSet<string> ignoreIDSet;
        private readonly bool _isEnabled;
        public VketUdonDynamicObjectParentRule(string name) : base(name)
        {
            _isEnabled = true;
        }

        public VketUdonDynamicObjectParentRule(string name, string[] guids, bool isEnabled) : this(name)
        {
            _isEnabled = isEnabled;
            ignoreIDSet = new HashSet<string>(guids);
        }

        protected override void Logic(ValidationTarget target)
        {
            if (_isEnabled)
            {
                var rootObjects = target.GetRootObjects();

                foreach (var rootObject in rootObjects)
                {
                    LogicForRootObject(rootObject);
                }
            }
        }

        private void LogicForRootObject(GameObject rootObject)
        {
            Transform dynamicRoot = null;

            foreach (Transform child in rootObject.transform)
            {
                if (child.name == "Dynamic" && dynamicRoot == null)
                {
                    dynamicRoot = child.gameObject.transform;
                }
            }

            var udonBehaviours = rootObject.transform.GetComponentsInChildren<UdonBehaviour>(true);
            // UdonBehaviourが無い場合は帰る
            if (udonBehaviours == null || udonBehaviours.Length == 0) return;

            // UdonBehavioursがあるのにdynamicRootが無いのはエラー
            if (dynamicRoot == null)
            {
                AddIssue(new Issue(
                    rootObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("UdonDynamicObjectParentRule.noDynamicObject"),
                    AssetUtility.GetValidator("UdonDynamicObjectParentRule.noDynamicObject.Solution")
                ));
                return;
            }

            foreach (var udonBehaviour in udonBehaviours)
            {
                // UdonBehaviour がDynamicの子かどうかの検証
                if (!udonBehaviour.transform.IsChildOf(dynamicRoot))
                {
                    AddIssue(new Issue(
                        udonBehaviour,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("UdonDynamicObjectParentRule.UdonBehaviourIsNotChildOfDynamic", udonBehaviour.name),
                        AssetUtility.GetValidator("UdonDynamicObjectParentRule.UdonBehaviourIsNotChildOfDynamic.Solution")
                    ));
                }
                else
                {
                    // UdonBehaviour のPublicVariableを探る
                    var symbols = udonBehaviour.publicVariables.VariableSymbols;
                    foreach (var symbol in symbols)
                    {
                        udonBehaviour.publicVariables.TryGetVariableType(symbol, out var declaredType);
                        // GO,Trans,UB の場合のみ検証(それ以外のコンポーネントはUdon側でエラーになるため)
                        if (declaredType == typeof(GameObject) || declaredType == typeof(UdonBehaviour) ||
                            declaredType == typeof(Transform))
                        {
                            // Transformの取得 (もっとイケてる方法があれば要リファクタ)
                            udonBehaviour.publicVariables.TryGetVariableValue(symbol, out var value);
                            Transform trans = null;
                            if (declaredType == typeof(Transform))
                            {
                                trans = value as Transform;
                            }
                            else if (declaredType == typeof(GameObject))
                            {
                                var go = value as GameObject;
                                if(go != null) trans = go.transform;
                            }
                            else if (declaredType == typeof(UdonBehaviour))
                            {
                                var ub = value as UdonBehaviour;
                                if(ub != null) trans = ub.transform;
                            }
                            // Scene 内配置を検証(Dunamicの子かどうか)
                            if (trans != null && !trans.IsChildOf(dynamicRoot))
                            {
                                // 無視すべきオブジェクトの場合はスキップ
                                if (IsExcludeGuid(trans)) continue;
                                AddIssue(new Issue(
                                    udonBehaviour,
                                    IssueLevel.Error,
                                    AssetUtility.GetValidator("UdonDynamicObjectParentRule.UdonBehaviourPublicValueIsNotChildOfDynamic", udonBehaviour.name, symbol),
                                    AssetUtility.GetValidator("UdonDynamicObjectParentRule.UdonBehaviourPublicValueIsNotChildOfDynamic.Solution")
                                ));
                            }
                        }
                    }
                }
            }

        }

        private bool IsExcludeGuid(Object obj)
        {
            if (ignoreIDSet == null) return false;
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (sourceObject == null) return false;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceObject, out var guid, out long _);
            return ignoreIDSet.Contains(guid);
        }

    }
}
#endif