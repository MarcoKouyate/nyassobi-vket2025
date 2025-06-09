using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using VitDeck.Utilities;
using VketTools.Networking;

namespace VketTools.Utilities
{
    public class LoginInfo : ScriptableObject
    {
        public enum ExhibitType
        {
            None,
            Space,
            Item,
        }
        
        public static VketWorldDefinition CurrentWorldDefinition;
        
        public Oauth.AccessTokenProvider.Result Authentication;
        public bool OauthEndFlg;
        public VketApi.UserData User;
        public VketApi.CircleData[] Circles;
        // 表示に必要なワールド情報のみを保持
        public VketApi.WorldData[] Worlds;
        public VketApi.Term SubmissionTerm;
        public int SelectedCircleIndex = 9999;
        public VketApi.WorldData SelectedWorld;
        public int SelectedOuterFrameTemplate;
        public ExhibitType SelectedType = ExhibitType.None;
        public int SpecialExhibitedId;
        private bool _refreshRunning;
        
        public bool IsSelectedWorld => SelectedWorld != null && SelectedWorld.world_id != 0;

        public bool IsLogin => !(User == null || !OauthEndFlg);
        
        public bool IsAvailable => !(!IsLogin
                                     || SubmissionTerm == null
                                     || Circles == null
                                     || Worlds == null);
        
        public bool IsQuest => SelectedWorld is { exhibition_platform: "cross_platform" };

        public bool IsItem => SelectedType == ExhibitType.Item;

        public bool IsOwner
        {
            get
            {
                if (SelectedCircleIndex != 9999)
                    return Circles[SelectedCircleIndex].user_id == User.user_id;
                return true;
            }
        }
        
        public static void ReloadWorldDefinition()
        {
            string definitionPath;
            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.stable:
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                case VersionInfo.PackageType.develop:
                    int worldId = AssetUtility.LoginInfoData.SelectedWorld?.world_id ?? 9999;
                    definitionPath = $"{AssetUtility.ConfigFolderPath}/WorldDefinitions/{AssetUtility.VersionInfoData.EventName}/{worldId}.asset";
                    break;
                default:
                    definitionPath = $"{AssetUtility.ConfigFolderPath}/WorldDefinitions/0.asset";
                    break;
            }
            CurrentWorldDefinition = AssetUtility.AssetLoad<VketWorldDefinition>(definitionPath);
            CurrentWorldDefinition.Init(AssetUtility.LoginInfoData.IsItem);
        }
        
        public VketApi.CircleData SelectedCircle
        {
            get
            {
                if (Circles == null || Circles.Length <= SelectedCircleIndex)
                    return null;
                
                return Circles[SelectedCircleIndex];
            }
        }

        public async UniTask<bool> IsSubmit()
        {
            await UpdateTerm();
            return SubmissionTerm.is_submit;
        }
        
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public async UniTask UpdateAll()
        {
            if (!IsLogin)
            {
                Debug.LogError("ログインしていません。情報の更新に失敗しました。");
                return;
            }
            await UpdateTerm();
            await UpdateCircles();
            await UpdateWorlds();
            
            ReloadWorldDefinition();
            Save();

            var userSettings = SettingUtility.GetSettings<UserSettings>();
            if (SelectedCircleIndex != 9999)
            {
                userSettings.validatorFolderPath = "Assets/" + SelectedCircle.circle_id;
                userSettings.validatorRuleSetType = CurrentWorldDefinition.ValidatorRuleSet;
                userSettings.exporterSettingFileName = CurrentWorldDefinition.ExportSetting;
            }
            else
            {
                userSettings.validatorRuleSetType = "ConceptWorldRuleSet";
            }
            SettingUtility.SaveSettings(userSettings);
        }

        private async UniTask UpdateTerm()
        {
            SubmissionTerm = await VketApi.GetTerm(AssetUtility.VersionInfoData.EventID);
        }

        private async UniTask UpdateCircles()
        {
            var circles = Circles = await VketApi.GetCircles(AssetUtility.VersionInfoData.EventID, User.GetAllEntryCircles());
            // サークルサイズが途中で変わった場合は選択に戻る
            if (SelectedCircleIndex != 9999 && circles.Length != Circles.Length)
            {
                SelectedCircleIndex = 9999;
                SelectedWorld = null;
            }
            Circles = circles;
            
            // 全てのサークルのサムネイル読み込み
            await Circles.Select(c => c.LoadThumbnailTexture());
        }
        
        private async UniTask UpdateWorlds(bool isDevelop = false)
        {
            if (isDevelop)
            {
                // 全てのワールドを取得
                Worlds = await VketApi.GetWorlds(AssetUtility.VersionInfoData.EventID);
            }
            else
            {
                var entryWorldIds = Circles.SelectMany(circle => circle.entry_worlds.space_worlds.Union(circle.entry_worlds.item_worlds).Union(circle.entry_worlds.avatar_worlds)).ToArray();
                Worlds = await VketApi.GetWorlds(AssetUtility.VersionInfoData.EventID, entryWorldIds);
            }
        }

        public VketApi.WorldData[] GetEntrySpaceWorlds(VketApi.CircleData circle)
        {
            if (circle == null || Worlds == null) return Array.Empty<VketApi.WorldData>();
            // Vket2025S時点ではSpaceとAvatarは同じ括り
            return Worlds.Where(w => circle.entry_worlds.space_worlds.Contains(w.world_id)
                                     || circle.entry_worlds.avatar_worlds.Contains(w.world_id)).ToArray();
        }

        public VketApi.WorldData[] GetEntryItemWorlds(VketApi.CircleData circle)
        {
            if (circle == null || Worlds == null) return Array.Empty<VketApi.WorldData>();
            return Worlds.Where(w => circle.entry_worlds.item_worlds.Contains(w.world_id)).ToArray();
        }

        public VketApi.WorldData[] GetAllSpaceWorlds()
        {
            if (Worlds == null) return new VketApi.WorldData[] { };
            return Worlds.Where(w => w.exhibition_category.Contains("space")
                                     || w.exhibition_category.Contains("avatar")).ToArray();
        }

        public VketApi.WorldData[] GetAllItemWorlds()
        {
            if (Worlds == null) return new VketApi.WorldData[] { };
            return Worlds.Where(w => w.exhibition_category.Contains("item")).ToArray();
        }

        #region Login機構

        public static string AuthorizationCode = "";
        private static (string codeVerifier, string redirectUri) _authorizationTuple;
        public static bool IsWaitAuth;
        
        public void LoginRequest()
        {
            _authorizationTuple = Oauth.GetAuthorizationCode();
            IsWaitAuth = true;
        }

        public void CancelLogin()
        {
            IsWaitAuth = false;
            AuthorizationCode = "";
            _authorizationTuple = default;
        }
        
        public async UniTask Login()
        {
            Authentication = await Oauth.GetAccessToken(AuthorizationCode, _authorizationTuple.codeVerifier, _authorizationTuple.redirectUri);
            OauthEndFlg = !string.IsNullOrEmpty(Authentication.AccessToken);
            if (Authentication == null || !OauthEndFlg)
            {
                //EditorUtility.DisplayDialog("Error", /* Vketアカウントでログインしてください。 */AssetUtility.GetLabelString(35), "OK");
                return;
            }

            try
            {
                User = await VketApi.GetUserData(Authentication.AccessToken);
                if (User == null)
                {
                    Logout();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Debug.LogError( /* ログインに失敗しました。お問い合わせにて表示されたエラーコードをお伝えください。 */AssetUtility.GetMain("Vket_ControlPanel.LoginErrorMessage"));
                Logout();
            }
            finally
            {
                Save();
            }

            if (IsLogin) await UpdateAll();
            
        }

        /// <summary>
        /// ゲストログイン
        /// </summary>
        /// <param name="userId">入稿ID</param>
        /// <param name="isDevelop">パッケージタイプがDevelopならtrueを指定</param>
        public async UniTask GuestLogin(int userId, bool isDevelop = false)
        {
            User = new VketApi.UserData
            {
                user_id = userId
            };
            Authentication = null;
            OauthEndFlg = true;
            await UpdateTerm();
            Circles = new[]
            {
                new VketApi.CircleData
                {
                    thumbnail = ""
                }
            };
            if(isDevelop)
                await UpdateWorlds(true);
            else
            {
                Worlds = Array.Empty<VketApi.WorldData>();
            }
            Save();
        }

        public bool NeedRefresh()
        {
            return IsLogin && Authentication.CreatedAt + Authentication.ExpiresIn < DateTimeOffset.Now.ToUnixTimeSeconds();
        }
        
        public async UniTask RefreshLogin()
        {
            if (Authentication == null)
            {
                Logout();
                return;
            }
            
            if (!NeedRefresh())
                return;
            
            if(_refreshRunning)
                return;

            _refreshRunning = true;
            Debug.Log("Try Auth Refresh.");
            try
            {
                // AccessToken が期限切れの場合、 RefreshToken で新しい AccessToken を得る
                var refreshResult = await Oauth.GetRefreshToken(Authentication.RefreshToken);
                if (refreshResult == null)
                {
                    Debug.LogError("Refresh Auth Error.");
                    Debug.LogError( /* ログインに失敗しました。お問い合わせにて表示されたエラーコードをお伝えください。 */AssetUtility.GetMain("Vket_ControlPanel.LoginErrorMessage"));
                    Logout();
                    return;
                }

                var oldAuth = Authentication;
                Authentication = JsonUtility.FromJson<Oauth.AccessTokenProvider.Result>(
                    "{\"access_token\":\"" + refreshResult.AccessToken +
                    "\",\"expires_in\":" + refreshResult.ExpiresIn + ",\"id_token\":\"" + oldAuth.IdToken +
                    "\",\"refresh_token\":\"" + refreshResult.RefreshToken + "\",\"scope\":\"" + refreshResult.Scope +
                    "\",\"token_type\":\"" + refreshResult.TokenType + "\",\"created_at\":" + refreshResult.CreatedAt +
                    "}");
                OauthEndFlg = !string.IsNullOrEmpty(Authentication.AccessToken);
                Save();

                try
                {
                    User = await VketApi.GetUserData(Authentication.AccessToken);
                    if (User == null)
                    {
                        Logout();
                        return;
                    }

                    await UpdateAll();
                    Debug.Log("Refresh Auth Success.");
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    Debug.LogError( /* ログインに失敗しました。お問い合わせにて表示されたエラーコードをお伝えください。 */AssetUtility.GetMain("Vket_ControlPanel.LoginErrorMessage"));
                    Debug.LogError("Refresh Auth Error.");
                    Logout();
                }
            }
            finally
            {
                _refreshRunning = false;
            }
        }
        
        public void Logout()
        {
            Authentication = null;
            OauthEndFlg = false;
            User = null;
            Worlds = null;
            SelectedCircleIndex = 9999;
            SelectedWorld = null;
            SelectedType = ExhibitType.None;
            IsWaitAuth = false;
            AuthorizationCode = "";
            _authorizationTuple = default;
            CurrentWorldDefinition = null;
            SpecialExhibitedId = 0;
            Save();
            
            var userSettings = SettingUtility.GetSettings<UserSettings>();
            userSettings.validatorFolderPath = "";
            userSettings.validatorRuleSetType = "";
            userSettings.exporterSettingFileName = "";
            SettingUtility.SaveSettings(userSettings);
        }

        #endregion
    }
}