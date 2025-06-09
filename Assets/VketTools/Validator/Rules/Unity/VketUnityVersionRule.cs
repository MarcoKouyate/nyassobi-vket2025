using System.Linq;
using System.Text;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// 実行中Unityのバージョンを検証するルール
    /// </summary>
    public class VketUnityVersionRule : BaseRule
    {
        private readonly string[] versions;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ルール名</param>
        /// <param name="versions">許可するUnityバージョン配列</param>
        /// <remarks>`UnityEngine.Application.unityVersion`で取得した値と`version`で指定した値が同一か検証する。</remarks>
        public VketUnityVersionRule(string name, string[] versions = null) : base(name)
        {
            this.versions = versions ?? new[] { "2022.3.22f1" };
        }

        protected override void Logic(ValidationTarget target)
        {
            bool check = false;
            var sb = new StringBuilder();
            foreach (var version in versions)
            {
                if (Application.unityVersion == version)
                    check = true;

                sb.Append(version);
                if(versions.Last() != version)
                    sb.Append(" or ");
            }
            
            if (!check)
            {
                var message = AssetUtility.GetValidator("UnityVersionRule.InvalidVersion", Application.unityVersion);
                var solution = AssetUtility.GetValidator("UnityVersionRule.InvalidVersion.Solution", sb.ToString());
                AddIssue(new Issue(null, IssueLevel.Error, message, solution, string.Empty));
            }
        }
    }
}