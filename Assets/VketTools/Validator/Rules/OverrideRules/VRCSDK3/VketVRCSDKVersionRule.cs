#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    /// <summary>
    /// VRCSDKのバージョンを検出するルール
    /// </summary>
    /// <remarks>
    /// GUIDは可変である可能性があるのでファイルパスをチェックする。
    /// </remarks>
    public class VketVRCSDKVersionRule : BaseRule
    {
        const string manifestFilePath = "Packages/com.vrchat.worlds/package.json";

        private VRCSDKVersion _targetVersion;
        private readonly Action _betaAction;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="name">ルール名</param>
        /// <param name="version">VRCSDKのバージョン</param>
        public VketVRCSDKVersionRule(string name, VRCSDKVersion version, Action betaAction = null) : base(name)
        {
            _targetVersion = version;
            _betaAction = betaAction;
        }

        protected override void Logic(ValidationTarget target)
        {
            if (!File.Exists(manifestFilePath))
            {
                AddIssue(new Issue(null,
                    IssueLevel.Error,
                    AssetUtility.GetValidator("VRCSDKVersionRule.NotInstalled"),
                    AssetUtility.GetValidator("VRCSDKVersionRule.NotInstalled.Solution")
                ));
                return;
            }

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestFilePath));
            var version = manifest.version;
            if (Regex.Match(version, @"-.*$").Success)
            {
                version = Regex.Replace(version, @"-.*$", "");
                _betaAction?.Invoke();
            }
            
            var currentVersion = new VRCSDKVersion(version);

            if (currentVersion < _targetVersion)
            {
                AddIssue(new Issue(null,
                    IssueLevel.Error,
                    /* "VRCSDKが最新バージョンではありません。" */ AssetUtility.GetValidator("VRCSDKVersionRule.PreviousVersion"),
                    /* "VRChat Creator Companion から VRChat SDK - Worlds を最新バージョン {0} にしてください。" */ AssetUtility.GetValidator("VRCSDKVersionRule.PreviousVersion.Solution", _targetVersion.ToReadableString())
                ));
            }
        }

        [Serializable]
        private class Manifest
        {
            [SerializeField] public string version;
        }
    }
}
#endif