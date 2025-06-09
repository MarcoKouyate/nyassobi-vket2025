#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Linq;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.SDK3.Components;

namespace VketTools.Validator
{
    /// <summary>
    /// ルートオブジェクト以外にPickupを付けないためのルール
    /// BoxCollider, Rigidbody, VRCPickupの有無を判別
    /// </summary>
    public class VketItemRule : BaseRule
    {
        public VketItemRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            var rootObjects = target.GetRootObjects();
            var targetObjects = target.GetAllObjects().ToList();
            // ルートオブジェクトをチェック対象から除外
            foreach (var rootObject in rootObjects)
            {
                targetObjects.Remove(rootObject);
            }
            
            // ルートオブジェクトのChildがチェック対象
            foreach (var targetObject in targetObjects)
            {
                LogicForItemRule(targetObject);
            }
        }

        void LogicForItemRule(GameObject targetObject)
        {
            if (targetObject.GetComponent<BoxCollider>())
            {
                AddIssue(new Issue(
                    targetObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketItemRule.UnauthorizedBoxCollider"),
                    AssetUtility.GetValidator("VketItemRule.UnauthorizedBoxCollider.Solution")
                ));
            }
            
            if (targetObject.GetComponent<Rigidbody>())
            {
                AddIssue(new Issue(
                    targetObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketItemRule.UnauthorizedRigidbody"),
                    AssetUtility.GetValidator("VketItemRule.UnauthorizedRigidbody.Solution")
                ));
            }
            
            if (targetObject.GetComponent<VRCPickup>())
            {
                AddIssue(new Issue(
                    targetObject,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VketItemRule.UnauthorizedVRCPickup"),
                    AssetUtility.GetValidator("VketItemRule.UnauthorizedVRCPickup.Solution")
                ));
            }
        }
    }
}
#endif
