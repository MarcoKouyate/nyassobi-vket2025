#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using UnityEditor.SceneManagement;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon;
using System;
using System.Linq;
using UnityEngine.Assertions;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

using UnityEditor.Callbacks;

namespace VketUdonAssembly
{
    public class VketTriggerProgramAsset : VRC.Udon.Editor.ProgramSources.UdonAssemblyProgramAsset
    {
        [SerializeField]
        public TriggerParameters.Trigger[] triggers;

        [SerializeField]
        private bool showAssembly = false;

        [NonSerialized, OdinSerialize]
        private Dictionary<string, (object value, Type type)> heapDefaultValues = new Dictionary<string, (object value, Type type)>();

        private UdonBehaviour udonBehaviour;
        protected SerializedObject serializedObject;
        //private Vket.UdonManager.VketUdonControl udonControl;

        private static bool hasPostProcessScene;

        [PostProcessScene(0)]
        public static void OnPostProcessScene()
        {
            hasPostProcessScene = true;
        }

        protected void OnEnable()
        {
            Undo.undoRedoPerformed += () =>
            {
                variablesList = new ReorderableList[0];
            };

            EditorSceneManager.sceneOpening += (string path, OpenSceneMode mode) =>
            {
                actionsList = new ReorderableList[0];
            };
        }

        // メイン描画
        protected override void DrawProgramSourceGUI(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            if (udonBehaviour != null)
            {
                this.udonBehaviour = udonBehaviour;

                if (udonBehaviour.SyncMethod != VRC.SDKBase.Networking.SyncType.Manual)
                {
                    Undo.RecordObject(udonBehaviour, "Change SyncMode");
                    udonBehaviour.SyncMethod = VRC.SDKBase.Networking.SyncType.Manual;
                    EditorUtility.SetDirty(udonBehaviour);
                }

                /*
                // UdonControl追加
                AddUdonControl();
                */

                // PublicVariables更新
                UpdatePublicVariables(ref dirty);

                // Trigger設定
                DrawCustomInspector(ref dirty);

                //DrawPublicVariables(udonBehaviour, ref dirty);

                if (hasDirty)
                    return;

                DrawAssemblyErrorTextArea();

                if (showAssembly)
                    DrawAssemblyTextArea(false, ref dirty);
            }
        }

        // コンパイル
        protected override void RefreshProgramImpl()
        {
            if (triggers == null)
                return;

            udonAssembly = AssemblyBuilder.GetAssemblyStr(triggers, out heapDefaultValues);

            base.RefreshProgramImpl();
            ApplyDefaultValuesToHeap();
        }
        /*
        private void AddUdonControl()
        {
            if (udonControl == null)
            {
                udonControl = udonBehaviour.GetComponent<Vket.UdonManager.VketUdonControl>();
                if (udonControl == null)
                    udonControl = (Vket.UdonManager.VketUdonControl)Undo.AddComponent(udonBehaviour.gameObject, typeof(Vket.UdonManager.VketUdonControl));
            }

            if (this.GetType() == typeof(VketOnBoothProgramAsset))
            {
                if (!udonControl.onBoothEnter || !udonControl.onBoothExit)
                {
                    SerializedObject udonControlSO = new SerializedObject(udonControl);
                    udonControlSO.Update();
                    SerializedProperty enterProperty = udonControlSO.FindProperty("onBoothEnter");
                    enterProperty.boolValue = true;
                    SerializedProperty exitProperty = udonControlSO.FindProperty("onBoothExit");
                    exitProperty.boolValue = true;
                    udonControlSO.ApplyModifiedProperties();
                }
            }
        }
        */
        // トリガー編集
        private bool hasChanged;
        private bool hasDirty;
        private ReorderableList[] actionsList = new ReorderableList[0];
        private ReorderableList[] variablesList = new ReorderableList[0];
        private Dictionary<string, object> reorderVariables = new Dictionary<string, object>();
        private Color baseBackground;

        private readonly Color triggerBackground = new Color(0.9f, 0.9f, 0.9f);
        private readonly Color broadcastBackground = new Color(0.8f, 0.9f, 0.8f);
        private readonly Color interactionBackground = new Color(0.8f, 0.8f, 0.9f);
        private readonly Color actionsBackground = new Color(0.9f, 0.8f, 0.8f);

        private void DrawCustomInspector(ref bool dirty)
        {
            hasChanged = false;
            hasDirty = false;

            baseBackground = GUI.backgroundColor;

            // シリアライズドオブジェクト取得
            if (serializedObject == null)
                serializedObject = new SerializedObject(this);
            
            serializedObject.Update();

            // プロパティ取得
            SerializedProperty triggersProperty = serializedObject.FindProperty(nameof(this.triggers));
            int triggersSize = triggersProperty.arraySize;

            if (hasPostProcessScene)
            {
                actionsList = new ReorderableList[0];
                hasPostProcessScene = false;
            }

            // ReorderableList再構築
            if (triggersSize != actionsList.Length)
            {
                actionsList = new ReorderableList[triggersSize];
            }
            if (triggersSize != variablesList.Length)
            {
                variablesList = new ReorderableList[triggersSize];
            }

            if (triggersSize > 0)
            {
                // GUI開始
                EditorGUILayout.Separator();
                GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(1));
                EditorGUILayout.Separator();

                // トリガーGUI描画
                RenderTriggers(triggersProperty);
            }
            else
            {
                // 初期トリガー追加
                AddPropertyArray(triggersProperty);
                triggersProperty.FindPropertyRelative("type").enumValueIndex = (int)TriggerParameters.TriggerType.OnInteract;
                triggersProperty.FindPropertyRelative("broadcast").enumValueIndex = (int)TriggerParameters.BroadcastType.Local;
                triggersProperty.FindPropertyRelative("actions").arraySize = 0;

                hasChanged = true;
            }

            serializedObject.ApplyModifiedProperties();

            // リコンパイル
            if (hasChanged)
            {
                RefreshProgram();
            }

            if (hasDirty)
            {
                dirty = true;
            }
        }

        private void RenderTriggers(SerializedProperty triggers)
        {
            int triggersSize = triggers.arraySize;

            for (int i = 0; i < triggersSize; i++)
            {
                SerializedProperty triggerProperty = triggers.GetArrayElementAtIndex(i);
                SerializedProperty actionsProperty = triggerProperty.FindPropertyRelative("actions");

                // index修正
                for(int j=0; j < actionsProperty.arraySize; j++)
                {
                    SerializedProperty indexProperty = actionsProperty.GetArrayElementAtIndex(j).FindPropertyRelative("index");
                    if (indexProperty.intValue != j)
                        indexProperty.intValue = j;
                }

                // トリガータイプ描画
                int typeIndex = triggerProperty.FindPropertyRelative("type").enumValueIndex;
                string triggerTypeName = "";
                switch (typeIndex)
                {
                    case (int)TriggerParameters.TriggerType.OnInteract:
                        triggerTypeName = "On Interact";
                        break;
                    case (int)TriggerParameters.TriggerType.OnBoothEnter:
                        triggerTypeName = "On Booth Enter";
                        break;
                    case (int)TriggerParameters.TriggerType.OnBoothExit:
                        triggerTypeName = "On Booth Exit";
                        break;
                    default:
                        break;
                }
                var style = new GUIStyle("ShurikenModuleTitle");
                style.margin = new RectOffset(0, 0, 8, 0);
                style.font = new GUIStyle(EditorStyles.boldLabel).font;
                style.fontSize = 14;
                style.border = new RectOffset(15, 7, 4, 4);
                style.fixedHeight = 22;
                style.contentOffset = new Vector2(6f, -2f);
                var rect = GUILayoutUtility.GetRect(16.0f, 22, style);
                GUI.Box(rect, triggerTypeName, style);

                // 背景描画
                BeginVerticalBox(triggerBackground);

                // Broadcastブロック描画
                DrawBroadcastArea(triggerProperty);

                EditorGUILayout.Separator();

                // Interactionブロック描画
                if (typeIndex == (int)TriggerParameters.TriggerType.OnInteract)
                {
                    DrawInteractionArea();
                }

                // Actionsブロック描画
                if (actionsList.Length == triggersSize)
                {
                    // Actionリスト描画
                    RenderActionEditor(actionsProperty, i);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
            }
        }

        private void DrawBroadcastArea(SerializedProperty triggerProperty)
        {
            BeginVerticalBox(broadcastBackground);
            EditorGUILayout.LabelField("Broadcast", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            SerializedProperty broadcastProperty = triggerProperty.FindPropertyRelative("broadcast");
            broadcastProperty.enumValueIndex = GUILayout.Toolbar(broadcastProperty.enumValueIndex, new string[] { "Local", "All Player"});

            if (EditorGUI.EndChangeCheck())
                hasChanged = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawInteractionArea()
        {
            BeginVerticalBox(interactionBackground);

            EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (udonBehaviour != null)
            {
                udonBehaviour.interactText = EditorGUILayout.TextField("Interaction Text", udonBehaviour.interactText);
                udonBehaviour.proximity = EditorGUILayout.Slider("Proximity", udonBehaviour.proximity, 0f, 100f);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Interaction Text", "Use");
                    EditorGUILayout.Slider("Proximity", 2.0f, 0f, 100f);
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }

        private void RenderActionEditor(SerializedProperty actionsProperty, int idx)
        {
            BeginVerticalBox(actionsBackground);

            EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);

            if (actionsList[idx] == null)
            {
                // ReorderableList構築
                ReorderableList newList = new ReorderableList(serializedObject, actionsProperty, true, false, true, true);
                // ヘッダー描画処理
                newList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Actions");
                // Element描画処理
                newList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty actionProperty = actionsProperty.GetArrayElementAtIndex(index);

                    int actionType = actionsProperty.GetArrayElementAtIndex(index).FindPropertyRelative("type").enumValueIndex;
                    string operation = "";
                    int enumIndex;
                    switch(actionType)
                    {
                        case (int)TriggerParameters.ActionType.SetGameObjectActive:
                        case (int)TriggerParameters.ActionType.AnimationBool:
                            enumIndex = actionProperty.FindPropertyRelative("parameterBoolOp").enumValueIndex;
                            operation = $" ({Enum.GetName(typeof(TriggerParameters.BoolOp), enumIndex)})";
                            break;
                        case (int)TriggerParameters.ActionType.AnimationFloat:
                        case (int)TriggerParameters.ActionType.AnimationInt:
                            enumIndex = actionProperty.FindPropertyRelative("parameterAnimatorOp").enumValueIndex;
                            operation = $" ({Enum.GetName(typeof(TriggerParameters.AnimatorOp), enumIndex)})";
                            break;
                        default:
                            break;
                    }
                    EditorGUI.LabelField(rect, Enum.GetName(typeof(TriggerParameters.ActionType), actionType) + operation);

                    if (isFocused)
                    {
                        variablesList[idx] = null;
                    }

                    if (isActive)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(EditorGUIUtility.IconContent("Icon Dropdown"), GUI.skin.label))
                        {
                            actionsList = new ReorderableList[0];
                            EditorGUILayout.EndHorizontal();
                            return;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();

                        // Actionパラメータブロック
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.LabelField(Enum.GetName(typeof(TriggerParameters.ActionType), actionType), EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;

                        // Actionパラメータ描画
                        if (RenderAction(actionProperty))
                        {
                            EditorGUI.indentLevel--;
                            EditorGUILayout.EndVertical();

                            serializedObject.ApplyModifiedProperties();
                            hasChanged = true;
                            RefreshProgram();

                            return;
                        }

                        EditorGUILayout.Space();
                        EditorGUI.indentLevel--;

                        // Variables描画
                        RenderVariablesEditor(actionProperty, idx, index);

                        EditorGUILayout.EndVertical();
                    }
                };
                // Action並び替え処理
                newList.onReorderCallback = (ReorderableList list) =>
                {
                    reorderVariables = SortActionsAndVariables(actionsProperty, idx);

                    serializedObject.ApplyModifiedProperties();
                    hasChanged = true;
                    RefreshProgram();
                };
                // Action追加処理
                newList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (TriggerParameters.ActionType type in Enum.GetValues(typeof(TriggerParameters.ActionType)))
                    {
                        menu.AddItem(new GUIContent(type.ToString()), false, (t) =>
                        {
                            serializedObject.Update();
                            int size = actionsProperty.arraySize;
                            actionsProperty.arraySize++;
                            SerializedProperty actionProperty = actionsProperty.GetArrayElementAtIndex(size);
                            actionProperty.FindPropertyRelative("type").enumValueIndex = (int)t;
                            actionProperty.FindPropertyRelative("parameterBoolOp").enumValueIndex = (int)TriggerParameters.BoolOp.False;
                            actionProperty.FindPropertyRelative("parameterAnimatorOp").enumValueIndex = (int)TriggerParameters.AnimatorOp.Set;
                            actionProperty.FindPropertyRelative("parameterString").stringValue = "";
                            actionProperty.FindPropertyRelative("parameterFloat").floatValue = 0;
                            actionProperty.FindPropertyRelative("parameterInt").intValue = 0;
                            actionProperty.FindPropertyRelative("foldout").boolValue = false;
                            actionProperty.FindPropertyRelative("index").intValue = size;
                            actionProperty.FindPropertyRelative("variableIndex").arraySize = 0;
                            /*
                            AddPropertyArray(actionsProperty);
                            actionsProperty.FindPropertyRelative("type").enumValueIndex = (int)t;
                            actionsProperty.FindPropertyRelative("parameterBoolOp").enumValueIndex = (int)TriggerParameters.BoolOp.False;
                            actionsProperty.FindPropertyRelative("parameterAnimatorOp").enumValueIndex = (int)TriggerParameters.AnimatorOp.Set;
                            actionsProperty.FindPropertyRelative("parameterString").stringValue = "";
                            actionsProperty.FindPropertyRelative("parameterFloat").floatValue = 0;
                            actionsProperty.FindPropertyRelative("parameterInt").intValue = 0;
                            actionsProperty.FindPropertyRelative("foldout").boolValue = false;
                            actionsProperty.FindPropertyRelative("index").intValue = size;
                            actionsProperty.FindPropertyRelative("variableIndex").arraySize = 0;
                            */

                            serializedObject.ApplyModifiedProperties();
                            hasChanged = true;
                            RefreshProgram();
                        }, type);
                    }
                    menu.ShowAsContext();

                    actionsList = new ReorderableList[0];
                    variablesList = new ReorderableList[0];
                };
                // Action削除処理
                newList.onRemoveCallback = (ReorderableList list) =>
                {
                    list.serializedProperty.Copy().DeleteArrayElementAtIndex(list.index);
                    serializedObject.ApplyModifiedProperties();

                    serializedObject.Update();
                    reorderVariables = SortActionsAndVariables(actionsProperty, idx);

                    serializedObject.ApplyModifiedProperties();
                    hasChanged = true;
                    RefreshProgram();
                };

                actionsList[idx] = newList;
            }
            actionsList[idx].DoLayoutList();

            EditorGUILayout.EndVertical();
        }

        private Dictionary<string, object> SortActionsAndVariables(SerializedProperty actionsProperty, int triggerIdx)
        {
            Dictionary<string, object> sortedVariables = new Dictionary<string, object>();
            IUdonSymbolTable symbolTable = program.SymbolTable;

            List<object> variables = new List<object>();

            int symbolCount = 0;
            while(true)
            {
                string symbol = $"objectArray_{triggerIdx}_{symbolCount}";
                if (!symbolTable.HasExportedSymbol(symbol))
                    break;

                if (!udonBehaviour.publicVariables.TryGetVariableValue(symbol, out object value))
                {
                    Debug.LogError($"TryGetVariableError: {symbol}");
                }
                if (value == null)
                {
                    Type symbolType = symbolTable.GetSymbolType(symbol);
                    if (symbolType.IsArray)
                    {
                        value = Array.CreateInstance(symbolType.GetElementType(), 1);
                    }
                }
                variables.Add(value);

                symbolCount++;
            }

            for (int i = 0; i < actionsProperty.arraySize; i++)
            {
                SerializedProperty indexProperty = actionsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("index");
                if(i != indexProperty.intValue)
                {
                    sortedVariables.Add($"objectArray_{triggerIdx}_{i}", variables[indexProperty.intValue]);
                    indexProperty.intValue = i;
                }
            }

            return sortedVariables;
        }

        private void RenderVariablesEditor(SerializedProperty actionProperty, int triggerIdx, int actionIdx)
        {
            if (variablesList[triggerIdx] == null)
            {
                SerializedProperty indexProperty = actionProperty.FindPropertyRelative("variableIndex");

                string symbol = $"objectArray_{triggerIdx}_{actionIdx}";
                Type variableType = program.SymbolTable.GetSymbolType(symbol);
                Type elementType = program.SymbolTable.GetSymbolType(symbol).GetElementType();
                if (!(variableType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(elementType)))
                {
                    EditorGUILayout.LabelField($"TypeError: {variableType.ToString()}");
                    return;
                }

                if (!udonBehaviour.publicVariables.TryGetVariableValue(symbol, out object value))
                {
                    Debug.LogError($"Failed Get Variable Value: {symbol}");
                }
                if (value == null)
                    value = Array.CreateInstance(elementType, 0);

                UnityEngine.Object[] variableValue = (UnityEngine.Object[])value;

                indexProperty.arraySize = variableValue.Length;
                for (int i = 0; i < indexProperty.arraySize; i++)
                    indexProperty.GetArrayElementAtIndex(i).intValue = i;
                serializedObject.ApplyModifiedProperties();

                //Debug.Log("Create");

                // ReorderableList構築
                ReorderableList newList = new ReorderableList(serializedObject, indexProperty, true, true, true, true);
                // ヘッダー描画処理
                newList.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Receivers");

                    // ドラッグ&ドロップ
                    if (!rect.Contains(Event.current.mousePosition))
                        return;

                    var objects = DragAndDrop.objectReferences.Where(obj => !AssetDatabase.IsMainAsset(obj)).ToArray();
                    List<UnityEngine.Object> draggableObjects = new List<UnityEngine.Object>();
                    foreach (var obj in objects)
                    {
                        if (elementType == typeof(GameObject))
                        {
                            draggableObjects.Add(obj);
                        }
                        else if(elementType == typeof(Animator))
                        {
                            GameObject go = (GameObject)obj;
                            Animator animator = go.GetComponent<Animator>();
                            if (animator != null)
                                draggableObjects.Add(animator);
                        }
                    }

                    if (draggableObjects.Count > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (Event.current.type == EventType.DragPerform)
                        {
                            List<UnityEngine.Object> newValue = new List<UnityEngine.Object>(variableValue);

                            int addIdx = indexProperty.arraySize;
                            foreach (var obj in draggableObjects)
                            {
                                indexProperty.arraySize++;
                                indexProperty.GetArrayElementAtIndex(addIdx).intValue = addIdx;
                                newValue.Add(obj);

                                addIdx++;
                            }
                            variableValue = newValue.ToArray();

                            variableValue = SortVariableValue(indexProperty, variableValue);
                            StoreVariables(variableValue, symbol, variableType);

                            DragAndDrop.AcceptDrag();
                            Event.current.Use();

                            variablesList[triggerIdx] = null;
                            return;
                        }
                    }
                };
                // Element描画処理
                newList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    EditorGUI.BeginChangeCheck();

                    variableValue[index] = EditorGUI.ObjectField(rect, variableValue[index], elementType, true);

                    if (EditorGUI.EndChangeCheck())
                    {
                        //Debug.Log("ChangeCheck");

                        StoreVariables(variableValue, symbol, variableType);

                        variablesList[triggerIdx] = null;
                        return;
                    }
                };
                // Variable変更時処理
                newList.onChangedCallback = (ReorderableList list) =>
                {
                    variableValue = SortVariableValue(indexProperty, variableValue);
                    StoreVariables(variableValue, symbol, variableType);

                    for (int i = 0; i < indexProperty.arraySize; i++)
                        indexProperty.GetArrayElementAtIndex(i).intValue = i;

                    serializedObject.ApplyModifiedProperties();
                };
                // Variable追加処理
                newList.onAddCallback = (ReorderableList list) =>
                {
                    int addIdx = indexProperty.arraySize;
                    indexProperty.arraySize++;
                    indexProperty.GetArrayElementAtIndex(addIdx).intValue = addIdx;
                    serializedObject.ApplyModifiedProperties();
                };
                // Variable削除処理
                newList.onRemoveCallback = (ReorderableList list) =>
                {
                    indexProperty.Copy().DeleteArrayElementAtIndex(list.index);
                    serializedObject.ApplyModifiedProperties();
                };
                
                variablesList[triggerIdx] = newList;
            }
            variablesList[triggerIdx].DoLayoutList();
        }

        private UnityEngine.Object[] SortVariableValue(SerializedProperty indexProperty, UnityEngine.Object[] variableValue)
        {
            List<UnityEngine.Object> newValue = new List<UnityEngine.Object>();
            for (int i = 0; i < indexProperty.arraySize; i++)
            {
                int idx = indexProperty.GetArrayElementAtIndex(i).intValue;
                if (idx < variableValue.Length)
                    newValue.Add(variableValue[idx]);
                else
                    newValue.Add(null);
            }

            return newValue.ToArray();
        }

        private void StoreVariables(UnityEngine.Object[] value, string symbol, Type variableType)
        {
            //serializedObject.Update();

            Type elementType = variableType.GetElementType();
            Array destinationArray = Array.CreateInstance(elementType, value.Length);
            Array.Copy(value, destinationArray, value.Length);
            object variableValue = destinationArray;

            Undo.RecordObject(udonBehaviour, "Modify Public Variable");

            if (!udonBehaviour.publicVariables.TrySetVariableValue(symbol, variableValue))
            {
                Debug.Log("SetFailed");
                if (!udonBehaviour.publicVariables.TryAddVariable(CreateUdonVariable(symbol, variableValue, variableType)))
                {
                    Debug.LogError($"Failed to set public variable '{symbol}' value.");
                }
            }

            EditorUtility.SetDirty(udonBehaviour);

            EditorSceneManager.MarkSceneDirty(udonBehaviour.gameObject.scene);

            if (PrefabUtility.IsPartOfPrefabInstance(udonBehaviour))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(udonBehaviour);
            }

            hasDirty = true;
        }

        private bool RenderAction(SerializedProperty actionProperty)
        {
            // Action Type
            SerializedProperty actionTypeProperty = actionProperty.FindPropertyRelative("type");

            EditorGUI.BeginChangeCheck();
            TriggerParameters.ActionType actionType = (TriggerParameters.ActionType)actionTypeProperty.enumValueIndex;
            switch (actionType)
            {
                case TriggerParameters.ActionType.SetGameObjectActive:
                    RenderSetGameObject(actionProperty);
                    break;
                case TriggerParameters.ActionType.AnimationFloat:
                    RenderAnimationFloat(actionProperty);
                    break;
                case TriggerParameters.ActionType.AnimationInt:
                    RenderAnimationInt(actionProperty);
                    break;
                case TriggerParameters.ActionType.AnimationBool:
                    RenderAnimationBool(actionProperty);
                    break;
                case TriggerParameters.ActionType.AnimationTrigger:
                    RenderAnimationTrigger(actionProperty);
                    break;
                default:
                    break;
            }

            if (EditorGUI.EndChangeCheck())
                return true;

            return false;
        }

#region RenderActions
        private void RenderSetGameObject(SerializedProperty actionProperty)
        {
            SerializedProperty boolOpProperty = actionProperty.FindPropertyRelative("parameterBoolOp");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Operation");
            boolOpProperty.enumValueIndex = 
                EditorGUILayout.Popup(boolOpProperty.enumValueIndex, Enum.GetNames(typeof(TriggerParameters.BoolOp)));
            EditorGUILayout.EndHorizontal();
        }

        private void RenderAnimationFloat(SerializedProperty actionProperty)
        {
            SerializedProperty boolOpProperty = actionProperty.FindPropertyRelative("parameterAnimatorOp");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Operation");
            boolOpProperty.enumValueIndex =
                EditorGUILayout.Popup(boolOpProperty.enumValueIndex, Enum.GetNames(typeof(TriggerParameters.AnimatorOp)));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Parameter");
            EditorGUILayout.PropertyField(actionProperty.FindPropertyRelative("parameterString"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Float Value");
            EditorGUILayout.PropertyField(actionProperty.FindPropertyRelative("parameterFloat"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            float value = actionProperty.FindPropertyRelative("parameterFloat").floatValue;
            if (boolOpProperty.enumValueIndex == (int)TriggerParameters.AnimatorOp.Divide && value == 0)
            {
                actionProperty.FindPropertyRelative("parameterFloat").floatValue = 1.0f;
            }
        }

        private void RenderAnimationInt(SerializedProperty actionProperty)
        {
            SerializedProperty boolOpProperty = actionProperty.FindPropertyRelative("parameterAnimatorOp");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Operation");
            boolOpProperty.enumValueIndex =
                EditorGUILayout.Popup(boolOpProperty.enumValueIndex, Enum.GetNames(typeof(TriggerParameters.AnimatorOp)));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Parameter");
            EditorGUILayout.PropertyField(actionProperty.FindPropertyRelative("parameterString"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Int Value");
            EditorGUILayout.PropertyField(actionProperty.FindPropertyRelative("parameterInt"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            int value = actionProperty.FindPropertyRelative("parameterInt").intValue;
            if (boolOpProperty.enumValueIndex == (int)TriggerParameters.AnimatorOp.Divide && value == 0)
            {
                actionProperty.FindPropertyRelative("parameterInt").intValue = 1;
            }
        }

        private void RenderAnimationBool(SerializedProperty actionProperty)
        {
            SerializedProperty boolOpProperty = actionProperty.FindPropertyRelative("parameterBoolOp");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Operation");
            boolOpProperty.enumValueIndex =
                EditorGUILayout.Popup(boolOpProperty.enumValueIndex, Enum.GetNames(typeof(TriggerParameters.BoolOp)));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Parameter");
            EditorGUILayout.PropertyField(actionProperty.FindPropertyRelative("parameterString"), GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }

        private void RenderAnimationTrigger(SerializedProperty actionProperty)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Parameter");
            EditorGUILayout.PropertyField(actionProperty.FindPropertyRelative("parameterString"), GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        private void UpdatePublicVariables(ref bool dirty)
        {
            IUdonVariableTable publicVariables = null;
            if (udonBehaviour != null)
            {
                publicVariables = udonBehaviour.publicVariables;
            }
            
            if(program?.SymbolTable == null)
            {
                return;
            }
            
            IUdonSymbolTable symbolTable = program.SymbolTable;
            // Remove non-exported public variables
            if(publicVariables != null)
            {
                foreach(string publicVariableSymbol in publicVariables.VariableSymbols.ToArray())
                {
                    if(!symbolTable.HasExportedSymbol(publicVariableSymbol))
                    {
                        publicVariables.RemoveVariable(publicVariableSymbol);
                    }
                }
            }

            ImmutableArray<string> exportedSymbolNames = symbolTable.GetExportedSymbols();
            if(exportedSymbolNames.Length <= 0)
            {
                return;
            }

            int actionCount = 0;
            foreach(var trigger in triggers)
            {
                foreach (var action in trigger.actions)
                    actionCount++;
            }
            if (exportedSymbolNames.Length != actionCount)
            {
                RefreshProgram();
                return;
            }
            
            foreach (string exportedSymbol in exportedSymbolNames)
            {
                // Type不整合修正
                Type symbolType = symbolTable.GetSymbolType(exportedSymbol);
                if (!publicVariables.TryGetVariableType(exportedSymbol, out Type declaredType) || declaredType != symbolType)
                {
                    publicVariables.RemoveVariable(exportedSymbol);
                    if (!publicVariables.TryAddVariable(CreateUdonVariable(exportedSymbol, GetPublicVariableDefaultValue(exportedSymbol, declaredType), symbolType)))
                    {
                        EditorGUILayout.LabelField($"Error drawing field for symbol '{exportedSymbol}'.");
                        continue;
                    }
                }
                
                // Value取得
                if (!publicVariables.TryGetVariableValue(exportedSymbol, out object variableValue))
                {
                    Debug.LogError("Failed TryGetVariableValue:" + exportedSymbol);
                    //variableValue = GetPublicVariableDefaultValue(exportedSymbol, declaredType);
                }
                
                // 配列型null修正
                if (variableValue == null && symbolType.IsArray)
                {
                    variableValue = Array.CreateInstance(symbolType.GetElementType(), 1);

                    Undo.RecordObject(udonBehaviour, "Modify Public Variable");

                    if (!publicVariables.TrySetVariableValue(exportedSymbol, variableValue))
                    {
                        if (!publicVariables.TryAddVariable(CreateUdonVariable(exportedSymbol, variableValue, symbolType)))
                        {
                            Debug.LogError($"Failed to set public variable '{exportedSymbol}' value.");
                        }
                    }

                    EditorSceneManager.MarkSceneDirty(udonBehaviour.gameObject.scene);

                    if (PrefabUtility.IsPartOfPrefabInstance(udonBehaviour))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(udonBehaviour);
                    }

                    dirty = true;
                }
            }

            // ソート
            if (reorderVariables.Count > 0)
            {
                foreach (KeyValuePair<string, object> variable in reorderVariables)
                {
                    if (symbolTable.HasExportedSymbol(variable.Key))
                    {
                        Type symbolType = symbolTable.GetSymbolType(variable.Key);

                        if (!publicVariables.TrySetVariableValue(variable.Key, variable.Value))
                        {
                            if (!publicVariables.TryAddVariable(CreateUdonVariable(variable.Key, variable.Value, symbolType)))
                            {
                                Debug.LogError($"Failed to set public variable '{variable.Key}' value.");
                            }
                        }
                    }
                }
                reorderVariables.Clear();
                dirty = true;
            }
        }

        private void AddPropertyArray(SerializedProperty property)
        {
            property.Next(true);
            property.Next(true);

            int actionsLength = property.intValue;
            property.intValue = actionsLength + 1;
            property.Next(true);

            for (int idx = 0; idx < actionsLength; ++idx)
                property.Next(false);
        }

        private void BeginVerticalBox(Color backgroundColor)
        {
            //GUI.backgroundColor = backgroundColor;
            EditorGUILayout.BeginVertical(new GUIStyle("HelpBox"));
            //GUI.backgroundColor = baseBackground;
        }

        protected void ApplyDefaultValuesToHeap()
        {
            IUdonSymbolTable symbolTable = program?.SymbolTable;
            IUdonHeap heap = program?.Heap;
            if (symbolTable == null || heap == null)
            {
                return;
            }

            foreach (KeyValuePair<string, (object value, Type type)> defaultValue in heapDefaultValues)
            {
                if (!symbolTable.HasAddressForSymbol(defaultValue.Key))
                {
                    continue;
                }

                uint symbolAddress = symbolTable.GetAddressFromSymbol(defaultValue.Key);
                (object value, Type declaredType) = defaultValue.Value;
                if (typeof(UnityEngine.Object).IsAssignableFrom(declaredType))
                {
                    if (value != null && !declaredType.IsInstanceOfType(value))
                    {
                        heap.SetHeapVariable(symbolAddress, null, declaredType);
                        continue;
                    }

                    if ((UnityEngine.Object)value == null)
                    {
                        heap.SetHeapVariable(symbolAddress, null, declaredType);
                        continue;
                    }
                }

                if (value != null)
                {
                    if (!declaredType.IsInstanceOfType(value))
                    {
                        value = declaredType.IsValueType ? Activator.CreateInstance(declaredType) : null;
                    }
                }

                if (declaredType == null)
                {
                    declaredType = typeof(object);
                }
                heap.SetHeapVariable(symbolAddress, value, declaredType);
            }
        }

        protected override object GetPublicVariableDefaultValue(string symbol, Type type)
        {
            IUdonSymbolTable symbolTable = program?.SymbolTable;
            IUdonHeap heap = program?.Heap;
            if (symbolTable == null || heap == null)
            {
                return null;
            }

            if (!heapDefaultValues.ContainsKey(symbol))
            {
                return null;
            }

            (object value, Type declaredType) = heapDefaultValues[symbol];
            if (!typeof(UnityEngine.Object).IsAssignableFrom(declaredType))
            {
                return value;
            }

            return (UnityEngine.Object)value == null ? null : value;
        }

        private static IUdonVariable CreateUdonVariable(string symbolName, object value, Type declaredType)
        {
            Type udonVariableType = typeof(UdonVariable<>).MakeGenericType(declaredType);
            return (IUdonVariable)Activator.CreateInstance(udonVariableType, symbolName, value);
        }
    }
}
#endif
