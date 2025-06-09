using System;
using System.Text.RegularExpressions;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketURLOpener : UdonSharpBehaviour
    {
        [SerializeField]
        private string[] allowURI =
        {
            "https://booth.pm/",
            "https://gumroad.com/",
            "https://jinxxy.com/",
            "https://payhip.com/",
        };

        [SerializeField]
        private string[] allowURIRegex =
        {
            "https://.*.booth.pm/items/[0-9]*",
            "https://.*.gumroad.com/*",
        };

        [SerializeField] private string _inputURL;
        [SerializeField] private CapsuleCollider _capsuleCollider;
        [SerializeField] private Text _reqText, _openOKText;
        [SerializeField] private Animator _animator;
        [SerializeField] private bool _autoAdjustPosition = true;
        [SerializeField] private Transform _popupTransform;
        [SerializeField] private int _type; // 0:2D, 1:3D
        [SerializeField, HideInInspector] private VRCCustomAction _launcher;

        private bool _hasOpen;

        public VRCCustomAction Launcher { get => _launcher; set => _launcher = value; }

        public string[] AllowUri => allowURI;

        public override void Interact()
        {
            _animator.enabled = true;
            if (!_hasOpen)
            {
                _animator.SetBool("Open", true);
                _reqText.text = "Open URL:" + _inputURL;
                _hasOpen = true;
                if (_capsuleCollider != null)
                {
                    if (_autoAdjustPosition)
                    {
                        float headHeight = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position.y;
                        Vector3 worldScale = transform.lossyScale;
                        Vector3 centerPos = transform.position + Vector3.Scale(_capsuleCollider.center, worldScale);
                        Vector3 popupCenter = _capsuleCollider.ClosestPoint(new Vector3(centerPos.x, headHeight, centerPos.z));
                        _popupTransform.position = popupCenter;
                    }
                }
            }
            else
            {
                _animator.SetTrigger("Trigger");
                if (CheckURL(_inputURL))
                {
                    if (_launcher != null)
                    {
                        _openOKText.text = "Web Page Opened!";
                        _launcher.Execute("0");
                    }
                }
                else
                {
                    _openOKText.text = "許可されていないURLのため\nWebページを開くことが出来ませんでした";
                }

                _CloseWindow();
            }
        }

        public bool CheckURL(string checkURL)
        {
            //遍历允许的URI
            foreach (var uri in allowURI)
            {
                if (checkURL.StartsWith(uri))
                {
                    return true;
                }
            }
            foreach (var uriRegex in allowURIRegex)
            {
                if (Regex.IsMatch(checkURL, uriRegex))
                {
                    return true;
                }
            }

            return false;
        }

        public void _CloseWindow()
        {
            _animator.SetBool("Open", false);
            _hasOpen = false;
        }

        public void _OnWindowClosed()
        {
            _animator.Rebind();
            _animator.enabled = false;
        }
        private void OnDisable()
        {
            _hasOpen = false;
            _OnWindowClosed();
        }
    }
}

