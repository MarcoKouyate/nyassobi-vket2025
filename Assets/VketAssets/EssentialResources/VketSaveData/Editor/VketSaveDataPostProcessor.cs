#if UNITY_EDITOR    
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vket.EssentialResources.VketSaveData.Attribute;
using Vket.EssentialResources.VketSaveData.Interface;
namespace Vket.EssentialResources.VketSaveData.Editor
{
    public class VketSaveDataPostProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            //查找VketSaveData.使用GetComponentInChildren.不按照名字查找
            VketSaveData vketSaveData = null;
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                vketSaveData = rootGameObject.GetComponentInChildren<VketSaveData>(true);
                if (vketSaveData != null) break;
            }
            if (vketSaveData != null)
            {
                //获取场景中所有的UdonSharpBehaviour
                var udonSharpBehaviours = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<UdonSharpBehaviour>(true));
                //获取有定义字段SaveAttribute的UdonSharpBehaviour.
                var saveFields = new List<(UdonSharpBehaviour beh, string path, string fieldType)>();
                foreach (var udonSharpBehaviour in udonSharpBehaviours)
                {
                    var fields = udonSharpBehaviour.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        var attr=field.GetCustomAttributes(typeof(SaveAttribute), false);
                        if (attr.Length > 0)
                        {
                            if(!field.IsPublic)
                            {
                                Debug.LogError($"<color=red>[VketSaveDataPostProcessor]</color>Field {udonSharpBehaviour.GetType().FullName}.{field.Name} is not public",udonSharpBehaviour);
                                continue;
                            }
                            SaveAttribute saveAttribute = (SaveAttribute)attr[0];
                            if(saveAttribute.MulitInstance)
                            {
                                var udonsharpBehavirouGameObjectFullPath = SearchUtils.GetHierarchyPath(udonSharpBehaviour.gameObject);
                                var patchHash = udonsharpBehavirouGameObjectFullPath.GetHashCode();
                                saveFields.Add((udonSharpBehaviour, $"*[{patchHash}]" +udonSharpBehaviour.GetType()+ "." + field.Name, field.FieldType.Name));
                            }
                            else
                            {
                                saveFields.Add((udonSharpBehaviour,udonSharpBehaviour.GetType()+"."+field.Name, field.FieldType.Name));
                            }
                        }
                    }
                }
                //Find _IVketSaveDataCallback
                var IVketSaveDataCallback = udonSharpBehaviours.Where(x => x is IVketSaveData).ToArray();
                //应用到vketSaveData的序列化字段
                var serializedObject = new SerializedObject(vketSaveData);
                serializedObject.FindProperty("_saveDataScripts").arraySize = saveFields.Count;
                serializedObject.FindProperty("_fieldPathNames").arraySize = saveFields.Count;
                serializedObject.FindProperty("_fieldTypes").arraySize = saveFields.Count;
                serializedObject.FindProperty("_IVketSaveDataCallback").arraySize = IVketSaveDataCallback.Length;
                for (int i = 0; i < saveFields.Count; i++)
                {
                    serializedObject.FindProperty("_saveDataScripts").GetArrayElementAtIndex(i).objectReferenceValue = saveFields[i].beh;
                    serializedObject.FindProperty("_fieldPathNames").GetArrayElementAtIndex(i).stringValue = saveFields[i].path;
                    serializedObject.FindProperty("_fieldTypes").GetArrayElementAtIndex(i).stringValue = saveFields[i].fieldType;
                }
                for (int i = 0; i < IVketSaveDataCallback.Length; i++)
                {
                    serializedObject.FindProperty("_IVketSaveDataCallback").GetArrayElementAtIndex(i).objectReferenceValue = IVketSaveDataCallback[i];
                }
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("VketSaveData not found");
            }
        }
    }
}
#endif