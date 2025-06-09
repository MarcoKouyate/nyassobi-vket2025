#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class VketExhibitStructureRule : BaseRule
    {
        private bool isEnabled;
        private bool onlyDynamic;
        public VketExhibitStructureRule(string name,bool isEnabled,bool onlyDynamic) : base(name)
        {
            this.isEnabled = isEnabled;
            this.onlyDynamic = onlyDynamic;
        }

        protected override void Logic(ValidationTarget target)
        {
            if (isEnabled)
            {
                var rootObjects = target.GetRootObjects();

                foreach (var rootObject in rootObjects)
                {
                    LogicForRootObject(rootObject);
                }
            }
        }

        void LogicForRootObject(GameObject rootObject)
        {
            CheckObjectIdentity(rootObject);
            
            GameObject staticRoot = null;
            GameObject dynamicRoot = null;

            foreach (Transform child in rootObject.transform)
            {
                if (child.name == "Static" && staticRoot == null)
                {
                    staticRoot = child.gameObject;
                }
                else if (child.name == "Dynamic" && dynamicRoot == null)
                {
                    dynamicRoot = child.gameObject;
                }
                else if (onlyDynamic)
                {

                }
                else
                {
                    AddIssue(new Issue(
                        child,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("ExhibitStructureRule.UnauthorizedObject"),
                        AssetUtility.GetValidator("ExhibitStructureRule.UnauthorizedObject.Solution")
                        ));
                }
            }

            CheckRootObject(rootObject, "Static", staticRoot);
            CheckRootObject(rootObject, "Dynamic", dynamicRoot);
        }

        private void CheckRootObject(GameObject rootObject, string instanceName, GameObject instance)
        {
            if (onlyDynamic && instanceName == "Static")
                return;
            if (instance == null)
            {
                AddIssue(new Issue(
                        rootObject,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("ExhibitStructureRule.RootObjectNotFound", instanceName),
                        AssetUtility.GetValidator("ExhibitStructureRule.RootObjectNotFound.Solution", instanceName)
                        ));
                return;
            }

            CheckObjectIdentity(instance);
            
            var components = instance.GetComponents<Component>();

            if (components.Length > 1 || components[0] is RectTransform)
            {
                AddIssue(new Issue(
                        instance,
                        IssueLevel.Error,
                        AssetUtility.GetValidator("ExhibitStructureRule.UnauthorizedComponent", instanceName),
                        AssetUtility.GetValidator("ExhibitStructureRule.UnauthorizedComponent.Solution", instanceName)
                        ));
            }
        }
        
        private void CheckObjectIdentity(GameObject instance)
        {
            var transform = instance.transform;
            if (transform.localPosition != Vector3.zero)
            {
                var message = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialPosition");
                var solution = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialPosition.Solution");
                var solutionURL = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialPosition.SolutionURL");
                AddIssue(new Issue(
                    instance,
                    IssueLevel.Error,
                    message, solution, solutionURL));
            }
            if (transform.localRotation != Quaternion.identity)
            {
                var message = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialRotation");
                var solution = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialRotation.Solution");
                var solutionURL = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialRotation.SolutionURL");
                AddIssue(new Issue(
                    instance,
                    IssueLevel.Error,
                    message, solution, solutionURL));
            }
            if (transform.localScale != Vector3.one)
            {
                var message = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialScale");
                var solution = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialScale.Solution");
                var solutionURL = AssetUtility.GetValidator("ExhibitStructureRule.NotInitialScale.SolutionURL");
                AddIssue(new Issue(
                    instance,
                    IssueLevel.Error,
                    message, solution, solutionURL));
            }
        }
    }
}
#endif
