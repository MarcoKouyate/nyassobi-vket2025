using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace VketTools.Utilities
{
    public enum DraftSequence
    {
        None,
        BakeCheck,
        VRChatCheck,
        RuleCheck,
        BuildSizeCheck,
        SetPassCheck,
        ScreenShotCheck,
        UploadCheck,
        PostUploadCheck,
    }
    
    public class SequenceInfo : ScriptableObject
    {
        [FormerlySerializedAs("sequenceStatus")] public Sequence[] Sequences;

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void Reset()
        {
            Sequences = new Sequence[Enum.GetValues(typeof(DraftSequence)).Length];
            for (int i = 0; i < Sequences.Length; ++i)
            {
                Sequences[i] = new Sequence();
            }
        }
    }

    [Serializable]
    public class Sequence
    {
        public enum RunStatus
        {
            None = 0,
            Running = 1,
            Complete = 2,
        }

        [FormerlySerializedAs("status")] public RunStatus Status = RunStatus.None;
        [FormerlySerializedAs("message")] [FormerlySerializedAs("desc")] public string Message = "";
    }
}