using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VketTools.Main
{
    [CreateAssetMenu(fileName = nameof(OuterFrameInfo))]
    public class OuterFrameInfo : ScriptableObject
    {
        [Serializable]
        public class OuterFrameData
        {
            public int ID;
            public string Name;
            public GameObject Prefab;
            
            public GameObject Instantiate(Transform parent)
            {
                var instance = PrefabUtility.InstantiatePrefab(Prefab, parent) as GameObject;
                if (instance) instance.name = Name;
                return instance;
            }
        }

        public int[] TargetWorldIds;
        public List<OuterFrameData> OuterFlames;
    }
}
