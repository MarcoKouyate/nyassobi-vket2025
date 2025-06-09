using System;
using UnityEngine;

namespace Vket.EssentialResources.VketSaveData.Attribute
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SaveAttribute : PropertyAttribute
    {
        private bool mulitInstance;
        /// <summary>
        /// 保存一个值
        /// </summary>
        /// <param name="mulitInstance">
        /// 如果为true，则保存多个实例的值.
        /// 注意!可能存在泄露的可能(如果保存的实例脚本在之后移除/改变路径的话)
        /// </param>
        public SaveAttribute(bool mulitInstance = false)
        {
            this.mulitInstance = mulitInstance;
        }

        public bool MulitInstance { get => mulitInstance;  }
    }
}