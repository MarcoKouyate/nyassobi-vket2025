using System.Collections.Generic;
using System;
using UdonSharp;
using UnityEngine;
using VRC.Udon;
using VRC.SDK3.Components;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using Vket.EssentialResources;
#if UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
using VRC.Udon.Serialization.OdinSerializer;
using SerializationUtility = VRC.Udon.Serialization.OdinSerializer.SerializationUtility;
using UnityEngine.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Vket.ExhibitorUdonManager
{
    [IgnoreBuild]
    public class ExhibitorBoothManagerSetup : MonoBehaviour
#if UNITY_EDITOR
        , IProcessSceneWithReport
#endif
    {
        [SerializeField] private Transform _boothRoot;
        [SerializeField] private Vector3 _boothActivatorAreaSize = new Vector3(6.0f, 6.0f, 8.0f);
        [SerializeField] private UdonBehaviour[] _attachedReferences;
        [SerializeField] private string[] _targetVariables;

#if UNITY_EDITOR

        public int callbackOrder => -1000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                var boothManagerSetup = rootObject.GetComponentInChildren<ExhibitorBoothManagerSetup>();
                if (boothManagerSetup == null)
                    continue;

                Process(boothManagerSetup);
            }
        }

        private static void Process(ExhibitorBoothManagerSetup boothManagerSetup)
        {
            var exhibitorBoothManager = boothManagerSetup.GetComponent<ExhibitorBoothManager>();
            if(exhibitorBoothManager == null)
            {
                Debug.LogError("ExhibitorBoothManager not found.", boothManagerSetup);
                return;
            }

            Transform boothRoot = boothManagerSetup._boothRoot;
            Vector3 boothActivatorAreaSize = boothManagerSetup._boothActivatorAreaSize;
            UdonBehaviour[] attachedReferences = boothManagerSetup._attachedReferences;
            string[] targetVariables = boothManagerSetup._targetVariables;

            // Find Booth Root Transform
            if (boothManagerSetup._boothRoot == null) {
                foreach (var rootObject in boothManagerSetup.gameObject.scene.GetRootGameObjects())
                {
                    if (int.TryParse(rootObject.name, out _))
                    {
                        boothRoot = rootObject.transform;
                        EditorUtility.SetDirty(boothManagerSetup);
                    }
                }
                if (boothRoot == null)
                {
                    Debug.LogWarning("BoothRoot is not found.");
                    return;
                }
            }

            // Modify VketPrefabs Components
            var udonSharpComponents = boothRoot.GetComponentsInChildren<UdonSharpBehaviour>(true);
            List<Component> soundFadeComponentsList = new List<Component>();
            List<AudioSource> soundFadeAudioSourcesList = new List<AudioSource>();
            List<Component> languageSwitcherComponentsList = new List<Component>();
            string jpUrl, enUrl;
#if UNITY_ANDROID
            jpUrl = "https://store-preview.vketcdn.com/2024Summer/item-656_ja_android.json";
            enUrl = "https://store-preview.vketcdn.com/2024Summer/item-656_en_android.json";
#else
            jpUrl = "https://store-preview.vketcdn.com/2024Summer/item-656_ja_pc.json";
            enUrl = "https://store-preview.vketcdn.com/2024Summer/item-656_en_pc.json";
#endif
            foreach (var usb in udonSharpComponents)
            {
                switch(usb.GetUdonTypeName())
                {
                    case "VketSoundFade":
                        soundFadeComponentsList.Add(usb.GetComponent<UdonBehaviour>());
                        var audioSource = usb.GetComponentInChildren<AudioSource>();
                        if (audioSource != null)
                            soundFadeAudioSourcesList.Add(audioSource);
                        break;
                    case "VketLanguageSwitcher":
                        languageSwitcherComponentsList.Add(usb.GetComponent<UdonBehaviour>());
                        break;
                    case "VketStorePreviewOpener":
                        var so = new SerializedObject(usb);
                        so.Update();
                        var enProp = so.FindProperty("dataURL_EN").FindPropertyRelative("url").stringValue = jpUrl;
                        var jpProp = so.FindProperty("dataURL_JP").FindPropertyRelative("url").stringValue = enUrl;
                        so.ApplyModifiedProperties();
                        break;
                }
            }
            exhibitorBoothManager.SoundFadeComponents = soundFadeComponentsList.ToArray();
            exhibitorBoothManager.LanguageSwitcherComponents = languageSwitcherComponentsList.ToArray();

            // Modify UdonBehaviour Components
            List<Component> startComponentsList = new List<Component>();
            List<Component> udonComponentsList = new List<Component>();
            List<Component> isQuestComponentsList = new List<Component>();
            List<int> callbackMasksList = new List<int>();
            foreach(var udonBehaviour in boothRoot.GetComponentsInChildren<UdonBehaviour>(true))
            {
                var serializedUdonProgramAsset = new SerializedObject(udonBehaviour)
                    .FindProperty("serializedProgramAsset").objectReferenceValue as AbstractSerializedUdonProgramAsset;
                var program = serializedUdonProgramAsset.RetrieveProgram();

                int callbackMask = GetCallbackMask(program.EntryPoints.GetExportedSymbols(), out bool hasVketStart);

                if (hasVketStart)
                    startComponentsList.Add(udonBehaviour);

                if (callbackMask > 0)
                {
                    udonComponentsList.Add(udonBehaviour);
                    callbackMasksList.Add(callbackMask);
                }

                if (HasIsQuestVariable(program.SymbolTable.GetExportedSymbols()))
                {
                    isQuestComponentsList.Add(udonBehaviour);
                }

                // Attach UdonBehaviour Reference
                for(int refIdx=0; refIdx < attachedReferences.Length; refIdx++)
                {
                    if (refIdx >= targetVariables.Length)
                        break;

                    if (attachedReferences[refIdx] != null && HasVariable(targetVariables[refIdx], program.SymbolTable.GetExportedSymbols()))
                    {
                        var usb = udonBehaviour.GetComponent<UdonSharpBehaviour>();
                        if (usb != null) UdonSharpEditorUtility.CopyProxyToUdon(usb);
                        // UdonBehaviour Variables
                        if (!udonBehaviour.publicVariables.TrySetVariableValue(targetVariables[refIdx], attachedReferences[refIdx]))
                        {
                            if (!udonBehaviour.publicVariables.TryAddVariable(CreateUdonVariable(targetVariables[refIdx], attachedReferences[refIdx], typeof(UdonBehaviour))))
                                Debug.LogError($"Failed to set public variable value.");
                        }
                        EditorUtility.SetDirty(udonBehaviour);

                        if (usb != null)
                        {
                            UdonSharpEditorUtility.CopyUdonToProxy(usb);
                            EditorUtility.SetDirty(usb);
                        }

                        // CyanTrigger Variables
                        Component cyanTriggerComponent = udonBehaviour.GetComponent("CyanTriggerBehaviour");
                        if (cyanTriggerComponent != null)
                        {
                            switch (cyanTriggerComponent.GetType().Name)
                            {
                                case "CyanTrigger":
                                    {
                                        SerializedObject so = new SerializedObject(cyanTriggerComponent);
                                        so.Update();
                                        var variablesProperty = so.FindProperty("triggerInstance").FindPropertyRelative("triggerDataInstance").FindPropertyRelative("variables");
                                        for (int i = 0; i < variablesProperty.arraySize; i++)
                                        {
                                            if (variablesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == targetVariables[refIdx])
                                            {
                                                var dataProperty = variablesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("data");

                                                dataProperty.FindPropertyRelative("objEncoded").stringValue =
                                                    EncodeObject(attachedReferences[refIdx], out List<UnityEngine.Object> unityObjects);

                                                var unityObjectsProperty = dataProperty.FindPropertyRelative("unityObjects");
                                                unityObjectsProperty.arraySize = 1;
                                                unityObjectsProperty.GetArrayElementAtIndex(0).objectReferenceValue = unityObjects[0];
                                            }
                                        }
                                        so.ApplyModifiedProperties();
                                        break;
                                    }
                                case "CyanTriggerAsset":
                                    {
                                        string targetVariableGuid = "";
                                        SerializedObject programAssetSO = new SerializedObject(udonBehaviour.programSource);
                                        programAssetSO.Update();
                                        var variableProperty = programAssetSO.FindProperty("ctDataInstance").FindPropertyRelative("variables");
                                        for (int i = 0; i < variableProperty.arraySize; i++)
                                        {
                                            if (variableProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == targetVariables[i])
                                            {
                                                targetVariableGuid = variableProperty.GetArrayElementAtIndex(i).FindPropertyRelative("variableID").stringValue;
                                                break;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(targetVariableGuid))
                                        {
                                            SerializedObject so = new SerializedObject(cyanTriggerComponent);
                                            so.Update();
                                            var variableGuidsProperty = so.FindProperty("assetInstance").FindPropertyRelative("variableGuids");
                                            var variableDataProperty = so.FindProperty("assetInstance").FindPropertyRelative("variableData");
                                            for (int i = 0; i < variableGuidsProperty.arraySize; i++)
                                            {
                                                if (variableGuidsProperty.GetArrayElementAtIndex(i).stringValue == targetVariableGuid)
                                                {
                                                    variableDataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("objEncoded").stringValue =
                                                        EncodeObject(attachedReferences[refIdx], out List<UnityEngine.Object> unityObjects);

                                                    var unityObjectsProperty = variableDataProperty.GetArrayElementAtIndex(i).FindPropertyRelative("unityObjects");
                                                    unityObjectsProperty.arraySize = 1;
                                                    unityObjectsProperty.GetArrayElementAtIndex(0).objectReferenceValue = unityObjects[0];
                                                }
                                            }
                                            so.ApplyModifiedProperties();
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                }

            }
            exhibitorBoothManager.UdonComponents = udonComponentsList.ToArray();
            exhibitorBoothManager.CallbackMasks = callbackMasksList.ToArray();
            exhibitorBoothManager.StartComponents = startComponentsList.ToArray();
            exhibitorBoothManager.IsQuestComponents = isQuestComponentsList.ToArray();
            // Modify Pickups
            List<VRCPickup> pickupsList = new List<VRCPickup>();
            List<Vector3> initPositionsList = new List<Vector3>();
            List<Quaternion> initRotationsList = new List<Quaternion>();
            var pickups = boothRoot.GetComponentsInChildren<VRCPickup>(true);
            for(int i=0; i < pickups.Length; i++)
            {
                bool isPickupPrefab = false;
                var usb = pickups[i].GetComponent<UdonSharpBehaviour>();
                if (usb != null)
                {
                    string udonTypeName = usb.GetUdonTypeName();
                    if (udonTypeName == "VketPickup" || udonTypeName == "VketFollowPickup")
                        isPickupPrefab = true;
                }
                if (!isPickupPrefab)
                {
                    pickupsList.Add(pickups[i]);
                    initPositionsList.Add(pickups[i].transform.position);
                    initRotationsList.Add(pickups[i].transform.rotation);
                }
            }
            exhibitorBoothManager.Pickups = pickupsList.ToArray();
            exhibitorBoothManager.InitPositions = initPositionsList.ToArray();
            exhibitorBoothManager.InitRotations = initRotationsList.ToArray();
            // Modify Audio Sources
            List<AudioSource> audioSourcesList = new List<AudioSource>(boothRoot.GetComponentsInChildren<AudioSource>(true));
            foreach (var soundFadeAudio in soundFadeAudioSourcesList)
                audioSourcesList.Remove(soundFadeAudio);
            exhibitorBoothManager.AudioSources = audioSourcesList.ToArray();

            // Modify Cameras
            exhibitorBoothManager.Cameras = boothRoot.GetComponentsInChildren<Camera>(true);

            // Modify Projector Objects
            List<GameObject> projectorObjectsList = new List<GameObject>();
            foreach (var projector in boothRoot.GetComponentsInChildren<Projector>(true))
                projectorObjectsList.Add(projector.gameObject);
            exhibitorBoothManager.ProjectorObjects = projectorObjectsList.ToArray();

            // Modify Video Players
            exhibitorBoothManager.VideoPlayers = boothRoot.GetComponentsInChildren<VRC.SDK3.Video.Components.Base.BaseVRCVideoPlayer>(true);
            EditorUtility.SetDirty(exhibitorBoothManager);



            // Modify Booth Activator
            var boothActivator = boothManagerSetup.GetComponentInChildren<ExhibitorBoothActivator>();
            if (boothActivator != null)
            {
                var boothActivatorCollider = boothActivator.GetComponent<BoxCollider>();
                if (boothActivatorCollider != null)
                {
                    boothActivatorCollider.center = new Vector3(0, boothActivatorAreaSize.y * 0.5f, 0);
                    boothActivatorCollider.size = boothActivatorAreaSize;
                    EditorUtility.SetDirty(boothActivatorCollider);
                }
            }

            // Modify Rigidbody Manager
            var rigidbodyManager = boothManagerSetup.GetComponentInChildren<RigidbodyManager>();
            if (rigidbodyManager != null)
            {
                List<Rigidbody> rigidbodiesList = new List<Rigidbody>();
                List<VRCObjectSync> objectsyncsList = new List<VRCObjectSync>();
                foreach (var rb in boothRoot.GetComponentsInChildren<Rigidbody>())
                {
                    if (rb.isKinematic)
                        continue;

                    var objectSync = rb.GetComponent<VRCObjectSync>();
                    if (objectSync == null)
                        rigidbodiesList.Add(rb);
                    else
                        objectsyncsList.Add(objectSync);
                }
                rigidbodyManager.Rigidbodies = rigidbodiesList.ToArray();
                rigidbodyManager.ObjectSyncs = objectsyncsList.ToArray();
                EditorUtility.SetDirty(rigidbodyManager);

                if (rigidbodyManager.BoxCollider != null)
                {
                    rigidbodyManager.BoxCollider.center = new Vector3(0, boothActivatorAreaSize.y * 0.5f, 0);
                    rigidbodyManager.BoxCollider.size = boothActivatorAreaSize;
                    EditorUtility.SetDirty(rigidbodyManager.BoxCollider);
                }
            }
        }

        private static int GetCallbackMask(System.Collections.Immutable.ImmutableArray<string> exportedSymbols, out bool hasVketStart)
        {
            hasVketStart = false;
            int callbackMask = 0;

            foreach (var symbol in exportedSymbols)
            {
                if (!hasVketStart && symbol == VketCallbacks.NAME_OF_VKET_START)
                    hasVketStart = true;

                if (symbol == VketCallbacks.NAME_OF_VKET_UPDATE)
                    callbackMask += 1;
                else if (symbol == VketCallbacks.NAME_OF_VKET_FIXED_UPDATE)
                    callbackMask += 1 << 1;
                else if (symbol == VketCallbacks.NAME_OF_VKET_LATE_UPDATE)
                    callbackMask += 1 << 2;
                else if (symbol == VketCallbacks.NAME_OF_VKET_POST_LATE_UPDATE)
                    callbackMask += 1 << 3;
                else if (symbol == VketCallbacks.NAME_OF_VKET_ON_BOOTH_ENTER)
                    callbackMask += 1 << 4;
                else if (symbol == VketCallbacks.NAME_OF_VKET_ON_BOOTH_EXIT)
                    callbackMask += 1 << 5;
            }

            return callbackMask;
        }

        private static bool HasVariable(string targetSymbol, System.Collections.Immutable.ImmutableArray<string> exportedSymbols)
        {
            foreach (var symbol in exportedSymbols)
            {
                if (symbol == targetSymbol)
                    return true;
            }

            return false;
        }

        private static bool HasIsQuestVariable(System.Collections.Immutable.ImmutableArray<string> exportedSymbols)
        {
            foreach (var symbol in exportedSymbols)
            {
                if (symbol == VketCallbacks.NAME_OF_VKET_IS_QUEST)
                    return true;
            }

            return false;
        }

        private static IUdonVariable CreateUdonVariable(string symbolName, object value, Type declaredType)
        {
            Type udonVariableType = typeof(UdonVariable<>).MakeGenericType(declaredType);
            return (IUdonVariable)Activator.CreateInstance(udonVariableType, symbolName, value);
        }

        public static string EncodeObject(object obj, out List<UnityEngine.Object> unityObjects)
        {
            byte[] serializedBytes = SerializationUtility.SerializeValue(obj, DataFormat.Binary, out unityObjects);
            return Convert.ToBase64String(serializedBytes);
        }
#endif
                }
            }