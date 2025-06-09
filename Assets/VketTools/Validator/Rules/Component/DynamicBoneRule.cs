#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Reflection;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class DynamicBoneRule : BaseRule
    {
        public DynamicBoneRule(string name) : base(name)
        {
        }

        protected override void Logic(ValidationTarget target)
        {
            // DynamicBoneの存在チェック
            var assembly = Assembly.Load("Assembly-CSharp");
            if (assembly == null)
            {
                return;
            }
            var type = assembly.GetType("DynamicBone");
            if (type == null)
            {
                return;
            }
            var fieldInfo = type.GetField("m_DistantDisable", BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                return;
            }
            
            foreach (var gameObject in target.GetAllObjects())
            {
                var components = gameObject.GetComponents(type);
                foreach (var component in components)
                {
                    var value = (bool)fieldInfo.GetValue(component);
                    if (!value)
                    {
                        // DistantDisableが無効です。
                        var message = AssetUtility.GetValidator("DynamicBoneRule.DistantDisabled");
                        var solution = AssetUtility.GetValidator("DynamicBoneRule.DistantDisabled.Solution");

                        AddIssue(new Issue(
                            component,
                            IssueLevel.Error,
                            message,
                            solution));
                    }
                }
            }
        }
    }
}
#endif