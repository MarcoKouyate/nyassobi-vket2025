#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator
{
    public class BoothOuterRule2023SummerPlaza : BaseRule
    {
        public enum BoothOuterType
        {
            Lake0 = 0,
            Lake1,
            Lake2,
            Volcano0,
            Volcano1,
            Volcano2,
        }

        static readonly int LakeVillageWorldId = 20;
        static readonly int VolcanoCityWorldId = 21;
        static readonly string FrameParentObjectName = "Plaza_BoothFrame";

        // 初回アクセス時にデータを定義してキャッシュをもつ
        static Dictionary<string, BoothOuterType> _dicOuters = null;
        public static Dictionary<string, BoothOuterType> DicOuters
            => _dicOuters ??= new Dictionary<string, BoothOuterType>()
            {
                { "BoothCageSet_Lake_0", BoothOuterType.Lake0 },
                { "BoothCageSet_Lake_1", BoothOuterType.Lake1 },
                { "BoothCageSet_Lake_2", BoothOuterType.Lake2 },
                { "BoothCageSet_Volcano_0", BoothOuterType.Volcano0 },
                { "BoothCageSet_Volcano_1", BoothOuterType.Volcano1 },
                { "BoothCageSet_Volcano_2", BoothOuterType.Volcano2 },
            };

        int _worldId = 0;

        public BoothOuterRule2023SummerPlaza(string name, int worldId) : base(name)
        {
            _worldId = worldId;
        }

        protected override void Logic(ValidationTarget target)
        {
            // Plaza以外のワールドはチェックしない
            if (_worldId != LakeVillageWorldId && _worldId != VolcanoCityWorldId)
                return;

            var outerType = GetBoothOuterType();

            // 外観オブジェクトが正常に有効になっていない場合のエラー
            if (outerType == null)
            {
                var message = AssetUtility.GetValidator("BoothOuterRule2023SummerPlaza.Invalid");
                var solution = AssetUtility.GetValidator("BoothOuterRule2023SummerPlaza.Invalid.Solution");
                AddIssue(new Issue(
                    null,
                    IssueLevel.Error,
                    message,
                    solution));
                return;
            }

            var isLakeWorld = _worldId == LakeVillageWorldId;
            var isLakeOuter = outerType == BoothOuterType.Lake0 || outerType == BoothOuterType.Lake1 || outerType == BoothOuterType.Lake2;

            // ワールドと外観が一致しない場合のエラー
            if (isLakeWorld != isLakeOuter)
            {
                var message = AssetUtility.GetValidator("BoothOuterRule2023SummerPlaza.DifferenceWorld");
                var solution = AssetUtility.GetValidator("BoothOuterRule2023SummerPlaza.DifferenceWorld.Solution");
                AddIssue(new Issue(
                    null,
                    IssueLevel.Error,
                    message,
                    solution));
                return;
            }
        }

        public static BoothOuterType? GetBoothOuterType()
        {
            // 外観オブジェクトの親を取得
            var frameParent = GameObject.Find(FrameParentObjectName);
            if (frameParent == null)
                return null;

            // アクティブな外観オブジェクトを取得
            var frameObjects = Enumerable.Range(0, frameParent.transform.childCount).Select(i => frameParent.transform.GetChild(i).gameObject);
            var activeFrameObjects = frameObjects.Where(o => o?.activeInHierarchy == true);

            // アクティブな外観オブジェクトが存在しないor複数ある場合はnullを返す
            if (!activeFrameObjects.Any() || activeFrameObjects.Count() > 1)
                return null;

            // アクティブな外観オブジェクトの名前から外観タイプを取得して返す
            if (DicOuters.TryGetValue(activeFrameObjects.FirstOrDefault()?.name, out var outerType))
                return outerType;
            else
                return null;
        }
    }
}
#endif