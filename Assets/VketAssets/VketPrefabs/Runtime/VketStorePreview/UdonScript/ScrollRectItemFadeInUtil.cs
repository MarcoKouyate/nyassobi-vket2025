
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScrollRectItemFadeInUtil : UdonSharpBehaviour
    {
        [SerializeField] private float nextTime=0.3f;
        [SerializeField] private Animator animator;

        private void Start()
        {
            animator.Update(0);
            animator.speed = 0;
            SendCustomEventDelayedSeconds(nameof(_OpenAnimatorSpeed), nextTime*(transform.GetSiblingIndex()-1));
        }
        public void _OpenAnimatorSpeed()
        {
            animator.speed = 1;
        }
    }
}