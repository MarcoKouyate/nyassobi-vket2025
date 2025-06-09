using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VketUdonAssembly
{
    public class TriggerParameters
    {
        public enum TriggerType
        {
            OnInteract, OnBoothEnter, OnBoothExit
        }

        public enum ActionType
        {
            SetGameObjectActive, AnimationFloat, AnimationInt, AnimationBool, AnimationTrigger
        }

        public enum BroadcastType
        {
            Local, All
        }

        public enum BoolOp
        {
            False, True, Toggle
        }

        public enum AnimatorOp
        {
            Set, Add, Subtract, Multiply, Divide
        }

        [System.Serializable]
        public class Trigger
        {
            public TriggerType type;
            public BroadcastType broadcast;
            public Action[] actions;

            public Trigger(TriggerType _type = TriggerType.OnInteract, BroadcastType _broadcast = BroadcastType.Local)
            {
                type = _type;
                broadcast = _broadcast;
                actions = new Action[0];
            }
        }

        [System.Serializable]
        public class Action
        {
            public ActionType type;
            public BoolOp parameterBoolOp;
            public AnimatorOp parameterAnimatorOp;
            public string parameterString;
            public float parameterFloat;
            public int parameterInt;
            public bool foldout;
            public int index;
            public int[] variableIndex;

            public Action(ActionType _type = ActionType.SetGameObjectActive)
            {
                type = _type;
                parameterBoolOp = BoolOp.False;
                parameterAnimatorOp = AnimatorOp.Set;
            }
        }
    }
}