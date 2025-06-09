#if VRC_SDK_VRCSDK3 && VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using UnityEditor;
using UnityEngine;
using VitDeck.Validator;
using VketTools.Utilities;
using VRC.SDK3.Components;
using VRC.Udon;

namespace VketTools.Validator.RuleSets
{

    /// <summary>
    /// Vket Udonの基本ルールセット。
    /// </summary>
    /// <remarks>
    /// 変数をabstractまたはvirtualプロパティで宣言し、継承先で上書きすることでワールドによる制限の違いを表現する。
    /// </remarks>
    public abstract class VketUdonRuleSetBase : IRuleSet
    {
        public abstract string RuleSetName
        {
            get;
        }

        protected IOfficialAssetData _officialAssetData;

        protected readonly long MegaByte = 1048576;

        protected readonly VketTargetFinder targetFinder = new();
        public IValidationTargetFinder TargetFinder => targetFinder;

        protected IObjectDetector officialPrefabsDetector;

        protected VketUdonRuleSetBase(IOfficialAssetData officialAssetData)
        {
            _officialAssetData = officialAssetData;
            officialPrefabsDetector = new PrefabPartsDetector(
                _officialAssetData.AudioSourcePrefabGUIDs,
                _officialAssetData.AvatarPedestalPrefabGUIDs,
                _officialAssetData.PickupObjectSyncPrefabGUIDs,
                _officialAssetData.VketMirrorPrefabGUIDs,
                _officialAssetData.CanvasPrefabGUIDs,
                _officialAssetData.PointLightProbeGUIDs,
                _officialAssetData.UdonBehaviourPrefabGUIDs);
        }

        /// <summary>
        /// ルールの取得
        /// ブースのルールに応じてオーバーライドして変更可能
        /// </summary>
        /// <returns></returns>
        public virtual IRule[] GetRules()
        {
            // デフォルトで使っていたAttribute式は宣言時にconst以外のメンバーが利用できない。
            // 継承したプロパティを参照して挙動を変えることが出来ない為、直接リストを返す方式に変更した。
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
                                                new []{".ma", ".mb", "max", "c4d", ".blend"}
                ),

                // アセットに埋め込まれたMaterialもしくはTextureを使っていないこと
                new ContainMatOrTexInAssetRule(AssetUtility.GetValidator("VketRuleSetBase.ContainMatOrTexInAssetRule.Title")),

                // フォルダサイズが設定値を超えていないか検出
                new FolderSizeRule(AssetUtility.GetValidator("VketRuleSetBase.FolderSizeRule.Title"), FolderSizeLimit),

                // Static,Dynamicの2つのEmptyオブジェクトを作り、すべてのオブジェクトはこのどちらかの階層下に入れること
                new VketExhibitStructureRule(AssetUtility.GetValidator("VketRuleSetBase.ExhibitStructureRule.Title"), ExhibitStructureRuleIsEnabled,ExhibitStructureRuleOnlyDynamic),

                // 入稿シーンの構造が正しいか
                new VketSceneStructureRule(AssetUtility.GetValidator("VketRuleSetBase.SceneStructureRule.Title")),
                
                // Staticフラグが正しいか、特定のStatic設定によるルールの適用
                new VketStaticFlagRule(AssetUtility.GetValidator("VketRuleSetBase.StaticFlagsRule.Title")),

                // Dynamicオブジェクト群のルール
                new DynamicRule(AssetUtility.GetValidator("VketRuleSetBase.DynamicRule.Title")),

                // EditorOnlyタグは使用不可
                new DisallowEditorOnlyTagRule(AssetUtility.GetValidator("VketRuleSetBase.DisallowEditorOnlyTagRule.Title")),

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

                // LightMapが最大値を超えていないか
                // LightMapの使用枚数が{0}枚以下、解像度は{1}以下、総面積は{2}以下であること
                new VketLightmapSizeLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.LightMapsLimitRule.Title", LightmapCountLimit, LightmapSizeLimit, LightmapSizeLimit * LightmapSizeLimit * LightmapCountLimit),
                    lightmapCountLimit: LightmapCountLimit,
                    lightmapAreaLimit: LightmapSizeLimit * LightmapSizeLimit * LightmapCountLimit),

                // staticでContributeGIにチェックが入っているオブジェクト(ScalesInLightmapの値が0のものは除く)の中で、ライトマップがオーバーラップ状態になっているものは入稿できない
                new LightMapOverlapsRule(AssetUtility.GetValidator("VketRuleSetBase.LightMapOverlapsRule.Title")),
                
                // シェーダーエラーの検出
                // マテリアルが設定されていない、シェーダーエラー、シェーダーがnullであることを検出
                new VketErrorShaderRule(AssetUtility.GetValidator("Booth.ErrorShaderRule.Title")),

                // Location は Use Embedded Materials に設定すること
                new FBXImportRule(AssetUtility.GetValidator("FBXImportRule.Title")),
                
                // 使用不可なコンポーネントの検出
                // ~の使用は許可されていません。
                // Udonコンポーネントの名称は自由に決められるためブラックリスト形式
                new VketUsableComponentListRule(AssetUtility.GetValidator("VketRuleSetBase.UsableComponentListRule.Title"),
                    GetComponentReferences(),
                    ignorePrefabGUIDs: _officialAssetData.GUIDs),

                // SkinnedMeshRendererのルール
                // Update When Offscreenを無効
                // Materials 0のSkinned Mesh Rendererは禁止
                new VketSkinnedMeshRendererRule(AssetUtility.GetValidator("VketRuleSetBase.SkinnedMeshRendererRule.Title")),

                // MeshRendererのルール
                // ContributeGI をチェックしたオブジェクトは、Layer割り当てをEnvironmentとしてください
                // Materials 0のMesh Rendererは禁止
                // TODO: Lightmap Parametersの変更は禁止。
                new VketMeshRendererRule(AssetUtility.GetValidator("VketRuleSetBase.MeshRendererRule.Title")),

                // ReflectionProbeのルール
                // TypeをBakedまたはCustomに設定すること。
                // Resolutionは128まで。
                new VketReflectionProbeRule(AssetUtility.GetValidator("VketRuleSetBase.ReflectionProbeRule.Title")),

                // MeshCollider以外のColliderを使用すること
                // エラーではなくWarning表示
                new UseMeshColliderRule(AssetUtility.GetValidator("VketRuleSetBase.UseMeshColliderRule.Title")),

                // DirectionalLightを使用しないこと
                new VketLightCountLimitRule(AssetUtility.GetValidator("VketRuleSetBase.DirectionalLightLimitRule.Title"), VketLightType.Directional, 0),

                // AreaLight個数制限
                new VketLightCountLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.AreaLightLimitRule.Title", AreaLightUsesLimit),
                    VketLightType.Area,
                    AreaLightUsesLimit),
                
                // PointLight
                new VketLightConfigRule(AssetUtility.GetValidator("VketRuleSetBase.PointLightConfigRule.Title"), VketLightType.Point, ApprovedPointLightConfig),
                // SpotLight
                new VketLightConfigRule(AssetUtility.GetValidator("VketRuleSetBase.SpotLightConfigRule.Title"), VketLightType.Spot, ApprovedSpotLightConfig),
                // AreaLight
                new VketLightConfigRule(AssetUtility.GetValidator("VketRuleSetBase.AreaLightConfigRule.Title"), VketLightType.Area, ApprovedAreaLightConfig),
                
                // AnimationClip内で../を含んだパスを利用する事は出来ません。
                // AnimationClip内でMaterialの変更は出来ません。
                //new VketAnimationClipRule(AssetUtility.GetValidator("VketRuleSetBase.AnimationClipRule.Title")),

                // Animatorコンポーネントの使用数は制限に収めること
                new SceneComponentLimitRule(AssetUtility.GetValidator("VketRuleSetBase.AnimatorComponentMaxCountRule.Title"),typeof(Animator), limit: AnimatorCountLimit,_officialAssetData.UdonBehaviourPrefabGUIDs),
                
                // 不具合が出る場合を除き、CullingTypeはAlwaysを避けて下さい。
                new AnimationComponentRule(AssetUtility.GetValidator("VketRuleSetBase.AnimationComponentRule.Title"), officialPrefabsDetector),
                
                // オブジェクトのLayerはWaterに設定すること。
                // Maximum AntialiasingでX8は使用出来ません。
                new VketMirrorRule(AssetUtility.GetValidator("VketMirrorRule.Title")),
                
                // Apply Root Motionは使用できません。
                // 不具合が出る場合を除き、CullingModeはAlwaysを避けて下さい。
                // Animatorと{第二引数指定のコンポーネント}を併用することは出来ません。
                new AnimatorComponentRule(AssetUtility.GetValidator("VketRuleSetBase.AnimatorComponentRule.Title"),
                    new System.Type[]
                    {
                        // typeof(VRC_Pickup),
                        // typeof(VRC_ObjectSync)
                    }, officialPrefabsDetector),
                
                new DynamicBoneRule(AssetUtility.GetValidator("VketRuleSetBase.DynamicBoneRule.Title")),
                
                // AudioSourceのEnabledはアニメーションから操作できません。
                new AnimationClipRule(AssetUtility.GetValidator("AnimationClipRule.Title")),

                // CanvasはWorldSpaceに設定すること
                new CanvasRenderModeRule(AssetUtility.GetValidator("VketRuleSetBase.CanvasRenderModeRule.Title")),

                // Cameraコンポーネントの使用数は制限に収めること
                new CameraComponentMaxCountRule(AssetUtility.GetValidator("VketRuleSetBase.CameraComponentMaxCountRule.Title"), limit: CameraCountLimit),

                // Cameraを使用する際は制限に従うこと
                // コンポーネントは初期状態でDisabledにする必要があります。
                // TargetTextureには必ずRenderTextureを指定してください
                new VketCameraComponentRule(AssetUtility.GetValidator("VketRuleSetBase.CameraComponentRule.Title")),

                // RenderTextureの枚数制限を超えないこと
                new RenderTextureMaxCountRule(AssetUtility.GetValidator("VketRuleSetBase.RenderTextureMaxCountRule.Title"), limit: RenderTextureCountLimit),

                // RenderTextureのサイズ制限を超えないこと
                new RenderTextureMaxSizeRule(AssetUtility.GetValidator("VketRuleSetBase.RenderTextureMaxSizeRule.Title"), limitSize: RenderTextureSizeLimit),

                // Projectorルール
                // コンポーネントは初期状態でDisabledにする必要があります。
                new VketProjectorComponentRule(AssetUtility.GetValidator("VketRuleSetBase.ProjectorComponentRule.Title")),

                // Projectorコンポーネントの使用数は制限に収めること
                new ProjectorComponentMaxCountRule(AssetUtility.GetValidator("VketRuleSetBase.ProjectorComponentMaxCountRule.Title"), limit: 1),
                
                // VRC Object Sync数制限
                new SceneComponentLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.VRCObjectSyncLimitRule.Title", VRCObjectSyncCountLimit),
                    typeof(VRCObjectSync),
                    VRCObjectSyncCountLimit,
                    _officialAssetData.UdonBehaviourPrefabGUIDs),
                
                // VRCObjectSyncのルール
                // AllowOwnershipTransferOnCollisionは必ずFalseにすること
                new VRCObjectSyncAllowOwnershipTransferOnCollisionIsFalseRule(AssetUtility.GetValidator("VRCObjectSyncAllowOwnershipTransferOnCollisionIsFalseRule.Title")),
                
                // VRC_Triggerが持つBroadcastTypeはlocalでなければなりません。
                // VRC_TriggerのTriggerTypeは、[Custom, OnInteract, OnEnterTrigger, OnExitTrigger, OnPickup, OnDrop, OnPickupUseDown, OnPickupUseUp]のいずれかである必要があります。{0}は使えません。
                // VRC_TriggerのSendRPCはSetAvataUse以外使用できません。
                // VRC_TriggerのActionは、[AnimationFloat, AnimationBool, AnimationTrigger, ActivateCustomTrigger, AudioTrigger, PlayAnimation, SetComponentActive, SetGameObjectActive, SendRPC(SetAvatarUse)]のいずれかである必要があります。{0}は使えません。
                new AvatarPedestalPrefabRule(AssetUtility.GetValidator("VketRuleSetBase.AvatarPedestalPrefabRule.Title"), _officialAssetData.AvatarPedestalPrefabGUIDs),

                // loop設定をオンに変更することは出来ません。
                // MaxDistanceの値をPrefabの値({0})より大きい値({1})にすることは出来ません。
                new AudioSourcePrefabRule(AssetUtility.GetValidator("VketRuleSetBase.AudioSourcePrefabRule.Title"),  _officialAssetData.AudioSourcePrefabGUIDs),

                #region VketPrefabsの使用数制限
                
                // ExistInSubmitFolderRule ではVket・VRC公式扱いのPrefabは制限されないので、禁止するPrefabはここで止める
                // （PickupやVideoPlayerのようなPrefab個数制限ルールを転用）
                // #302
                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.UnusabePrefabRule.Title"),
                    _officialAssetData.VRCSDKForbiddenPrefabGUIDs,
                    0),

                // Prefabの使用数制限
                // VketPickup, VketFollowPickupのカウント
                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.PickupObjectSyncPrefabLimitRule.Title", PickupObjectSyncUsesLimit),
                    _officialAssetData.PickupObjectSyncPrefabGUIDs,
                    PickupObjectSyncUsesLimit),

                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.VketVideoPlayerPrefabLimitRule.Title", VketVideoPlayerUsesLimit),
                    _officialAssetData.VideoPlayerPrefabGUIDs,
                    VketVideoPlayerUsesLimit),

                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.ImageDownloaderPrefabLimitRule.Title", VketImageDownloaderUsesLimit),
                    _officialAssetData.ImageDownloaderPrefabGUIDs,
                    VketImageDownloaderUsesLimit),

                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.StringDownloaderPrefabLimitRule.Title", VketStringDownloaderUsesLimit),
                    _officialAssetData.StringDownloaderPrefabGUIDs,
                    VketStringDownloaderUsesLimit),
                
                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.VketMirrorPrefabLimitRule.Title", VketMirrorUsesLimit),
                    _officialAssetData.VketMirrorPrefabGUIDs,
                    VketMirrorUsesLimit),
                
                new PrefabLimitRule(
                    AssetUtility.GetValidator("VketRuleSetBase.StarshipTreasurePrefabLimitRule.Title", VketStarshipTreasureUsesLimit),
                    _officialAssetData.StarshipTreasurePrefabGUIDs,
                    VketStarshipTreasureUsesLimit),
                
                #endregion
                
                // Udon Behaviour
                // UdonBehaviourを含むオブジェクト、UdonBehaviourによって操作を行うオブジェクトは全て入稿ルール C.Scene内階層規定におけるDynamicオブジェクトの階層下に入れてください
                new VketUdonDynamicObjectParentRule(AssetUtility.GetValidator("VketUdonRuleSetBase.UdonDynamicObjectParentRule.Title"), _officialAssetData.UdonBehaviourGlobalLinkGUIDs, UdonDynamicObjectParentRuleIsEnabled), 
                
                // 全てのUdonBehaviourオブジェクトの親であるDynamicオブジェクトは初期でInactive状態にしてください(第二引数がtrueの場合のみルール適用)
                new VketUdonDynamicObjectInactiveRule(AssetUtility.GetValidator("VketUdonRuleSetBase.UdonDynamicObjectInactiveRule.Title"), UdonInactiveRuleIsEnabled), 
                
                // 参照型(非プリミティブ型)のシリアライズ配列が初期化されていないこと
                new UdonBehaviourSerializeArrayRule(AssetUtility.GetValidator("VketUdonRuleSetBase.UdonBehaviourSerializeArrayRule.Title"), _officialAssetData.UdonBehaviourGlobalLinkGUIDs),
                
                // UdonBehaviour数制限
                new SceneComponentLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.UdonBehaviourLimitRule.Title", UdonBehaviourCountLimit),
                    typeof(UdonBehaviour),
                    UdonBehaviourCountLimit,
                    _officialAssetData.UdonBehaviourPrefabGUIDs),

                // VRC Object Pool数制限
                new SceneComponentLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.VRCObjectPoolLimitRule.Title", VRCObjectPoolCountLimit),
                    typeof(VRCObjectPool),
                    VRCObjectPoolCountLimit,
                    _officialAssetData.UdonBehaviourPrefabGUIDs),

                // VRCObjectPoolのPoolに登録するオブジェクト数制限
                new VRCObjectPoolPoolLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.VRCObjectPoolPoolLimitRule.Title", VRCObjectPoolPoolLimit),
                    VRCObjectPoolPoolLimit),

                // VRC Pickup数制限
                new SceneComponentLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.VRCObjectPickupLimitRule.Title", VRCPickupCountLimit),
                    typeof(VRCPickup),
                    VRCPickupCountLimit,
                    _officialAssetData.GUIDs),

                // Cloth数制限
                new SceneComponentLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.ClothLimitRule.Title", ClothCountLimit),
                    typeof(Cloth),
                    ClothCountLimit,
                    _officialAssetData.GUIDs),

                // AudioSource数制限
                new SceneComponentLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.AudioSourceLimitRule.Title", AudioSourceCountLimit),
                    typeof(AudioSource),
                    AudioSourceCountLimit,
                    _officialAssetData.GUIDs),

                // SynchronizePositionが有効なUdonBehaviour数制限
                new UdonBehaviourSynchronizePositionCountLimitRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.UdonBehaviourSynchronizePositionCountLimitRule.Title", UdonBehaviourSynchronizePositionCountLimit),
                    UdonBehaviourSynchronizePositionCountLimit
                ),

                // Continuous同期は使わないこと
                new UdonBehaviourSyncModeRule(
                    AssetUtility.GetValidator("VketUdonRuleSetBase.UdonBehaviourSyncModeRule.Title"),
                    _officialAssetData.ContinueIgnoreUdonGUIDs
                ),

                // VRCStation数制限
                new VRCStationCountLimitRule(AssetUtility.GetValidator("VketUdonRuleSetBase.VRCStationCountLimitRule.Title", VRCStationCountLimit), VRCStationCountLimit), 

                // VRCSpatialAudioSourceを含むオブジェクトは全てDynamicオブジェクトの階層下に入れてください
                new VketVRCSpatialAudioSourceDynamicObjectParentRule(AssetUtility.GetValidator("VketUdonRuleSetBase.SpatialAudioDynamicObjectParentRule.Title")), 
                
                // AudioSource [{0}] は Dynamic オブジェクトの子でなければなりません
                new VketAudioSourceDynamicObjectParentRule(/* "AudioSourceを含むオブジェクトは全てDynamicオブジェクトの子に配置されること" */ AssetUtility.GetValidator("VketUdonRuleSetBase.VketAudioSourceDynamicObjectParentRule.Title")), 
                
                // Rigidbody [{0}] は Dynamic オブジェクトの子でなければなりません
                new VketRigidbodyDynamicObjectParentRule(/* "RigidBodyを含むオブジェクトは全てDynamicオブジェクトの子に配置されること" */ AssetUtility.GetValidator("VketUdonRuleSetBase.VketRigidbodyDynamicObjectParentRule.Title")), 
                
                // ParticleSystem [{0}] は Dynamic オブジェクトの子でなければなりません
                new VketParticleSystemDynamicObjectParentRule(/* "ParticleSystemを含むオブジェクトは全てDynamicオブジェクトの子に配置されること" */ AssetUtility.GetValidator("VketUdonRuleSetBase.VketParticleSystemDynamicObjectParentRule.Title")), 
                
                // UdonBehaviourの制約
                // ProgramSourceがNoneのUdonBehaviourは使用しないこと
                new UdonBehaviourProgramSourceRule(AssetUtility.GetValidator("UdonBehaviourProgramSourceRule.Title")),
                
                // Udon Script
                // 使用禁止Udon関数
                new VketUsableUdonAssemblyListRule(AssetUtility.GetValidator("VketUdonRuleSetBase.UsableUdonAssemblyListRule.Title"),
                    GetUdonAssemblyReferences(),
                    ignoreUdonProgramGUIDs: _officialAssetData.GUIDs), 

                // [UdonSynced]を付与した変数の使用数制限
                // [UdonSynced]を付与した変数は下記の型のみ使用できます bool, sbyte, byte, ushort, short, uint, int, float
                new VketUdonBehaviourSyncedVariablesRule(AssetUtility.GetValidator("VketUdonRuleSetBase.UdonBehaviourSyncedVariablesRule.Title"), UdonScriptSyncedVariablesLimit, _officialAssetData.GUIDs),
                
                // Is Kinematicを有効にすること
                new VketRigidbodyRule(AssetUtility.GetValidator("VketRuleSetBase.RigidbodyRule.Title"),AllowIsKinematic),
                
                // AutoPlayを無効にすること
                new VRCAVProVideoPlayerRule(/* "VRCAVProVideoPlayerのルールに違反しています。" */ AssetUtility.GetValidator("VketRuleSetBase.VRCAVProVideoPlayerRule.Title")),
                
                // Use Shared Materialを無効にすること
                new VRCAVProVideoScreenRule(/* "VRCAVProVideoScreenのルールに違反しています。" */ AssetUtility.GetValidator("VketRuleSetBase.VRCAVProVideoScreenRule.Title")),
                
                // AudioSourceの制約
                // 初期状態でコンポーネントをDisabledにすること
                // 運営配布のprefabsで使用されているもの以外ではSpatial Blendを1.0に設定すること
                new AudioSourceRule(AssetUtility.GetValidator("VketUdonRuleSetBase.AudioSourceRule.Title")),
                
                // ParticleSystemForceFiledのShapeはBoxとSphereのみ許容
                new ParticleSystemForceFieldRule(/* "ParticleSystemForceFieldのルールに違反しています。" */ AssetUtility.GetValidator("VketUdonRuleSetBase.ParticleSystemForceFieldRule.Title")),
                
                new ParticleSystemShapeModuleRule(/* "ParticleSystemShapeModuleのルールに違反しています。" */ AssetUtility.GetValidator("VketUdonRuleSetBase.ParticleSystemShapeModuleRule.Title")),
                
                // AudioSourceのサイズ
                new AudioSourceMaxDistanceRule(AssetUtility.GetValidator("AudioSourceRule.MaxDistance.Title"), AudioSourceMaxDistance),
                
                // ParticleSystemのサイズ・位置制限
                new ParticleSystemMaxDistanceRule(AssetUtility.GetValidator("ParticleSystemRule.MaxDistance.Title"), BoothSizeLimit, BoothSizeLimit * 0.2f),
                
                // ParticleSystemForceFiledのサイズ・位置制限
                new ParticleSystemForceFieldMaxDistanceRule(/* "ParticleSystemForceFieldがはみ出ています。" */ AssetUtility.GetValidator("VketUdonRuleSetBase.ParticleSystemForceFieldRule.MaxDistance.Title"), BoothSizeLimit),
                
                // 特定のコールバックの使用禁止
                // コールバックを取り除くか _Vket* で置き換えてください
                new UdonAssemblyCallbackRule(AssetUtility.GetValidator("VketUdonRuleSetBase.UdonAssemblyCallbackRule.Title"),
                    _officialAssetData.DisabledCallback,
                    _officialAssetData.GUIDs),

                // 禁止された #if や #elif が使用されていないか検出
                new DisabledDirectiveRule(AssetUtility.GetValidator("VketUdonRuleSetBase.DisabledDirectiveRule.Title"),
                    _officialAssetData.DisabledDirectives,
                    _officialAssetData.GUIDs),

                //PostProcessingのルール
                // IsGlobalはオフにすること
                // AmbientOcclusionはオフにすること
                // ScreenSpaceReflectionsはオフにすること
                // DepthOfFieldはオフにすること
                // MotionBlurはオフにすること
                // LensDistortionはオフにすること
                new PostProcessingVolumeRule(AssetUtility.GetValidator("VketUdonRuleSetBase.PostProcessingVolumeRule.Title")),
                
                // UdonBehaviourによってオブジェクトをスペース外に移動させる行為は禁止
                // ⇒ スタッフによる目視確認

                // プレイヤーの設定(移動速度等)の変更はプレイヤーがスペース内にいる場合のみ許可されます
                // ⇒ スタッフによる目視確認

                // プレイヤーの位置変更(テレポート)は、プレイヤーがスペース内にいる状態 スペース内のどこかに移動させる
                // ⇒ スタッフによる目視確認
                
                // 親オブジェクト({0})が持つAnimationによってColliderが動く可能性があります。
                new VketAnimationMakesMoveCollidersRule(AssetUtility.GetValidator("VketRuleSetBase.AnimationMakesMoveCollidersRule.Title"), IssueLevel.Info, _officialAssetData.GUIDs),
            };
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
        /// UdonBehaviourの最大数
        /// </summary>
        protected abstract int UdonBehaviourCountLimit { get; }
        
        /// <summary>
        /// SynchronizePositionが有効なUdonBehaviourの最大数
        /// 多くの場合UdonBehaviourCountLimitと同じ想定
        /// </summary>
        protected abstract int UdonBehaviourSynchronizePositionCountLimit { get; }
        
        /// <summary>
        /// UdonSyncedを付与した変数の使用数制限
        /// </summary>
        protected abstract int UdonScriptSyncedVariablesLimit { get; }

        /// <summary>
        /// VRCObjectSyncの最大数
        /// </summary>
        protected abstract int VRCObjectSyncCountLimit { get; }

        /// <summary>
        /// VRCObjectPoolの最大数
        /// </summary>
        protected abstract int VRCObjectPoolCountLimit { get; }

        /// <summary>
        /// VRCObjectPoolの最大数
        /// </summary>
        protected abstract int VRCObjectPoolPoolLimit { get; }

        /// <summary>
        /// VRCPickupの最大数
        /// </summary>
        protected abstract int VRCPickupCountLimit { get; }

        /// <summary>
        /// Materialの最大数
        /// 0の場合はルール検証をスキップ
        /// </summary>
        protected abstract int MaterialUsesLimit { get; }

        /// <summary>
        /// Lightmapの最大数
        /// </summary>
        protected abstract int LightmapCountLimit { get; }
        
        /// <summary>
        /// Lightmapの長辺制限(ex.1024)
        /// </summary>
        protected abstract int LightmapSizeLimit { get; }

        /// <summary>
        /// VRCStationの最大数
        /// </summary>
        protected abstract int VRCStationCountLimit { get; }

        /// <summary>
        /// Clothの最大数
        /// </summary>
        protected abstract int ClothCountLimit { get; }

        /// <summary>
        /// Animatorの最大数
        /// </summary>
        protected abstract int AnimatorCountLimit { get; }

        /// <summary>
        /// AudioSourceの最大数
        /// </summary>
        protected abstract int AudioSourceCountLimit { get; }

        /// <summary>
        /// AudioSourceのMaxDistanceパラメータの最大値
        /// VRCSpatialAudioSourceが有ればVRCSpatialAudioSourceのFarにも適用される
        /// AudioReverbZoneが有ればAudioReverbZoneのMaxDistanceにも適用される
        /// </summary>
        protected abstract float AudioSourceMaxDistance { get; }

        /// <summary>
        /// カメラの最大数
        /// </summary>
        protected abstract int CameraCountLimit { get; }

        /// <summary>
        /// RenderTextureの最大数
        /// </summary>
        protected abstract int RenderTextureCountLimit { get; }

        /// <summary>
        /// RenderTextureの長辺制限(ex.1024)
        /// </summary>
        protected abstract Vector2 RenderTextureSizeLimit { get; }

        /// <summary>
        /// 使用可能、不可能なコンポーネントの設定
        /// </summary>
        /// <returns></returns>
        private ComponentReference[] GetComponentReferences()
        {
            return new ComponentReference[] 
            {
                // Physics
                new ComponentReference("Collider", new string[]{"UnityEngine.SphereCollider", "UnityEngine.BoxCollider", "UnityEngine.SphereCollider", "UnityEngine.CapsuleCollider", "UnityEngine.MeshCollider", "UnityEngine.WheelCollider"}, ValidationLevel.ALLOW),
                new ComponentReference("Rigidbody", new string[]{"UnityEngine.Rigidbody"}, ValidationLevel.ALLOW),
                new ComponentReference("Cloth", new string[]{"UnityEngine.Cloth"}, ValidationLevel.ALLOW),
                new ComponentReference("Character Controller", new string[]{"UnityEngine.CharacterController"}, ValidationLevel.ALLOW),
                new ComponentReference("Dynamic Bone", new string[]{"DynamicBone"}, ValidationLevel.ALLOW),
                new ComponentReference("Dynamic Bone Collider", new string[]{"DynamicBoneCollider"}, ValidationLevel.ALLOW),
                
                // Rendering/Effect
                new ComponentReference("Mesh Filter", new string[]{"UnityEngine.MeshFilter"}, ValidationLevel.ALLOW),
                new ComponentReference("Mesh Renderer ", new string[]{"UnityEngine.MeshRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Skinned Mesh Renderer", new string[]{"UnityEngine.SkinnedMeshRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Sprite Renderer", new string[]{"UnityEngine.SpriteRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Sprite Mask", new string[]{"UnityEngine.SpriteMask"}, ValidationLevel.ALLOW),
                new ComponentReference("Sorting Group", new string[]{"UnityEngine.Rendering.SortingGroup"}, ValidationLevel.ALLOW),
                new ComponentReference("Particle System", new string[]{"UnityEngine.ParticleSystem", "UnityEngine.ParticleSystemRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Trail Renderer", new string[]{"UnityEngine.TrailRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Line Renderer", new string[]{"UnityEngine.LineRenderer"}, ValidationLevel.ALLOW),
                new ComponentReference("Particle System Force Field", new string[]{"UnityEngine.ParticleSystemForceField"}, ValidationLevel.ALLOW),
                new ComponentReference("Light", new string[]{"UnityEngine.Light"}, ValidationLevel.ALLOW),
                new ComponentReference("LightProbeGroup", new string[]{"UnityEngine.LightProbeGroup"}, ValidationLevel.ALLOW),
                new ComponentReference("Light Probe Proxy Volume", new string[]{"UnityEngine.LightProbeProxyVolume"}, ValidationLevel.ALLOW),
                new ComponentReference("ReflectionProbe", new string[]{"UnityEngine.ReflectionProbe"}, ValidationLevel.ALLOW),
                new ComponentReference("Camera", new string[]{"UnityEngine.Camera"}, ValidationLevel.ALLOW),
                new ComponentReference("FlareLayer", new string[]{"UnityEngine.FlareLayer"}, ValidationLevel.ALLOW),
                new ComponentReference("Skybox", new string[]{"UnityEngine.Skybox"}, ValidationLevel.ALLOW),
                new ComponentReference("Projector", new string[]{"UnityEngine.Projector"}, ValidationLevel.ALLOW),
                new ComponentReference("PostProcess", new string[]{"UnityEngine.Rendering.PostProcessing.PostProcessVolume" }, ValidationLevel.ALLOW),
                
                // Constraint
                new ComponentReference("Constraint", new[]
                {
                    "UnityEngine.Animations.AimConstraint",
                    "UnityEngine.Animations.LookAtConstraint",
                    "UnityEngine.Animations.ParentConstraint",
                    "UnityEngine.Animations.PositionConstraint",
                    "UnityEngine.Animations.RotationConstraint",
                    "UnityEngine.Animations.ScaleConstraint"
                }, ValidationLevel.ALLOW),
                
                // UI
                new ComponentReference("Canvas", new string[]
                {
                    "UnityEngine.Canvas",
                    "UnityEngine.RectTransform",
                    "UnityEngine.UI.CanvasScaler",
                    "UnityEngine.UI.GraphicRaycaster",
                    "UnityEngine.CanvasGroup",
                    "UnityEngine.UI.GridLayoutGroup",
                    "UnityEngine.UI.HorizontalLayoutGroup",
                    "UnityEngine.UI.VerticalLayoutGroup",
                    "UnityEngine.UI.LayoutElement",
                    "UnityEngine.UI.ContentSizeFitter",
                    "UnityEngine.CanvasRenderer",
                    "TMPro.TextMeshPro",
                    "TMPro.TextMeshProUGUI",
                    "UnityEngine.UI.Text",
                    "UnityEngine.UI.Outline",
                    "UnityEngine.UI.Shadow",
                    "UnityEngine.UI.Image",
                    "UnityEngine.UI.RawImage",
                    "UnityEngine.UI.Mask",
                    "UnityEngine.UI.RectMask2D",
                    "UnityEngine.UI.AspectRatioFitter",
                    "UnityEngine.UI.PositionAsUV1",
                    "UnityEngine.UI.Button",
                    "UnityEngine.UI.Toggle",
                    "UnityEngine.UI.ToggleGroup",
                    "UnityEngine.UI.Slider",
                    "UnityEngine.UI.Scrollbar",
                    "UnityEngine.UI.ScrollRect",
                    "UnityEngine.UI.Dropdown",
                    "TMPro.TMP_Dropdown",
                    "UnityEngine.UI.InputField",
                    "TMPro.TMP_InputField",
                    "UnityEngine.EventSystems.EventTrigger",
                    "UnityEngine.UI.Selectable"
                }, ValidationLevel.ALLOW),
                
                // Other
                new ComponentReference("Animator", new string[]{"UnityEngine.Animator"}, ValidationLevel.ALLOW),
                new ComponentReference("Animation", new string[]{"UnityEngine.Animation"}, ValidationLevel.ALLOW),
                new ComponentReference("Audio Source", new string[]
                {
                    "UnityEngine.AudioSource",
                    "UnityEngine.AudioReverbZone"
                }, ValidationLevel.ALLOW),
                new ComponentReference("VideoPlayer", new string[]{"UnityEngine.Video.VideoPlayer" }, ValidationLevel.ALLOW),
                new ComponentReference("EventSystem", new string[]{"UnityEngine.EventSystems.EventSystem", "UnityEngine.EventSystems.StandaloneInputModule"}, ValidationLevel.DISALLOW),
                new ComponentReference("PlayableDirector", new string[]{"UnityEngine.Playables.PlayableDirector" }, ValidationLevel.DISALLOW),
                new ComponentReference("Text Mesh", new string[]{"UnityEngine.TextMesh"}, ValidationLevel.DISALLOW),
                new ComponentReference("Halo", new string[]{"UnityEngine.Halo"}, ValidationLevel.DISALLOW),
                // 運営の描画負荷軽量化ギミックと干渉するため LOD Group コンポーネントは禁止する
                new ComponentReference("LOD Group", new string[]{"UnityEngine.LODGroup"}, ValidationLevel.DISALLOW),
                
                // VRCSDK2
                new ComponentReference("VRC_Trigger", new string[]{"VRCSDK2.VRC_Trigger", "VRCSDK2.VRC_EventHandler"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Object Sync", new string[]{"VRCSDK2.VRC_ObjectSync"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Pickup", new string[]{"VRCSDK2.VRC_Pickup"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Audio Bank", new string[]{"VRCSDK2.VRC_AudioBank"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Avatar Pedestal", new string[]{"VRCSDK2.VRC_AvatarPedestal"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Ui Shape", new string[]{"VRCSDK2.VRC_UiShape"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Station", new string[]{"VRCSDK2.VRC_Station"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Mirror", new string[]{ "VRCSDK2.VRC_MirrorCamera", "VRCSDK2.VRC_MirrorReflection" }, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_PlayerAudioOverride", new string[]{"VRCSDK2.VRC_PlayerAudioOverride"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Panorama", new string[]{"VRCSDK2.scripts.Scenes.VRC_Panorama", "VRC.SDKBase.VRC_Panorama" }, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_SyncVideoPlayer", new string[]{"VRCSDK2.VRC_SyncVideoPlayer", "VRCSDK2.VRC_SyncVideoStream", "VRC.SDKBase.VRC_SyncVideoPlayer", "VRC.SDKBase.VRC_SyncVideoStream" }, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_SceneResetPosition", new string[]{"VRCSDK2.VRC_SceneResetPosition"}, ValidationLevel.DISALLOW),

                // VRCSDK3
                //// VRC_Trigger is obsolete. Use instead Udon Behaviour
                new ComponentReference("VRC Trigger", new string[]{"VRC.SDKBase.VRC_Trigger", "VRC.SDK3.Components.VRCTrigger"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Pickup", new string[]{"VRC.SDKBase.VRC_Pickup", "VRC.SDK3.Components.VRCPickup"}, ValidationLevel.ALLOW),
                new ComponentReference("VRC Station", new string[]{"VRC.SDKBase.VRCStation", "VRC.SDK3.Components.VRCStation"}, ValidationLevel.ALLOW),
                new ComponentReference("VRC Avatar Pedestal", new string[]{"VRC.SDKBase.VRC_AvatarPedestal", "VRC.SDK3.Components.VRCAvatarPedestal"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Mirror Reflection", new string[]{"VRC.SDKBase.VRC_MirrorReflection", "VRC.SDK3.Components.VRCMirrorReflection"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Ui Shape", new string[]{"VRC.SDKBase.VRC_UiShape", "VRC.SDK3.Components.VRCUiShape"}, ValidationLevel.ALLOW),
                new ComponentReference("VRC Url Input Field", new string[]{"VRC.SDK3.Components.VRCUrlInputField"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Visual Damage", new string[]{"VRC.SDKBase.VRC_VisualDamage", "VRC.SDK3.Components.VRCVisualDamage"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Spacial Audio Source", new string[]{"VRC.SDKBase.VRC_SpatialAudioSource", "VRC.SDK3.Components.VRCSpatialAudioSource"}, ValidationLevel.ALLOW),
                new ComponentReference("VRC Unity Video Player", new string[]{"VRC.SDK3.Video.Components.VRCUnityVideoPlayer", }, ValidationLevel.DISALLOW),
                new ComponentReference("VRC AV Pro Video", new[]
                {
                    "VRC.SDK3.Video.Components.AVPro.VRCAVProVideoPlayer"
                }, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Midi", new string[]{"VRC.SDK3.Midi.VRCMidiHandler", "VRC.SDK3.Midi.VRCMidiListener"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Portal Marker", new string[]{"VRC.SDKBase.VRC_PortalMarker", "VRC.SDK3.Components.VRCPortalMarker"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Scene Descriptor", new string[]{"VRC.SDKBase.VRC_SceneDescriptor", "VRC.SDK3.Components.VRCSceneDescriptor"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Test Marker", new string[]{"VRC.SDK3.VRCTestMarker"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Project Settings", new string[]{"VRCProjectSettings"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Sdk Builder", new string[]{"VRC.SDK3.Editor.VRC_SdkBuilder"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC_Event Handler(Obsolete)", new string[]{"VRC.SDKBase.VRC_EventHandler", "VRC.SDK3.Components.VRCEventHandler"}, ValidationLevel.DISALLOW),
                new ComponentReference("Udon Behaviour", new string[]{"VRC.Udon.UdonBehaviour", "VRC.SDKBase.VRC_Interactable"}, ValidationLevel.ALLOW),
                new ComponentReference("VRC Object Pool", new string[]{"VRC.SDK3.Components.VRCObjectPool"}, ValidationLevel.DISALLOW),
                new ComponentReference("VRC Object Sync", new string[]{"VRC.SDK3.Components.VRCObjectSync"}, ValidationLevel.ALLOW),
                
                new ComponentReference("VRCPhysBone", new[]
                {
                    "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone",
                    "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider",
                }, ValidationLevel.DISALLOW),
                
                // VRCConstraint
                new ComponentReference("VRCConstraint", new[]
                {
                    "VRC.SDK3.Dynamics.Constraint.Components.VRCAimConstraint",
                    "VRC.SDK3.Dynamics.Constraint.Components.VRCLookAtConstraint",
                    "VRC.SDK3.Dynamics.Constraint.Components.VRCParentConstraint",
                    "VRC.SDK3.Dynamics.Constraint.Components.VRCPositionConstraint",
                    "VRC.SDK3.Dynamics.Constraint.Components.VRCRotationConstraint",
                    "VRC.SDK3.Dynamics.Constraint.Components.VRCScaleConstraint"
                }, ValidationLevel.DISALLOW),
                
                // CyanTrigger
                new ComponentReference("CyanTrigger", new[] { "Cyan.CT.CyanTrigger" }, ValidationLevel.DISALLOW),
                new ComponentReference("CyanTriggerAsset", new[] { "Cyan.CT.CyanTriggerAsset" }, ValidationLevel.ALLOW),
                
                // VketAsset
                new ComponentReference("Vket Udon Controll", new string[] { "Vket.UdonManager.VketUdonControl" }, ValidationLevel.ALLOW),
                new ComponentReference("Item Teleport Marker", new string[] { "Vket.UdonManager.ItemTeleportMarker" }, ValidationLevel.ALLOW),
                new ComponentReference("Vket Weapon", new[] { "VketMarche.VketWeapon" }, VketWeaponValidationLevel),
                new ComponentReference("Vket Kart", new[] { "VketFormula.VketKart" }, VketKartValidationLevel),
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

        /// <summary>
        /// ブースルールに応じてコンポーネントの許可不許可を変えたい場合にオーバーライドする
        /// </summary>
        protected virtual ValidationLevel AdvancedObjectValidationLevel => ValidationLevel.ALLOW;
        
        /// <summary>
        /// 使用禁止UdonAssembly関数群
        /// VketUsableUdonAssemblyListRule.csのcodeを参照
        /// </summary>
        /// <returns>使用禁止関数</returns>
        public static UdonAssemblyReference[] GetUdonAssemblyReferences()
        {
            return new UdonAssemblyReference[]
            {
                // Variables
                new("Transform.root", new[] { "__get_root__UnityEngineTransform", "__set_root__UnityEngineTransform" }, ValidationLevel.DISALLOW),
                //new UdonAssemblyReference("GameObject.Layer", new string[]{"UnityEngineGameObject.__get_layer__SystemInt32", "UnityEngineGameObject.__set_layer__SystemInt32"}, ValidationLevel.DISALLOW),
                new("RenderSettings", new[] { "UnityEngineRenderSettings" }, ValidationLevel.DISALLOW),

                // Functions
                new("UdonSharpBehaviour.VRCInstantiate, Object.Instantiate", new[] { "VRCInstantiate" }, ValidationLevel.DISALLOW),
                new("UnityEngine.Physics.bounceThreshold(Set)", new[] { "UnityEnginePhysics.__set_bounceThreshold__SystemSingle__SystemVoid" }, ValidationLevel.DISALLOW),
                //new UdonAssemblyReference("UnityEngine.Physics.bounceThreshold(Get)", new string[]{"UnityEnginePhysics.__get_bounceThreshold__SystemSingle"}, ValidationLevel.DISALLOW),
                new("GameObject.Find", new[] { "__Find__SystemString__UnityEngineGameObject" }, ValidationLevel.DISALLOW),
                new("Object.Destroy", new[] { "UnityEngineObject.__Destroy__UnityEngineObject__SystemVoid" }, ValidationLevel.DISALLOW),
                new("Object.DestroyImmediate", new[] { "UnityEngineObject.__DestroyImmediate__UnityEngineObject__SystemVoid" }, ValidationLevel.DISALLOW),
                //new UdonAssemblyReference("Transform.DetachChildren", new string[]{"UnityEngineTransform.__DetachChildren__SystemVoid"}, ValidationLevel.DISALLOW),
                new("VRCSDK3VideoPlayer", new[] { "VRCSDK3VideoComponentsBaseBaseVRCVideoPlayer" }, ValidationLevel.DISALLOW),
                new("VRCGraphics.DrawMeshInstanced", new[] { "VRCSDKBaseVRCGraphics.__DrawMeshInstanced__" }, ValidationLevel.DISALLOW),
                new("VRCImageDownloader", new[] { "VRCSDK3ImageVRCImageDownloader" }, ValidationLevel.DISALLOW),
                new("VRCStringDownloader", new[] { "VRCSDK3StringLoadingVRCStringDownloader" }, ValidationLevel.DISALLOW),
                
                // Persistence
                new("PlayerData", new[] { "VRCSDK3PersistencePlayerData" }, ValidationLevel.DISALLOW),
                new("VRCPlayerApi.GetPlayerObjects", new[] { "VRCSDKBaseVRCPlayerApi.__GetPlayerObjects__UnityEngineGameObjectArray" }, ValidationLevel.DISALLOW),
                new("VRCPlayerApi.FindComponentInPlayerObjects", new[] { "VRCSDKBaseVRCPlayerApi.__FindComponentInPlayerObjects__UnityEngineComponent__UnityEngineComponent" }, ValidationLevel.DISALLOW),
                new("Networking.GetPlayerObjects", new[] { "VRCSDKBaseNetworking.__GetPlayerObjects__VRCSDKBaseVRCPlayerApi__UnityEngineGameObjectArray" }, ValidationLevel.DISALLOW),
                new("Networking.FindComponentInPlayerObjects", new[] { "VRCSDKBaseNetworking.__FindComponentInPlayerObjects__VRCSDKBaseVRCPlayerApi_UnityEngineComponent__UnityEngineComponent" }, ValidationLevel.DISALLOW),
                
                // VRCShader.SetGlobal* (まとめて指定)
                new("VRCShader.SetGlobal*", new[] { "VRCSDKBaseVRCShader.__SetGlobal" }, ValidationLevel.DISALLOW),
                
                // AvatarScalingのワールド側設定に関わる禁止関数
                new("VRCPlayerApi.SetManualAvatarScalingAllowed", new[] { "VRCSDKBaseVRCPlayerApi.__SetManualAvatarScalingAllowed" }, ValidationLevel.DISALLOW),
                new("VRCPlayerApi.SetAvatarEyeHeightMinimumByMeters", new[] { "VRCSDKBaseVRCPlayerApi.__SetAvatarEyeHeightMinimumByMeters" }, ValidationLevel.DISALLOW),
                new("VRCPlayerApi.SetAvatarEyeHeightMaximumByMeters", new[] { "VRCSDKBaseVRCPlayerApi.__SetAvatarEyeHeightMaximumByMeters" }, ValidationLevel.DISALLOW),
                
                // VRCCameraSettings
                new("VRC.SDK3.Rendering.VRCCameraSettings.ScreenCamera", new[]
                {
                    "VRCSDK3RenderingVRCCameraSettings.__get_ScreenCamera__VRCSDK3RenderingVRCCameraSettings"
                }, ValidationLevel.DISALLOW),
                new("VRC.SDK3.Rendering.VRCCameraSettings.PhotoCamera", new[]
                {
                    "VRCSDK3RenderingVRCCameraSettings.__get_PhotoCamera__VRCSDK3RenderingVRCCameraSettings"
                }, ValidationLevel.DISALLOW),
            };
        }

        private UdonAssemblyFunctionEssentialArgumentReference[] GetUdonAssemblyPhysicsCastFunctionReferences()
        {
            return new UdonAssemblyFunctionEssentialArgumentReference[]
            {
                new UdonAssemblyFunctionEssentialArgumentReference(
                    "Physics.RayCast",
                    "MaxDistance, LayerMask",
                    new []{"UnityEnginePhysics.__Raycast__", "UnityEnginePhysics.__RaycastAll__", "UnityEnginePhysics.__RaycastNonAlloc__"},
                    "_SystemSingle_SystemInt32_"),
                new UdonAssemblyFunctionEssentialArgumentReference(
                    "Physics.BoxCast",
                    "MaxDistance, LayerMask",
                    new []{"UnityEnginePhysics.__Boxcast__", "UnityEnginePhysics.__BoxCastAll__", "UnityEnginePhysics.__BoxCastNonAlloc__"},
                    "_SystemSingle_SystemInt32_"),
                new UdonAssemblyFunctionEssentialArgumentReference(
                    "Physics.SphereCast",
                    "MaxDistance, LayerMask",
                    new []{"UnityEnginePhysics.__SphereCast__", "UnityEnginePhysics.__SphereCastAll__", "UnityEnginePhysics.__SphereCastNonAlloc__"},
                    "_SystemSingle_SystemInt32_"),
                new UdonAssemblyFunctionEssentialArgumentReference(
                    "Physics.CapsuleCast",
                    "MaxDistance, LayerMask",
                    new []{"UnityEnginePhysics.__CapsuleCast__", "UnityEnginePhysics.__CapsuleCastAll__", "UnityEnginePhysics.__CapsuleCastNonAlloc__"},
                    "_SystemSingle_SystemInt32_"),
                new UdonAssemblyFunctionEssentialArgumentReference(
                    "Physics.LineCast",
                    "LayerMask",
                    new []{"UnityEnginePhysics.__Linecast__"},
                    "_SystemInt32_"),
            };
        }
        
        /// <summary>
        /// 許可されているPointLightの設定
        /// </summary>
        protected abstract VketLightConfigRule.LightConfig ApprovedPointLightConfig { get; }

        /// <summary>
        /// 許可されているSpotLightの設定
        /// </summary>
        protected abstract VketLightConfigRule.LightConfig ApprovedSpotLightConfig { get; }

        /// <summary>
        /// 許可されているAreaLightの設定
        /// </summary>
        protected abstract VketLightConfigRule.LightConfig ApprovedAreaLightConfig { get; }

        /// <summary>
        /// AreaLightの最大数
        /// </summary>
        protected abstract int AreaLightUsesLimit { get; }

        /// <summary>
        /// VketPickup, VketFollowPickupの合計最大数
        /// </summary>
        protected abstract int PickupObjectSyncUsesLimit { get; }

        /// <summary>
        /// 全てのUdonBehaviourオブジェクトの親であるDynamicオブジェクトは初期でInactive状態にしなければならない場合はtrue
        /// </summary>
        protected abstract bool UdonInactiveRuleIsEnabled { get; }

        /// <summary>
        /// Static,Dynamicの2つのEmptyオブジェクトを作り、すべてのオブジェクトはこのどちらかの階層下に入れているかチェックする場合はtrue
        /// </summary>
        protected abstract bool ExhibitStructureRuleIsEnabled { get; }

        /// <summary>
        /// ExhibitStructureRuleIsEnabledが有効な場合、Staticオブジェクトが無い場合はtrue
        /// trueにするとブース構造ルールはIDのオブジェクトのみが正しい状態になる
        /// </summary>
        protected abstract bool ExhibitStructureRuleOnlyDynamic { get; }

        /// <summary>
        /// 以下のルールを有効にするか
        /// UdonBehaviourを含むオブジェクト、UdonBehaviourによって操作を行うオブジェクトは全て入稿ルール C.Scene内階層規定におけるDynamicオブジェクトの階層下に入れてください
        /// </summary>
        protected abstract bool UdonDynamicObjectParentRuleIsEnabled { get; }

        /// <summary>
        /// VketImageDownloaderの最大数
        /// </summary>
        protected abstract int VketImageDownloaderUsesLimit { get; }

        /// <summary>
        /// VketStringDownloaderの最大数
        /// </summary>
        protected abstract int VketStringDownloaderUsesLimit { get; }
        
        /// <summary>
        /// VketStarshipTreasureの最大数
        /// </summary>
        protected abstract int VketStarshipTreasureUsesLimit { get; }

        /// <summary>
        /// VketVideoPlayerの最大数
        /// </summary>
        protected abstract int VketVideoPlayerUsesLimit { get; }
        
        /// <summary>
        /// VketMirrorの最大数
        /// </summary>
        protected abstract int VketMirrorUsesLimit { get; }

        /// <summary>
        /// RigidBodyのIs Kinematicを必ず有効にしなければならない場合はfalseを指定
        /// </summary>
        protected abstract bool AllowIsKinematic { get; }
    }
}
#endif
