using UdonSharp;
using UnityEngine;
using Vket.EssentialResources.Attribute;
using VRC.Economy;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketGroupPageOpener : UdonSharpBehaviour
    {
        [SerializeField] private string _groupId;
        [Tooltip("If true, will open the store page for the group instead of the group info page.")]
        [SerializeField] private bool _openToStorePage;
        [SerializeField] [SelfComponent] private Animator _animator;
        
        public string GroupId => _groupId;
        
        private bool _hasOpen;

        private void OpenGroup()
        {
            if (string.IsNullOrEmpty(_groupId))
            {
                Debug.LogError("You need to set a group id in the inspector for OpenGroupPage to work.");
                return;
            }

            if (_openToStorePage)
                Store.OpenGroupStorePage(_groupId);
            else
                Store.OpenGroupPage(_groupId);
        }

        public override void Interact()
        {
            _animator.enabled = true;
            if (!_hasOpen)
            {
                _animator.SetBool("Open", true);
                _hasOpen = true;
            }
            else
            {
                _animator.SetTrigger("Trigger");
                OpenGroup();
                _CloseWindow();
            }
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