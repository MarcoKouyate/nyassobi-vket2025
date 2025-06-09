#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class DynamicRule : BaseRule
    {
        private bool isError;

        public DynamicRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            isError = false;
            var rootObjects = target.GetRootObjects();

            foreach (var rootObject in rootObjects)
            {
                Transform dynamicRoot = rootObject.transform.Find("Dynamic");
                if (dynamicRoot != null)
                {
                    CheckStatic(dynamicRoot.gameObject);
                }

            }

            if (isError)
            {
                var solution = AssetUtility.GetValidator("DynamicRule.Solution");
                AddIssue(new Issue(
                    null,
                    IssueLevel.Error,
                    solution));
            }
        }


        private void CheckStatic(GameObject obj)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                if (obj.transform.GetChild(i).gameObject.isStatic)
                {
                    isError = true;
                }
                CheckStatic(obj.transform.GetChild(i).gameObject);
            }
        }

    }
}
#endif