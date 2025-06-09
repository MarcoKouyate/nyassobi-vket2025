#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.Udon.Editor.ProgramSources.Attributes;

[assembly: UdonProgramSourceNewMenu(typeof(VketUdonAssembly.VketOnBoothProgramAsset), "Vket OnBooth Program Asset")]
namespace VketUdonAssembly
{
    [CreateAssetMenu(menuName = "Vket/Vket Trigger/Vket OnBooth Program Asset", fileName = "New Vket OnBooth Program Asset")]
    public class VketOnBoothProgramAsset : VketTriggerProgramAsset
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
                triggersProperty.arraySize = 2;
                SerializedProperty enterTriggerProperty = triggersProperty.GetArrayElementAtIndex(0);
                enterTriggerProperty.FindPropertyRelative("type").enumValueIndex = (int)TriggerParameters.TriggerType.OnBoothEnter;
                enterTriggerProperty.FindPropertyRelative("broadcast").enumValueIndex = (int)TriggerParameters.BroadcastType.Local;
                enterTriggerProperty.FindPropertyRelative("actions").arraySize = 0;
                SerializedProperty exitTriggerProperty = triggersProperty.GetArrayElementAtIndex(1);
                exitTriggerProperty.FindPropertyRelative("type").enumValueIndex = (int)TriggerParameters.TriggerType.OnBoothExit;
                exitTriggerProperty.FindPropertyRelative("broadcast").enumValueIndex = (int)TriggerParameters.BroadcastType.Local;
                exitTriggerProperty.FindPropertyRelative("actions").arraySize = 0;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
