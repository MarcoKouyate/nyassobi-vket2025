using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Serialization;

namespace VketTools.Utilities
{
    public class VersionInfo : ScriptableObject
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum PackageType
        {
            stable,
            company,
            community,
            develop,
        }
        
        public enum DllType
        {
            Production,
            Develop,
            ApiDummy,
        }
        
        [SerializeField]
        string _eventName = "";
        [SerializeField]
        int _eventID = 1;
        [SerializeField]
        string _eventVersion = "14";
        [SerializeField]
        string _version = "";
        [SerializeField]
        PackageType _packageType;
        [SerializeField]
        DllType dllType;
        
        public string EventName => _eventName;

        public int EventID
        {
            get => _eventID;
            set => _eventID = value;
        }
        
        public string EventVersion
        {
            get => _eventVersion;
            set => _eventVersion = value;
        }

        public string Version
        {
            get => _version;
            set => _version = value;
        }

        public PackageType Type
        {
            get => _packageType;
            set => _packageType = value;
        }

        public DllType DLLType
        {
            get => dllType;
            set => dllType = value;
        }
    }
}