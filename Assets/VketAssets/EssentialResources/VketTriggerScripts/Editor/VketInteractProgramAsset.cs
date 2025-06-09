#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.Udon.Editor.ProgramSources.Attributes;

[assembly: UdonProgramSourceNewMenu(typeof(VketUdonAssembly.VketInteractProgramAsset), "Vket Interact Program Asset")]
namespace VketUdonAssembly
{
    [CreateAssetMenu(menuName = "Vket/Vket Trigger/Vket Interact Program Asset", fileName = "New Vket Interact Program Asset")]
    public class VketInteractProgramAsset : VketTriggerProgramAsset
    {
        new private void OnEnable()
        {
            base.OnEnable();

            if (triggers == null)
            {
                if (serializedObject == null)
                    serializedObject = new SerializedObject(this);

                serializedObject.Update();

                // プロパティ取得
                SerializedProperty triggersProperty = serializedObject.FindProperty(nameof(this.triggers));

                // 初期トリガー追加
                triggersProperty.arraySize = 1;
                SerializedProperty newTriggerProperty = triggersProperty.GetArrayElementAtIndex(0);
                newTriggerProperty.FindPropertyRelative("type").enumValueIndex = (int)TriggerParameters.TriggerType.OnInteract;
                newTriggerProperty.FindPropertyRelative("broadcast").enumValueIndex = (int)TriggerParameters.BroadcastType.Local;
                newTriggerProperty.FindPropertyRelative("actions").arraySize = 0;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
