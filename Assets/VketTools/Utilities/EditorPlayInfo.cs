using UnityEditor;
using UnityEngine;

namespace VketTools.Utilities
{
    public class EditorPlayInfo : ScriptableObject
    {
        #region 入稿チェック時の結果データ避難先

        public bool ErrorFlag;
        public bool IsVketEditorPlay;
        public bool IsSetPassCheckOnly;
        public bool BuildSizeSuccessFlag;
        public bool SetPassSuccessFlag;
        public bool SetPassFailedFlag;
        public bool SsSuccessFlag;

        /// <summary>
        /// PlayModeに入る前にClientSimの設定を保持
        /// </summary>
        public bool ClientSimEnabledRestoreFlag;
        
        /// <summary>
        /// SetPassの一時避難先
        /// </summary>
        public float BuildSize;
        
        /// <summary>
        /// SetPassの一時避難先
        /// </summary>
        public int SetPassCalls;
        
        /// <summary>
        /// Batchesの一時避難先
        /// </summary>
        public int Batches;
        
        /// <summary>
        /// スクショのパス避難先
        /// </summary>
        public string SsPath;
        
        #endregion
        
        #region エディターの設定
        
        /// <summary>
        /// 確認以外でPlayModeに入ったときに注意ダイアログを表示するか
        /// 「次回以降表示しない」を選択した場合にtrueになる。
        /// </summary>
        public bool IsShowPlayModeNotification = true;

        #endregion
        
        public void ResetDraftFlags()
        {
            ErrorFlag = false;
            IsVketEditorPlay = false;
            IsSetPassCheckOnly = false;
            BuildSizeSuccessFlag = false;
            SetPassSuccessFlag = false;
            SetPassFailedFlag = false;
            SsSuccessFlag = false;
            ClientSimEnabledRestoreFlag = false;
            BuildSize = 0;
            SetPassCalls = 0;
            Batches = 0;
            SsPath = string.Empty;
            Save();
        }
        
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}