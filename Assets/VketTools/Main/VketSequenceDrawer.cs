using System;
using UnityEditor;
using UnityEngine;
using VketTools.Utilities;
using Status = VketTools.Utilities.Sequence.RunStatus;

namespace VketTools.Main
{
    public static class VketSequenceDrawer
    {
        private static readonly string[] LocalizeKeys = 
        {
            /* None */
            "",
            /* LightBakeチェック */
            "Vket_SequenceWindow.LightBakeCheck",
            /* VRChat内での確認 */
            "Vket_SequenceWindow.EvaluationVRC",
            /* ルールチェック */
            "Vket_SequenceWindow.RuleCheck",
            /* 容量チェック */
            "Vket_SequenceWindow.BuildSizeCheck",
            /* SetPassチェック */
            "Vket_SequenceWindow.SetPassCheck",
            /* スクリーンショット撮影 */
            "Vket_SequenceWindow.TakeScreenshot",
            /* アップロード */
            "Vket_SequenceWindow.Upload",
            /* アップロード後処理 */
            "Vket_SequenceWindow.PostUploadProcess",
        };

        public static string GetSequenceText(int sequenceIndex)
        {
            return AssetUtility.GetMain(LocalizeKeys[sequenceIndex]);
        }

        public static void SetState(DraftSequence sequence, Status status, string message = "")
        {
            var sequenceInfo = AssetUtility.SequenceInfoData;
            sequenceInfo.Sequences[(int)sequence].Status = status;
            sequenceInfo.Sequences[(int)sequence].Message = message;
            sequenceInfo.Save();
        }
        
        public static string GetResultLog()
        {
            var result = "";
            var info = AssetUtility.SequenceInfoData;
            for (int i = 0; i < info.Sequences.Length; ++i)
            {
                result += $"{Enum.ToObject(typeof(DraftSequence), i)}:{info.Sequences[i].Status},{info.Sequences[i].Message}\r\n";
            }
            return result;
        }

        /// <summary>
        /// シーケンス情報の初期化
        /// ウィンドウは閉じたり開かれたりするため、SequenceInfoData内に情報を保持しておく
        /// </summary>
        public static void ResetSequence()
        {
            // シーケンスの初期化
            var info = AssetUtility.SequenceInfoData;
            info.Reset();
        }

        private static GUIStyle _l1;
        private static GUIStyle _l2;

        private static void InitStyle()
        {
            if (_l1 != null)
                return;
            
            _l1 = new GUIStyle(GUI.skin.label);
            _l2 = new GUIStyle(GUI.skin.label);
            _l1.fontSize = 13;
            _l1.fixedHeight = 30;
            _l2.fontSize = 10;
            _l2.fixedHeight = 25;
        }
        
        public static void Draw(Rect position)
        {
            InitStyle();
            
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(position.width), GUILayout.Height(320f));
            {
                var info = AssetUtility.SequenceInfoData;
                foreach (DraftSequence draftSequence in Enum.GetValues(typeof(DraftSequence)))
                {
                    if(draftSequence == DraftSequence.None)
                        continue;
                    
                    DrawSequenceStatus((int)draftSequence, info.Sequences[(int)draftSequence], position.width);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private static void DrawSequenceStatus(int sequenceIndex, Sequence sequence, float width)
        {
            EditorGUILayout.BeginHorizontal();
            {
                // アイコン
                GUILayout.Space(20);
                if (sequence.Status == Status.Running)
                {
                    UIUtility.EditorGUIWaitSpin();
                }
                else if (sequence.Status == Status.Complete)
                {
                    var icon = EditorGUIUtility.IconContent("toggle on");
                    GUILayout.Box(icon, GUIStyle.none, GUILayout.Width(16), GUILayout.Height(16));
                }
                else
                {
                    GUILayout.Space(16);
                }
                
                // テキスト
                EditorGUILayout.Space();
                var content = new GUIContent(GetSequenceText(sequenceIndex));
                EditorGUILayout.LabelField(content, UIUtility.GetContentSizeFitStyle(content, _l1, width));
                // テキスト左詰め
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            
            // アップロード情報
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(36);
                EditorGUILayout.Space();
                var content = new GUIContent(sequence.Message);
                EditorGUILayout.LabelField(content, UIUtility.GetContentSizeFitStyle(content, _l2, width));
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
        }
    }
}