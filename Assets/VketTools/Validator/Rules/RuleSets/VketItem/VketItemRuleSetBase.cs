#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR

using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;

namespace VketTools.Validator.RuleSets
{

    /// <summary>
    /// Vket Itemの基本ルールセット。
    /// </summary>
    /// <remarks>
    /// 変数をabstractまたはvirtualプロパティで宣言し、継承先で上書きすることでワールドによる制限の違いを表現する。
    /// </remarks>
    public abstract class VketItemRuleSetBase : IRuleSet
    {
        public abstract string RuleSetName
        {
            get;
        }

        protected IOfficialAssetData _officialAssetData;

        protected readonly long MegaByte = 1048576;

        protected readonly VketTargetFinder targetFinder = new VketTargetFinder();
        public IValidationTargetFinder TargetFinder => targetFinder;

        public VketItemRuleSetBase(IOfficialAssetData officialAssetData) : base()
        {
            _officialAssetData = officialAssetData;
        }

        /// <summary>
        /// 入稿フォルダの最大容量
        /// </summary>
        protected abstract long FolderSizeLimit { get; }

        /// <summary>
        /// ブースのサイズ
        /// </summary>
        protected abstract Vector3 BoothSizeLimit { get; }
        
        /// <summary>
        /// Materialの最大数
        /// 0の場合はルール検証をスキップ
        /// </summary>
        protected abstract int MaterialUsesLimit { get; }

        private ComponentReference[] GetComponentReferences()
        {
            return new[] {
                new ComponentReference("Skinned Mesh Renderer", new[]{"UnityEngine.SkinnedMeshRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Mesh Renderer ", new[]{"UnityEngine.MeshRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Mesh Filter", new[]{"UnityEngine.MeshFilter"}, ValidationLevel.ALLOW),
                
                // ルート以外で使用していたら弾くのはVketItemRuleで行う
                new ComponentReference("Collider", new[]{"UnityEngine.BoxCollider"}, ValidationLevel.ALLOW),
                new ComponentReference("Rigidbody", new[]{"UnityEngine.Rigidbody"}, ValidationLevel.ALLOW),
                new ComponentReference("VRC Pickup", new[]{"VRC.SDKBase.VRC_Pickup", "VRC.SDK3.Components.VRCPickup"}, ValidationLevel.ALLOW),
                
                // Marche
                new ComponentReference("Vket Weapon", new [] { "VketMarche.VketWeapon" }, VketWeaponValidationLevel),
                
                // Formula
                new ComponentReference("Vket Kart", new [] { "VketFormula.VketKart" }, VketKartValidationLevel),
            };
        }
        
        /// <summary>
        /// VketWeaponの使用許可
        /// </summary>
        protected virtual ValidationLevel VketWeaponValidationLevel => ValidationLevel.DISALLOW;
        
        /// <summary>
        /// VketKartの使用許可
        /// </summary>
        protected virtual ValidationLevel VketKartValidationLevel => ValidationLevel.DISALLOW;
        
        public virtual IRule[] GetRules()
        {
            // デフォルトで使っていたAttribute式は宣言時にconst以外のメンバーが利用できない。
            // 継承したプロパティを参照して挙動を変えることが出来ない為、直接リストを返す方式。
            return new IRule[]
            {
                // Unityバージョンルール
                new VketUnityVersionRule(AssetUtility.GetValidator("VketRuleSetBase.UnityVersionRule.Title", "2022.3.22f1"), new []{ "2022.3.22f1" }),
                
                // SDKバージョンルール
                new VketVRCSDKVersionRule(AssetUtility.GetValidator("VketRuleSetBase.VRCSDKVersionRule.Title"), new VRCSDKVersion("3.7.1"),
                    () =>
                    {
                        if (VketTools.Utilities.Hiding.HidingUtil.DebugMode)
                        {
                            EditorUtility.DisplayDialog("VketToolsリリース前に確認してください。", "VRCSDKがβ版です。\nそのままリリースするとエラーが起きる可能性があります。", "OK");
                        }
                    }),

                // 入稿不可のシェーダールール
                // Unityのビルトインシェーダーと同名のシェーダーは使用できません。
                new ShaderNameBlockRule(AssetUtility.GetValidator("VketRuleSetBase.ShaderNameBlockRule.Title"), _officialAssetData.DeniedShaderNames),
                
                // シェーダー規定の制約 - 機械的に検知することは不完全であり、やりすぎなため廃止
                // new VketShaderRule(AssetUtility.GetValidator("VketUdonRuleSetBase.VketShaderRule.Title"), _officialAssetData.GUIDs),
                
                // 配布物以外のすべてのオブジェクトが入稿フォルダ内にあるか検出するルール
                new VketExistInSubmitFolderRule(AssetUtility.GetValidator("VketRuleSetBase.ExistInSubmitFolderRule.Title"), _officialAssetData.GUIDs, targetFinder),

                // 配布アセットが入稿フォルダ内に入っていないか確認
                new AssetGuidBlacklistRule(AssetUtility.GetValidator("VketRuleSetBase.OfficialAssetDontContainRule.Title"), _officialAssetData.GUIDs),

                // ファイル名とフォルダ名の使用禁止文字ルール
                new VketAssetNamingRule(AssetUtility.GetValidator("VketRuleSetBase.NameOfFileAndFolderRule.Title"), _officialAssetData.GUIDs, @"[a-zA-Z0-9 _\.\-\(\)]+"),

                // ファイルパスの長さが規定値を超えていないか検出するルール
                new VketAssetPathLengthRule(AssetUtility.GetValidator("VketRuleSetBase.FilePathLengthLimitRule.Title", 128), _officialAssetData.GUIDs, 128),

                // 特定の拡張子を持つアセットを検出するルール(メッシュアセットのファイル形式で特定のものが含まれていないこと)
                new AssetExtentionBlacklistRule(AssetUtility.GetValidator("VketRuleSetBase.MeshFileTypeBlacklistRule.Title"),
                                                new string[]{".ma", ".mb", "max", "c4d", ".blend"}
                ),

                // アセットに埋め込まれたMaterialもしくはTextureを使っていないこと
                new ContainMatOrTexInAssetRule(AssetUtility.GetValidator("VketRuleSetBase.ContainMatOrTexInAssetRule.Title")),

                // フォルダサイズが設定値を超えていないか検出
                new FolderSizeRule(AssetUtility.GetValidator("VketRuleSetBase.FolderSizeRule.Title"), FolderSizeLimit),
                
                // VketItem入稿用のルール
                // Pickup動作確認のため追加
                // VRCPickup, BoxCollider, RigidBodyはRootのオブジェクト以外では使用できない
                new VketItemRule(AssetUtility.GetValidator("VketItemRuleSetBase.VketItemRule.Title")),
                
                // 入稿シーンの構造が正しいか
                new VketSceneStructureRule(AssetUtility.GetValidator("VketRuleSetBase.SceneStructureRule.Title")),
                
                // Staticフラグが正しいか、特定のStatic設定によるルールの適用
                // アイテムの場合、Static禁止
                new VketStaticFlagRule(AssetUtility.GetValidator("VketItemRuleSetBase.StaticFlagsRule.Title"), true),
                
                // EditorOnlyタグは使用不可
                new DisallowEditorOnlyTagRule(AssetUtility.GetValidator("VketRuleSetBase.DisallowEditorOnlyTagRule.Title"), true),

                // ブースサイズのルール
                new VRCBoothBoundsRule(AssetUtility.GetValidator("VketRuleSetBase.BoothBoundsRule.Title"),
                    size: BoothSizeLimit,
                    margin: 0.01f,
                    Vector3.zero,
                    _officialAssetData.SizeIgnorePrefabGUIDs),

                // Material使用数が最大値を超えていないか
                new VketAssetTypeLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.MaterialLimitRule.Title", MaterialUsesLimit),
                    typeof(Material),
                    MaterialUsesLimit,
                    _officialAssetData.MaterialGUIDs),
                
                // シェーダーエラーの検出
                // マテリアルが設定されていない、シェーダーエラー、シェーダーがnullであることを検出
                new VketErrorShaderRule(AssetUtility.GetValidator("Booth.ErrorShaderRule.Title")), 
                // // シェーダーホワイトリストのチェック
                // new ShaderWhitelistRule(AssetUtility.Get("Booth.ShaderWhiteListRule.Title"),
                //     _officialAssetData.AllowedShaders, AssetUtility.Get("Booth.ShaderWhiteListRule.Solution")), 
                
                // Location は Use Embedded Materials に設定すること
                new FBXImportRule(AssetUtility.GetValidator("FBXImportRule.Title")),
                
                // 使用不可なコンポーネントの検出
                // ~の使用は許可されていません。
                new VketUsableComponentListRule(AssetUtility.GetValidator("VketRuleSetBase.UsableComponentListRule.Title"),
                    GetComponentReferences(), ignorePrefabGUIDs: _officialAssetData.GUIDs, false, ValidationLevel.DISALLOW),

                // SkinnedMeshRendererのルール
                // Update When Offscreenを無効
                // Materials 0のSkinned Mesh Rendererは禁止
                new VketSkinnedMeshRendererRule(AssetUtility.GetValidator("VketRuleSetBase.SkinnedMeshRendererRule.Title")),

                // MeshRendererのルール
                // ContributeGI をチェックしたオブジェクトは、Layer割り当てをEnvironmentとしてください
                // Materials 0のMesh Rendererは禁止
                new VketMeshRendererRule(AssetUtility.GetValidator("VketRuleSetBase.MeshRendererRule.Title")),
                
                // ExistInSubmitFolderRule ではVket・VRC公式扱いのPrefabは制限されないので、禁止するPrefabはここで止める
                // （PickupやVideoPlayerのようなPrefab個数制限ルールを転用）
                // #302
                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.UnusabePrefabRule.Title"),
                    _officialAssetData.VRCSDKForbiddenPrefabGUIDs,
                    0),
            };
        }
    }
}
#endif
