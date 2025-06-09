
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LongTextScrollDisplayer : UdonSharpBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float scrollSpeed=.1f;
        [SerializeField] private bool nonLoop=false;

        bool nonLoopFlag=false;
        int loopCount;
        bool isScrollingRight = true; // 标志位，用于判断滚动
        float scrollPosition;
        void OnDisable()
        {
            scrollPosition = 0;
            isScrollingRight =true;
            loopCount=0;
            nonLoopFlag=false;
        }
        private void Update()
        {
            if(nonLoopFlag)return;

            // 计算水平滚动的偏移量
            float horizontalOffset = isScrollingRight ? Time.deltaTime * scrollSpeed : -Time.deltaTime * scrollSpeed;

            // 更新scrollRect的水平滚动位置
            scrollPosition+=horizontalOffset;
            scrollRect.horizontalNormalizedPosition = scrollPosition;

            // 检查是否滚动到了边界（0或1），如果是，则反向滚动
            if (scrollPosition < 0 || scrollPosition > 1)
            {
                isScrollingRight = !isScrollingRight;
                loopCount++;
                if(nonLoop)
                {
                    nonLoopFlag=true;
                }
            }
        }
    }
}