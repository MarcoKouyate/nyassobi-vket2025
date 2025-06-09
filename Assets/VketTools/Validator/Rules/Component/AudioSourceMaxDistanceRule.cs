#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class AudioSourceMaxDistanceRule : BaseRule
    {
        private readonly float _maxDistance;
        
        /// <param name="maxDistance">はみ出し許容距離</param>
        public AudioSourceMaxDistanceRule(string name, float maxDistance) : base(name)
        {
            _maxDistance = maxDistance;
        }

        protected override void Logic(ValidationTarget target)
        {
            var assets = target.GetScenes()[0].GetRootGameObjects();

            foreach (var asset in assets)
            {
                if (asset.name != "ReferenceObjects")
                {
                    FindAudioSource(asset);
                }
            }
        }

        private void FindAudioSource(GameObject obj)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var audio = obj.transform.GetChild(i).GetComponent<AudioSource>();
                if (audio != null)
                {
                    if (_maxDistance < audio.maxDistance)
                    {
                        // Audio SourceのMaxDistanceは{0}以下に設定してください。
                        var solution = AssetUtility.GetValidator("AudioSourceRule.MaxDistance.Solution", _maxDistance);
                        AddIssue(new Issue(
                            null,
                            IssueLevel.Error,
                            solution));
                    }
                }

                var audioVRC = obj.transform.GetChild(i).GetComponent<VRC.SDK3.Components.VRCSpatialAudioSource>();
                if (audioVRC != null)
                {
                    if (_maxDistance < audioVRC.Far)
                    {
                        // VRCSpatialAudioSourceのFarは{0}以下に設定してください。
                        var solution = AssetUtility.GetValidator("AudioSourceRule.MaxDistance.Solution.VRCSpatialAudioSource", _maxDistance);
                        AddIssue(new Issue(
                            null,
                            IssueLevel.Error,
                            solution));
                    }
                }


                var audioReverb = obj.transform.GetChild(i).GetComponent<AudioReverbZone>();
                if (audioReverb != null)
                {
                    if (_maxDistance < audioReverb.maxDistance)
                    {
                        // Audio Reverb ZoneのMaxDistanceは{0}以下に設定してください。
                        var solution = AssetUtility.GetValidator("AudioSourceRule.MaxDistance.Solution.Reverb", _maxDistance);
                        AddIssue(new Issue(
                            null,
                            IssueLevel.Error,
                            solution));
                    }
                }
                FindAudioSource(obj.transform.GetChild(i).gameObject);
            }
        }
    }
}
#endif