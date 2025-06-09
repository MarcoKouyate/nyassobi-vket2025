using UnityEditor;
using UnityEngine;

namespace VketTools.Main
{
    [InitializeOnLoad]
    public class UdonLayerSetting
    {
        static UdonLayerSetting()
        {
#if VRC_SDK_VRCSDK3
            if(LayerMask.LayerToName(23) != "") return;
            CreateLayer();
#endif
        }
        
        private static void CreateLayer()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
 
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                return;
            }

            SerializedProperty layer22 = layers.GetArrayElementAtIndex(22);
            if (layer22.stringValue == "")
            {
                Debug.Log("Setting up layers. Layer 22 is created ");
                layer22.stringValue = "PostProcessing";
            }

            SerializedProperty layer23 = layers.GetArrayElementAtIndex(23);
            if (layer23.stringValue == "")
            {
                Debug.Log("Setting up layers. Layer 23 is created ");
                layer23.stringValue = "UserLayer23";
                for (var i = 0; i < 24; i++)
                {
                    bool ignore = !(i == 9 || i == 10 || i == 23); 
                    Physics.IgnoreLayerCollision(23, i, ignore);
                }
            }
            tagManager.ApplyModifiedProperties();
        }
    }
}
