using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using VRC.Udon;
using UnityEngine;

namespace VketUdonAssembly
{
    public static class AssemblyBuilder
    {
        public static string GetAssemblyStr(TriggerParameters.Trigger[] triggers, out Dictionary<string, (object value, Type type)> defaultValues)
        {
            
            StringBuilder assemblyTextBuilder = new StringBuilder();
            Dictionary<string, (object value, Type type)> values = new Dictionary<string, (object value, Type type)>();

            List<ActionBuilder> buildersList = new List<ActionBuilder>();
            List<string> publicVariablesName = new List<string>();

            // Create ActionBuilders List
            int triggerCount = 0;
            foreach (TriggerParameters.Trigger trigger in triggers)
            {
                if (trigger.broadcast == TriggerParameters.BroadcastType.All)
                {
                    buildersList.Add(new SendNetworkEvent(triggerCount, trigger.type));
                }

                int actionCount = 0;
                foreach (TriggerParameters.Action action in trigger.actions)
                {
                    switch (action.type)
                    {
                        case TriggerParameters.ActionType.SetGameObjectActive:
                            buildersList.Add(new SetGameObjectActive(triggerCount, trigger.type, actionCount, action.parameterBoolOp));
                            break;
                        case TriggerParameters.ActionType.AnimationFloat:
                            buildersList.Add(new AnimationFloat(triggerCount, trigger.type, actionCount, action.parameterAnimatorOp, action.parameterFloat , action.parameterString));
                            break;
                        case TriggerParameters.ActionType.AnimationInt:
                            buildersList.Add(new AnimationInt(triggerCount, trigger.type, actionCount, action.parameterAnimatorOp, action.parameterInt, action.parameterString));
                            break;
                        case TriggerParameters.ActionType.AnimationBool:
                            buildersList.Add(new AnimationBool(triggerCount, trigger.type, actionCount, action.parameterBoolOp, action.parameterString));
                            break;
                        case TriggerParameters.ActionType.AnimationTrigger:
                            buildersList.Add(new AnimationTrigger(triggerCount, trigger.type, actionCount, action.parameterString));
                            break;
                    }

                    publicVariablesName.Add($"objectArray_{triggerCount}_{actionCount}");
                    actionCount++;
                }
                triggerCount++;
            }

            //Construct Data Block and Get Default Values
            assemblyTextBuilder.Append(".data_start\n\n");

            foreach (var publicVariable in publicVariablesName)
                assemblyTextBuilder.Append($"{new string(' ', 4)}.export {publicVariable}\n");

            foreach (var actionBuilder in buildersList)
            {
                var valueDic = actionBuilder.GetValues();

                foreach (KeyValuePair<string, (object value, Type type)> item in valueDic)
                {
                    if (values.ContainsKey(item.Key))
                        continue;

                    string typeStr = "";
                    if (item.Value.type == typeof(float))
                        typeStr = "SystemSingle, null";
                    if (item.Value.type == typeof(int))
                        typeStr = "SystemInt32, null";
                    else if (item.Value.type == typeof(bool))
                        typeStr = "SystemBoolean, null";
                    else if (item.Value.type == typeof(string))
                        typeStr = "SystemString, null";
                    else if (item.Value.type == typeof(GameObject))
                        typeStr = "UnityEngineGameObject, null";
                    else if (item.Value.type == typeof(GameObject[]))
                        typeStr = "UnityEngineGameObjectArray, null";
                    else if (item.Value.type == typeof(Animator))
                        typeStr = "UnityEngineAnimator, null";
                    else if (item.Value.type == typeof(Animator[]))
                        typeStr = "UnityEngineAnimatorArray, null";
                    else if (item.Value.type == typeof(UdonBehaviour))
                        typeStr = "VRCUdonUdonBehaviour, this";
                    else if (item.Value.type == typeof(VRC.Udon.Common.Interfaces.NetworkEventTarget))
                        typeStr = "VRCUdonCommonInterfacesNetworkEventTarget, null";

                    assemblyTextBuilder.Append($"{new string(' ', 4)}{item.Key}: %{typeStr}\n");
                }
                values.Marge(valueDic);
            }

            List<string> removeKeyList = new List<string>();
            foreach (KeyValuePair<string, (object value, Type type)> item in values)
            {
                if (!(item.Value.type.IsValueType || item.Value.type == typeof(string)))
                    removeKeyList.Add(item.Key);
            }
            foreach (var target in removeKeyList)
                values.Remove(target);

            defaultValues = values;

            assemblyTextBuilder.Append("\n.data_end\n\n");


            //Construct Code Block
            assemblyTextBuilder.Append(".code_start\n\n");

            float currentLine = 0;

            // Network Actions
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i].broadcast == TriggerParameters.BroadcastType.All)
                {
                    bool hasCode = false;
                    foreach (var builder in buildersList)
                    {
                        if (builder.triggerCount == i && builder.GetType() != typeof(SendNetworkEvent))
                        {
                            if (!hasCode)
                            {
                                // Build Function
                                string str = $"SendAction_{i}";
                                assemblyTextBuilder.Append($"{new string(' ', 4)}.export {str}\n\n");
                                assemblyTextBuilder.Append($"{new string(' ', 4)}{str}:\n\n");
                                hasCode = true;
                            }

                            // Build Code
                            currentLine = builder.BuildCode(assemblyTextBuilder, currentLine);
                        }
                    }
                    if (hasCode)
                    {
                        assemblyTextBuilder.Append($"{new string(' ', 8)}JUMP, 0xFFFFFFFC\n\n");
                        currentLine += 1.0f;
                    }
                }
            }
            // Local Actions
            foreach(var typeName in Enum.GetNames(typeof(TriggerParameters.TriggerType)))
            {
                bool hasCode = false;
                foreach(var builder in buildersList)
                {
                    if (Enum.GetName(typeof(TriggerParameters.TriggerType), builder.triggerType) == typeName)
                    {
                        if (triggers[builder.triggerCount].broadcast == TriggerParameters.BroadcastType.Local || builder.GetType() == typeof(SendNetworkEvent))
                        {
                            if (!hasCode)
                            {
                                // Build Function
                                string str = "";
                                switch (typeName)
                                {
                                    case nameof(TriggerParameters.TriggerType.OnInteract):
                                        str = "_interact";
                                        //str = "_start";
                                        break;
                                    case nameof(TriggerParameters.TriggerType.OnBoothEnter):
                                        str = "_VketOnBoothEnter";
                                        break;
                                    case nameof(TriggerParameters.TriggerType.OnBoothExit):
                                        str = "_VketOnBoothExit";
                                        break;
                                }
                                assemblyTextBuilder.Append($"{new string(' ', 4)}.export {str}\n\n");
                                assemblyTextBuilder.Append($"{new string(' ', 4)}{str}:\n\n");
                                hasCode = true;
                            }

                            // Build Code
                            currentLine = builder.BuildCode(assemblyTextBuilder, currentLine);
                        }
                    }
                }
                if (hasCode)
                {
                    assemblyTextBuilder.Append($"{new string(' ', 8)}JUMP, 0xFFFFFFFC\n\n");
                    currentLine += 1.0f;
                }
            }

            assemblyTextBuilder.Append("\n\n.code_end");
            
            return assemblyTextBuilder.ToString();
        }
    }

    public abstract class ActionBuilder
    {
        public TriggerParameters.TriggerType triggerType;
        public int triggerCount;

        protected int actionCount;
        protected StringBuilder stringBuilder;
        protected float initLine;
        protected float lineSum;

        public abstract float BuildCode(StringBuilder sb, float lineCount);

        public abstract Dictionary<string, (object value, Type type)> GetValues();

        #region Create Code Line Method
        protected void AppendLine(string line)
        {
            stringBuilder.Append($"{new string(' ', 8)}{line}\n");
        }
        protected void AddNop()
        {
            AppendLine("NOP");
            lineSum += 0.5f;
        }
        protected void AddPush(string value)
        {
            AppendLine($"PUSH, {value}");
            lineSum += 1.0f;
        }
        protected void AddPop()
        {
            AppendLine("POP");
            lineSum += 0.5f;
        }
        protected void AddJumpIfFalse(float jumpLine)
        {
            AppendLine($"JUMP_IF_FALSE, {convertBase(initLine + jumpLine)}");
            lineSum += 1.0f;
        }
        protected void AddJump(float jumpLine)
        {
            AppendLine($"JUMP, {convertBase(initLine + jumpLine)}");
            lineSum += 1.0f;
        }
        protected void AddExtern(string methodStr)
        {
            AppendLine($"EXTERN, \"{methodStr}\"");
            lineSum += 1.0f;
        }
        protected void AddAnnotation()
        {
        }
        protected void AddJumpIndirect(string value)
        {
            AppendLine($"JUMP_INDIRECT, {value}");
            lineSum += 1.0f;
        }
        protected void AddCopy()
        {
            AppendLine("COPY");
            lineSum += 0.5f;
        }
        private string convertBase(float dec)
        {
            dec *= 8;
            return $"0x{String.Format("{0:X8}", (int)dec)}";
        }

        protected void AddJumpFromAddress(string address)
        {
            AppendLine($"JUMP, {address}");
        }
        
        #endregion
    }

    public class SetGameObjectActive : ActionBuilder
    {
        private TriggerParameters.BoolOp boolOp;

        public SetGameObjectActive(int _triggerCount, TriggerParameters.TriggerType _triggerType, int _actionCount, TriggerParameters.BoolOp _boolOp)
        {
            triggerCount = _triggerCount;
            triggerType = _triggerType;
            actionCount = _actionCount;
            boolOp = _boolOp;
        }

        // Build Code
        public override float BuildCode(StringBuilder sb, float lineCount)
        {
            stringBuilder = sb;
            initLine = lineCount;
            lineSum = 0;

            string boolOpString = "";
            float[] jumpLine = new float[2];
            switch(boolOp)
            {
                case TriggerParameters.BoolOp.False:
                    boolOpString = "const_bool_false";
                    jumpLine[0] = 29.5f;
                    jumpLine[1] = 24.5f;
                    break;
                case TriggerParameters.BoolOp.True:
                    boolOpString = "const_bool_true";
                    jumpLine[0] = 29.5f;
                    jumpLine[1] = 24.5f;
                    break;
                case TriggerParameters.BoolOp.Toggle:
                    jumpLine[0] = 38.5f;
                    jumpLine[1] = 33.5f;
                    break;
                default:
                    break;
            }

            AddPush("const_int_0");
            AddPush("int_i");
            AddCopy();
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_0");
            AddExtern("UnityEngineGameObjectArray.__get_Length__SystemInt32");
            AddPush("int_i");
            AddPush("int_0");
            AddPush("bool_0");
            AddExtern("SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[0]);
            // ------ ここからループ内処理 ------
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_i");
            AddPush("GameObject_0");
            AddExtern("SystemObjectArray.__Get__SystemInt32__SystemObject");
            AddPush("GameObject_0");
            AddPush("const_GameObject_null");
            AddPush("bool_0");
            AddExtern("UnityEngineObject.__op_Inequality__UnityEngineObject_UnityEngineObject__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[1]);
            if (boolOp == TriggerParameters.BoolOp.False || boolOp == TriggerParameters.BoolOp.True)
            {
                AddPush("GameObject_0");
                AddPush(boolOpString);
                AddExtern("UnityEngineGameObject.__SetActive__SystemBoolean__SystemVoid");
            }
            else
            {
                AddPush($"GameObject_0");
                AddPush($"bool_0");
                AddExtern("UnityEngineGameObject.__get_activeSelf__SystemBoolean");
                AddPush($"bool_0");
                AddJumpIfFalse(30.5f);
                AddPush($"GameObject_0");
                AddPush($"const_bool_false");
                AddExtern("UnityEngineGameObject.__SetActive__SystemBoolean__SystemVoid");
                AddJump(33.5f);
                AddPush($"GameObject_0");
                AddPush($"const_bool_true");
                AddExtern("UnityEngineGameObject.__SetActive__SystemBoolean__SystemVoid");
            }
            AddPush("int_i");
            AddPush("const_int_1");
            AddPush("int_i");
            AddExtern("SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32");
            AddJump(2.5f);
            
            return initLine + lineSum;
        }

        // Define Variables
        public override Dictionary<string, (object, Type)> GetValues()
        {
            Dictionary<string, (object value, Type type)> valuesDic = new Dictionary<string, (object value, Type type)>();

            valuesDic.Add("const_int_0", (0, typeof(int)));
            valuesDic.Add("const_int_1", (1, typeof(int)));
            valuesDic.Add("const_GameObject_null", (null, typeof(GameObject)));
            valuesDic.Add("int_i", (null, typeof(int)));
            valuesDic.Add("int_0", (null, typeof(int)));
            valuesDic.Add("bool_0", (null, typeof(bool)));
            valuesDic.Add("GameObject_0", (null, typeof(GameObject)));

            switch (boolOp)
            {
                case TriggerParameters.BoolOp.False:
                    valuesDic.Add("const_bool_false", (false, typeof(bool)));
                    break;
                case TriggerParameters.BoolOp.True:
                    valuesDic.Add("const_bool_true", (true, typeof(bool)));
                    break;
                case TriggerParameters.BoolOp.Toggle:
                    valuesDic.Add("const_bool_false", (false, typeof(bool)));
                    valuesDic.Add("const_bool_true", (true, typeof(bool)));
                    break;
            }

            valuesDic.Add($"objectArray_{triggerCount}_{actionCount}", (null, typeof(GameObject[])));

            return valuesDic;
        }
    }

    public class AnimationFloat : ActionBuilder
    {
        private TriggerParameters.AnimatorOp animatorOp;
        private float parameterFloat;
        private string parameterString;

        public AnimationFloat(int _triggerCount, TriggerParameters.TriggerType _triggerType, int _actionCount,TriggerParameters.AnimatorOp _animatorOp,float _parameterFloat, string _parameterString)
        {
            triggerCount = _triggerCount;
            triggerType = _triggerType;
            actionCount = _actionCount;
            animatorOp = _animatorOp;
            parameterFloat = _parameterFloat;
            parameterString = _parameterString;
        }

        // Build Code
        public override float BuildCode(StringBuilder sb, float lineCount)
        {
            stringBuilder = sb;
            initLine = lineCount;
            lineSum = 0;

            float[] jumpLine = new float[2];
            if (animatorOp == TriggerParameters.AnimatorOp.Set)
            {
                jumpLine[0] = 30.5f;
                jumpLine[1] = 25.5f;
            }
            else
            {
                jumpLine[0] = 38.5f;
                jumpLine[1] = 33.5f;
            }

            AddPush("const_int_0");
            AddPush("int_i");
            AddCopy();
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_0");
            AddExtern("UnityEngineAnimatorArray.__get_Length__SystemInt32");
            AddPush("int_i");
            AddPush("int_0");
            AddPush("bool_0");
            AddExtern("SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[0]);
            // ------ ここからループ内処理 ------
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_i");
            AddPush("Animator_0");
            AddExtern("SystemObjectArray.__Get__SystemInt32__SystemObject");
            AddPush("Animator_0");
            AddPush("const_Animator_null");
            AddPush("bool_0");
            AddExtern("UnityEngineObject.__op_Inequality__UnityEngineObject_UnityEngineObject__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[1]);
            if (animatorOp == TriggerParameters.AnimatorOp.Set) {
                AddPush("Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush($"param_float_{triggerCount}_{actionCount}");
                AddExtern("UnityEngineAnimator.__SetFloat__SystemString_SystemSingle__SystemVoid");
            }
            else
            {
                AddPush("Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush("float_0");
                AddExtern("UnityEngineAnimator.__GetFloat__SystemString__SystemSingle");
                AddPush("float_0");
                AddPush($"param_float_{triggerCount}_{actionCount}");
                AddPush("float_0");
                switch(animatorOp)
                {
                    case TriggerParameters.AnimatorOp.Add:
                        AddExtern("SystemSingle.__op_Addition__SystemSingle_SystemSingle__SystemSingle");
                        break;
                    case TriggerParameters.AnimatorOp.Subtract:
                        AddExtern("SystemSingle.__op_Subtraction__SystemSingle_SystemSingle__SystemSingle");
                        break;
                    case TriggerParameters.AnimatorOp.Multiply:
                        AddExtern("SystemSingle.__op_Multiplication__SystemSingle_SystemSingle__SystemSingle");
                        break;
                    case TriggerParameters.AnimatorOp.Divide:
                        AddExtern("SystemSingle.__op_Division__SystemSingle_SystemSingle__SystemSingle");
                        break;
                }
                AddPush("Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush("float_0");
                AddExtern("UnityEngineAnimator.__SetFloat__SystemString_SystemSingle__SystemVoid");
            }
            AddPush("int_i");
            AddPush("const_int_1");
            AddPush("int_i");
            AddExtern("SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32");
            AddJump(2.5f);

            return initLine + lineSum;
        }

        // Define Variables
        public override Dictionary<string, (object, Type)> GetValues()
        {
            Dictionary<string, (object value, Type type)> valuesDic = new Dictionary<string, (object value, Type type)>();

            valuesDic.Add("const_int_0", (0, typeof(int)));
            valuesDic.Add("const_int_1", (1, typeof(int)));
            valuesDic.Add("const_Animator_null", (null, typeof(Animator)));
            valuesDic.Add("int_i", (null, typeof(int)));
            valuesDic.Add("int_0", (null, typeof(int)));
            valuesDic.Add("bool_0", (null, typeof(bool)));
            valuesDic.Add("Animator_0", (null, typeof(Animator)));

            if (animatorOp != TriggerParameters.AnimatorOp.Set)
                valuesDic.Add("float_0", (null, typeof(float)));

            valuesDic.Add($"param_float_{triggerCount}_{actionCount}", (parameterFloat, typeof(float)));
            valuesDic.Add($"param_string_{triggerCount}_{actionCount}", (parameterString, typeof(string)));
            valuesDic.Add($"objectArray_{triggerCount}_{actionCount}", (null, typeof(Animator[])));

            return valuesDic;
        }
    }

    public class AnimationInt : ActionBuilder
    {
        private TriggerParameters.AnimatorOp animatorOp;
        private int parameterInt;
        private string parameterString;

        public AnimationInt(int _triggerCount, TriggerParameters.TriggerType _triggerType, int _actionCount, TriggerParameters.AnimatorOp _animatorOp, int _parameterInt, string _parameterString)
        {
            triggerCount = _triggerCount;
            triggerType = _triggerType;
            actionCount = _actionCount;
            animatorOp = _animatorOp;
            parameterInt = _parameterInt;
            parameterString = _parameterString;
        }

        // Build Code
        public override float BuildCode(StringBuilder sb, float lineCount)
        {
            stringBuilder = sb;
            initLine = lineCount;
            lineSum = 0;

            float[] jumpLine = new float[2];
            if (animatorOp == TriggerParameters.AnimatorOp.Set)
            {
                jumpLine[0] = 30.5f;
                jumpLine[1] = 25.5f;
            }
            else
            {
                jumpLine[0] = 38.5f;
                jumpLine[1] = 33.5f;
            }

            AddPush("const_int_0");
            AddPush("int_i");
            AddCopy();
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_0");
            AddExtern("UnityEngineAnimatorArray.__get_Length__SystemInt32");
            AddPush("int_i");
            AddPush("int_0");
            AddPush("bool_0");
            AddExtern("SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[0]);
            // ------ ここからループ内処理 ------
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_i");
            AddPush("Animator_0");
            AddExtern("SystemObjectArray.__Get__SystemInt32__SystemObject");
            AddPush("Animator_0");
            AddPush("const_Animator_null");
            AddPush("bool_0");
            AddExtern("UnityEngineObject.__op_Inequality__UnityEngineObject_UnityEngineObject__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[1]);
            if (animatorOp == TriggerParameters.AnimatorOp.Set)
            {
                AddPush("Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush($"param_int_{triggerCount}_{actionCount}");
                AddExtern("UnityEngineAnimator.__SetInteger__SystemString_SystemInt32__SystemVoid");
            }
            else
            {
                AddPush("Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush("int_0");
                AddExtern("UnityEngineAnimator.__GetInteger__SystemString__SystemInt32");
                AddPush("int_0");
                AddPush($"param_int_{triggerCount}_{actionCount}");
                AddPush("int_0");
                switch (animatorOp)
                {
                    case TriggerParameters.AnimatorOp.Add:
                        AddExtern("SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32");
                        break;
                    case TriggerParameters.AnimatorOp.Subtract:
                        AddExtern("SystemInt32.__op_Subtraction__SystemInt32_SystemInt32__SystemInt32");
                        break;
                    case TriggerParameters.AnimatorOp.Multiply:
                        AddExtern("SystemInt32.__op_Multiplication__SystemInt32_SystemInt32__SystemInt32");
                        break;
                    case TriggerParameters.AnimatorOp.Divide:
                        AddExtern("SystemInt32.__op_Division__SystemInt32_SystemInt32__SystemInt32");
                        break;
                }
                AddPush("Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush("int_0");
                AddExtern("UnityEngineAnimator.__SetInteger__SystemString_SystemInt32__SystemVoid");
            }
            AddPush("int_i");
            AddPush("const_int_1");
            AddPush("int_i");
            AddExtern("SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32");
            AddJump(2.5f);

            return initLine + lineSum;
        }

        // Define Variables
        public override Dictionary<string, (object, Type)> GetValues()
        {
            Dictionary<string, (object value, Type type)> valuesDic = new Dictionary<string, (object value, Type type)>();

            valuesDic.Add("const_int_0", (0, typeof(int)));
            valuesDic.Add("const_int_1", (1, typeof(int)));
            valuesDic.Add("const_Animator_null", (null, typeof(Animator)));
            valuesDic.Add("int_i", (null, typeof(int)));
            valuesDic.Add("int_0", (null, typeof(int)));
            valuesDic.Add("bool_0", (null, typeof(bool)));
            valuesDic.Add("Animator_0", (null, typeof(Animator)));

            valuesDic.Add($"param_int_{triggerCount}_{actionCount}", (parameterInt, typeof(int)));
            valuesDic.Add($"param_string_{triggerCount}_{actionCount}", (parameterString, typeof(string)));
            valuesDic.Add($"objectArray_{triggerCount}_{actionCount}", (null, typeof(Animator[])));

            return valuesDic;
        }
    }

    public class AnimationBool : ActionBuilder
    {
        private TriggerParameters.BoolOp boolOp;
        private string parameterString;

        public AnimationBool(int _triggerCount, TriggerParameters.TriggerType _triggerType, int _actionCount, TriggerParameters.BoolOp _boolOp, string _parameterString)
        {
            triggerCount = _triggerCount;
            triggerType = _triggerType;
            actionCount = _actionCount;
            boolOp = _boolOp;
            parameterString = _parameterString;
        }

        // Build Code
        public override float BuildCode(StringBuilder sb, float lineCount)
        {
            stringBuilder = sb;
            initLine = lineCount;
            lineSum = 0;

            string boolOpString = "";
            float[] jumpLine = new float[2];
            switch (boolOp)
            {
                case TriggerParameters.BoolOp.False:
                    boolOpString = "const_bool_false";
                    jumpLine[0] = 30.5f;
                    jumpLine[1] = 25.5f;
                    break;
                case TriggerParameters.BoolOp.True:
                    boolOpString = "const_bool_true";
                    jumpLine[0] = 30.5f;
                    jumpLine[1] = 25.5f;
                    break;
                case TriggerParameters.BoolOp.Toggle:
                    jumpLine[0] = 41.5f;
                    jumpLine[1] = 36.5f;
                    break;
                default:
                    break;
            }

            AddPush("const_int_0");
            AddPush("int_i");
            AddCopy();
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_0");
            AddExtern("UnityEngineAnimatorArray.__get_Length__SystemInt32");
            AddPush("int_i");
            AddPush("int_0");
            AddPush("bool_0");
            AddExtern("SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[0]);
            // ------ ここからループ内処理 ------
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_i");
            AddPush("Animator_0");
            AddExtern("SystemObjectArray.__Get__SystemInt32__SystemObject");
            AddPush("Animator_0");
            AddPush("const_Animator_null");
            AddPush("bool_0");
            AddExtern("UnityEngineObject.__op_Inequality__UnityEngineObject_UnityEngineObject__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(jumpLine[1]);
            if (boolOp == TriggerParameters.BoolOp.False || boolOp == TriggerParameters.BoolOp.True)
            {
                AddPush("Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush(boolOpString);
                AddExtern("UnityEngineAnimator.__SetBool__SystemString_SystemBoolean__SystemVoid");
            }
            else
            {
                AddPush($"Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush("bool_0");
                AddExtern("UnityEngineAnimator.__GetBool__SystemString__SystemBoolean");
                AddPush($"bool_0");
                AddJumpIfFalse(32.5f);
                AddPush($"Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush($"const_bool_false");
                AddExtern("UnityEngineAnimator.__SetBool__SystemString_SystemBoolean__SystemVoid");
                AddJump(36.5f);
                AddPush($"Animator_0");
                AddPush($"param_string_{triggerCount}_{actionCount}");
                AddPush($"const_bool_true");
                AddExtern("UnityEngineAnimator.__SetBool__SystemString_SystemBoolean__SystemVoid");
            }
            AddPush("int_i");
            AddPush("const_int_1");
            AddPush("int_i");
            AddExtern("SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32");
            AddJump(2.5f);

            return initLine + lineSum;
        }

        // Define Variables
        public override Dictionary<string, (object, Type)> GetValues()
        {
            Dictionary<string, (object value, Type type)> valuesDic = new Dictionary<string, (object value, Type type)>();

            valuesDic.Add("const_int_0", (0, typeof(int)));
            valuesDic.Add("const_int_1", (1, typeof(int)));
            valuesDic.Add("const_Animator_null", (null, typeof(Animator)));
            valuesDic.Add("int_i", (null, typeof(int)));
            valuesDic.Add("int_0", (null, typeof(int)));
            valuesDic.Add("bool_0", (null, typeof(bool)));
            valuesDic.Add("Animator_0", (null, typeof(Animator)));

            switch (boolOp)
            {
                case TriggerParameters.BoolOp.False:
                    valuesDic.Add("const_bool_false", (false, typeof(bool)));
                    break;
                case TriggerParameters.BoolOp.True:
                    valuesDic.Add("const_bool_true", (true, typeof(bool)));
                    break;
                case TriggerParameters.BoolOp.Toggle:
                    valuesDic.Add("const_bool_false", (false, typeof(bool)));
                    valuesDic.Add("const_bool_true", (true, typeof(bool)));
                    break;
            }

            valuesDic.Add($"param_string_{triggerCount}_{actionCount}", (parameterString, typeof(string)));
            valuesDic.Add($"objectArray_{triggerCount}_{actionCount}", (null, typeof(Animator[])));

            return valuesDic;
        }
    }

    public class AnimationTrigger : ActionBuilder
    {
        private string parameterString;

        public AnimationTrigger(int _triggerCount, TriggerParameters.TriggerType _triggerType, int _actionCount, string _parameterString)
        {
            triggerCount = _triggerCount;
            triggerType = _triggerType;
            actionCount = _actionCount;
            parameterString = _parameterString;
        }

        // Build Code
        public override float BuildCode(StringBuilder sb, float lineCount)
        {
            stringBuilder = sb;
            initLine = lineCount;
            lineSum = 0;

            AddPush("const_int_0");
            AddPush("int_i");
            AddCopy();
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_0");
            AddExtern("UnityEngineAnimatorArray.__get_Length__SystemInt32");
            AddPush("int_i");
            AddPush("int_0");
            AddPush("bool_0");
            AddExtern("SystemInt32.__op_LessThan__SystemInt32_SystemInt32__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(29.5f);
            // ------ ここからループ内処理 ------
            AddPush($"objectArray_{triggerCount}_{actionCount}");
            AddPush("int_i");
            AddPush("Animator_0");
            AddExtern("SystemObjectArray.__Get__SystemInt32__SystemObject");
            AddPush("Animator_0");
            AddPush("const_Animator_null");
            AddPush("bool_0");
            AddExtern("UnityEngineObject.__op_Inequality__UnityEngineObject_UnityEngineObject__SystemBoolean");
            AddPush("bool_0");
            AddJumpIfFalse(24.5f);
            AddPush("Animator_0");
            AddPush($"param_string_{triggerCount}_{actionCount}");
            AddExtern("UnityEngineAnimator.__SetTrigger__SystemString__SystemVoid");
            AddPush("int_i");
            AddPush("const_int_1");
            AddPush("int_i");
            AddExtern("SystemInt32.__op_Addition__SystemInt32_SystemInt32__SystemInt32");
            AddJump(2.5f);

            return initLine + lineSum;
        }

        // Define Variables
        public override Dictionary<string, (object, Type)> GetValues()
        {
            Dictionary<string, (object value, Type type)> valuesDic = new Dictionary<string, (object value, Type type)>();

            valuesDic.Add("const_int_0", (0, typeof(int)));
            valuesDic.Add("const_int_1", (1, typeof(int)));
            valuesDic.Add("const_Animator_null", (null, typeof(Animator)));
            valuesDic.Add("int_i", (null, typeof(int)));
            valuesDic.Add("int_0", (null, typeof(int)));
            valuesDic.Add("bool_0", (null, typeof(bool)));
            valuesDic.Add("Animator_0", (null, typeof(Animator)));

            valuesDic.Add($"param_string_{triggerCount}_{actionCount}", (parameterString, typeof(string)));
            valuesDic.Add($"objectArray_{triggerCount}_{actionCount}", (null, typeof(Animator[])));

            return valuesDic;
        }
    }

    public class SendNetworkEvent : ActionBuilder
    {

        public SendNetworkEvent(int _triggerCount, TriggerParameters.TriggerType _triggerType)
        {
            triggerCount = _triggerCount;
            triggerType = _triggerType;
        }

        // Build Code
        public override float BuildCode(StringBuilder sb, float lineCount)
        {
            stringBuilder = sb;
            initLine = lineCount;
            lineSum = 0;
            
            AddPush("this_UdonBehaviour");
            AddPush("const_VRCUdonCommonInterfacesNetworkEventTarget_All");
            AddPush($"const_string_SendAction_{triggerCount}");
            AddExtern("VRCUdonCommonInterfacesIUdonEventReceiver.__SendCustomNetworkEvent__VRCUdonCommonInterfacesNetworkEventTarget_SystemString__SystemVoid");
            
            return initLine + lineSum;
        }

        // Define Variables
        public override Dictionary<string, (object, Type)> GetValues()
        {
            Dictionary<string, (object value, Type type)> valuesDic = new Dictionary<string, (object value, Type type)>();

            valuesDic.Add($"const_string_SendAction_{triggerCount}", ($"SendAction_{triggerCount}", typeof(string)));
            valuesDic.Add("this_UdonBehaviour", (null, typeof(UdonBehaviour)));
            valuesDic.Add("const_VRCUdonCommonInterfacesNetworkEventTarget_All", (VRC.Udon.Common.Interfaces.NetworkEventTarget.All, typeof(VRC.Udon.Common.Interfaces.NetworkEventTarget)));

            return valuesDic;
        }
    }
}
