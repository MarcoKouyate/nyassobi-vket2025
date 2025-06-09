#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 使用可能なコンポーネントを検証する。
    /// </summary>
    /// <remarks>
    /// 複数のコンポーネントリストを持ち、リストの設定に応じて許可/許否/要申請を決定します。
    /// また、プレハブをGUIDで与えることで、そのプレハブに元から追加してあるコンポーネントを許可されているものとして無視します。
    /// </remarks>
    public class VketUsableComponentListRule : BaseRule
    {
        private readonly ComponentReference[] references;

        private readonly ValidationLevel unregisteredComponentValidationLevel;

        private readonly HashSet<string> ignorePrefabs;

        private readonly bool _showOfficialAssetsSolution;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルール名</param>
        /// <param name="references">コンポーネントリスト</param>
        /// <param name="ignorePrefabGUIDs">例外Prefabのリスト</param>
        /// <param name="isItem">アイテム入稿の場合はtrueを指定</param>
        /// <param name="unregisteredComponent">リストにないコンポーネントの扱い</param>
        public VketUsableComponentListRule(string name,
            ComponentReference[] references,
            string[] ignorePrefabGUIDs = null,
            bool showOfficialAssetsSolution = true,
            ValidationLevel unregisteredComponent = ValidationLevel.ALLOW)
            : base(name)
        {
            this.references = references ?? new ComponentReference[] { };
            if (ignorePrefabGUIDs == null)
            {
                ignorePrefabGUIDs = new string[0];
            }
            ignorePrefabs = new HashSet<string>(ignorePrefabGUIDs);
            unregisteredComponentValidationLevel = unregisteredComponent;
            _showOfficialAssetsSolution = showOfficialAssetsSolution;
        }

        protected override void Logic(ValidationTarget target)
        {
            foreach (var gameObject in target.GetAllObjects())
            {
                var components = gameObject.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null ||
                        component is Transform)
                        continue;

                    if ((component.hideFlags & HideFlags.DontSaveInEditor) == HideFlags.DontSaveInEditor)
                    {
                        continue;
                    }
                    var isPrefabComponent = !PrefabUtility.IsAddedComponentOverride(component);
                    if (IsIgnoredComponent(component) &&
                        isPrefabComponent)
                    {
                        continue;
                    }

                    bool found = false;
                    foreach (var reference in references)
                    {
                        if (reference != null && reference.Exists(component))
                        {
                            found = true;
                            AddComponentIssue(reference.name, gameObject, component, reference.level);
                        }
                    }

                    if (!found)
                    {
                        AddComponentIssue(AssetUtility.GetValidator("UsableComponentListRule.DefaultComponentGroupName"), gameObject, component, unregisteredComponentValidationLevel);
                    }
                }
            }
        }

        private bool IsIgnoredComponent(Component cp)
        {
            if (PrefabUtility.GetPrefabInstanceStatus(cp) != PrefabInstanceStatus.Connected)
            {
                return false;
            }

            var prefabObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(cp);
            var path = AssetDatabase.GetAssetPath(prefabObject);
            var guid = AssetDatabase.AssetPathToGUID(path);

            if (ignorePrefabs.Contains(guid))
            {
                return true;
            }

            return false;
        }

        private void AddComponentIssue(string name, GameObject obj, Component component, ValidationLevel level)
        {
            switch (level)
            {
                case ValidationLevel.ALLOW:
                    break;
                case ValidationLevel.DISALLOW:
                    /* {0}:{1}の使用は許可されていません。 */
                    var message = AssetUtility.GetValidator("UsableComponentListRule.Disallow", name, component.GetType().Name);
                    var solution = "";
                    if (_showOfficialAssetsSolution)
                    {
                        /* "公式Prefabが用意されている場合は、そちらを使うことで使用することが可能です。" */
                        solution = AssetUtility.GetValidator("UsableComponentListRule.Disallow.Solution");
                    }

                    var solutionURL = AssetUtility.GetValidator("UsableComponentListRule.Disallow.SolutionURL");
                    
                    AddIssue(new Issue(obj, IssueLevel.Error, message, solution));
                    break;
            }
        }
    }
}
#endif