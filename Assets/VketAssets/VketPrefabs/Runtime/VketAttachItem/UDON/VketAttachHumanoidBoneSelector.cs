
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace VketAssets.VketPrefabs.VketAttachItem.Udon
{
    [UdonBehaviourSyncMode( BehaviourSyncMode.None )]
    public class VketAttachHumanoidBoneSelector : UdonSharpBehaviour
    {

        [SerializeField] private UdonBehaviour changeCallback;
        [SerializeField] private string changeCallBackMethod;
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private Button[] buttons;

        [SerializeField] private MeshRenderer reference;

        private int currentSelectBone=0;

        public int CurrentSelectBone { get => currentSelectBone;  }

        private void OnEnable()
        {
            displayText.text=string.Empty;
        }
        public void _SetCurrentSelectBone(int selectBone)
        {
            currentSelectBone = selectBone;
            foreach (var button in buttons) {
                if (button.gameObject.name==selectBone.ToString())
                {
                    button.interactable = false;
                }
                else
                {
                    button.interactable = true;
                }
            }
        }
        public void _ButtonClick()
        {
            _SetCurrentSelectBone((int)ButtonNameToEnum(reference.probeAnchor.name));
            if (changeCallback)
            {
                changeCallback.SendCustomEvent(changeCallBackMethod);
            }
        }
        public void _PointEnterButton()
        {
            displayText.text=ButtonNameToEnum(reference.probeAnchor.name).ToString();    
        }
        private HumanBodyBones ButtonNameToEnum(string name)
        {
            return (HumanBodyBones)int.Parse(name);
        }
    }
}