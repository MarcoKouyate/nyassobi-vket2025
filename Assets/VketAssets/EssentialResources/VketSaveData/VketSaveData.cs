
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using Vket.EssentialResources.Attribute;
using Vket.EssentialResources.VketSaveData.Interface;
using VRC.SDK3.Persistence;
using VRC.SDKBase;
using VRC.Udon;
namespace Vket.EssentialResources.VketSaveData
{
    /// <summary>
    /// 保存系统
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketSaveData : UdonSharpBehaviour
    {
        [SerializeField] float _loadTimeOut = 30.0f;
        [SerializeField,ReadOnly] UdonSharpBehaviour[] _saveDataScripts;
        [SerializeField,ReadOnly] string[] _fieldPathNames;
        [SerializeField,ReadOnly] string[] _fieldTypes;
        [SerializeField,ReadOnly] UdonSharpBehaviour[] _IVketSaveDataCallback;

        bool isLoaded;
        bool isError;
        bool isRestored;

        public bool IsLoaded { get => isLoaded; set => isLoaded = value; }
        public bool IsError { get => isError; set => isError = value; }
        public bool IsRestored { get => isRestored; set => isRestored = value; }


        void Update()
        {
            if(isError||isRestored)
                return;
           _loadTimeOut -= Time.deltaTime;
            if (_loadTimeOut <= 0.0f)
            {
                isError = true;
                Debug.LogError($"<color=cyan>[VketSaveData]</color> LoadData Timeout!");
            }
        }   
        public void SaveAll()
        {
            if(!isRestored||isError)
            {
                Debug.LogError($"<color=cyan>[VketSaveData]</color> Save Failed! isLoaded:{isLoaded} isError:{isError}");
                return;
            }
            Debug.Log($"<color=cyan>[VketSaveData]</color> SaveAll ...");
            for (int i = 0; i < _saveDataScripts.Length; i++)
            {
                var fieldName = _fieldPathNames[i].Substring(_fieldPathNames[i].LastIndexOf('.') + 1);
                var value = _saveDataScripts[i].GetProgramVariable(fieldName);
                if (value != null)
                {
                    // Debug.Log($"<color=cyan>[VketSaveData]</color> Save {_fieldPathNames[i]} => {value}");
                    SetObject(_fieldTypes[i], _fieldPathNames[i], value);
                }
            }
        }
        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if(player.isLocal)
            {
                isRestored = true;
                if(!isLoaded)
                {
                    Debug.Log($"<color=cyan>[VketSaveData]</color> New User!");
                    isLoaded = true;
                }
            }
        }
        public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
        {
            if (player.isLocal)
            {
                if (!isLoaded&&!isError)
                {
                    isLoaded = true;
                    Debug.Log($"<color=cyan>[VketSaveData]</color> Load Data...");
                    for (int i = 0; i < _saveDataScripts.Length; i++)
                    {
                        var val = GetObject(_fieldTypes[i], _fieldPathNames[i], player);
                        // Debug.Log($"<color=cyan>[VketSaveData]</color> Load {_fieldPathNames[i]} => {val}");
                        var fieldName = _fieldPathNames[i].Substring(_fieldPathNames[i].LastIndexOf('.') + 1);
                        SetProgramVariable(_saveDataScripts[i], _fieldTypes[i], fieldName, val);
                    }
                    foreach (var callback in _IVketSaveDataCallback)
                    {
                        if (callback != null)
                        {
                            callback.SendCustomEvent(nameof(IVketSaveData.LoadedSaveData));
                        }
                    }
                    Debug.Log($"<color=cyan>[VketSaveData]</color> Loaded Success!");
                }

            }
        }
        void SetProgramVariable(UdonSharpBehaviour udon,string fieldType, string fieldName, object value)
        {
            switch (fieldType)
            {
                //特殊处理
                case nameof(Toggle):
                    ((Toggle)udon.GetProgramVariable(fieldName)).isOn = (bool)value;
                    break;
                case nameof(Slider):
                    ((Slider)udon.GetProgramVariable(fieldName)).value = (float)value;
                    break;
                case nameof(GameObject):
                    ((GameObject)udon.GetProgramVariable(fieldName)).SetActive((bool)value);
                    break;
                case nameof(Text):
                    ((Text)udon.GetProgramVariable(fieldName)).text = (string)value;
                    break;
                case nameof(TextMeshPro):
                    ((TextMeshPro)udon.GetProgramVariable(fieldName)).text = (string)value;
                    break;
                case nameof(TextMeshProUGUI):
                    ((TextMeshProUGUI)udon.GetProgramVariable(fieldName)).text = (string)value;
                    break;
                default:
                    udon.SetProgramVariable(fieldName, value);
                    break;
            }
        }
        public object GetObject(string type, string key, VRCPlayerApi player)
        {
            switch (type)
            {
                case nameof(Int32):
                    return PlayerData.GetInt(player, key);
                case nameof(Single):
                    return PlayerData.GetFloat(player, key);
                case nameof(String):
                    return PlayerData.GetString(player, key);
                case nameof(Boolean):
                    return PlayerData.GetBool(player, key);
                case nameof(SByte):
                    return PlayerData.GetSByte(player, key);
                case nameof(Byte):
                    return PlayerData.GetByte(player, key);
                case nameof(Int16):
                    return PlayerData.GetShort(player, key);
                case nameof(UInt16):
                    return PlayerData.GetUShort(player, key);
                case nameof(Int64):
                    return PlayerData.GetLong(player, key);
                case nameof(UInt64):
                    return PlayerData.GetULong(player, key);
                case nameof(UInt32):
                    return PlayerData.GetUInt(player,key);
                case nameof(Double):
                    return PlayerData.GetDouble(player, key);
                case "Byte[]":
                    return PlayerData.GetBytes(player, key);
                case nameof(Vector2):
                    return PlayerData.GetVector2(player, key);
                case nameof(Vector3):
                    return PlayerData.GetVector3(player, key);
                case nameof(Vector4):
                    return PlayerData.GetVector4(player, key);
                case nameof(Quaternion):
                    return PlayerData.GetQuaternion(player, key);
                case "Color":
                    return PlayerData.GetColor(player, key);
                case "Color32":
                    return PlayerData.GetColor32(player, key);
                case nameof(GameObject):
                case nameof(Toggle):
                    return PlayerData.GetBool(player, key);
                case nameof(Slider):
                    return PlayerData.GetFloat(player, key);
                case nameof(Text):
                case nameof(TextMeshPro):
                case nameof(TextMeshProUGUI):
                    return PlayerData.GetString(player, key);

                default:
                    Debug.Log($"<color=red>[VketSaveData]</color> ({key}) Unsupported type: {type}");
                    return null;
            }
        }
        public void SetObject(string type, string key, object value)
        {
            switch (type)
            {
                case nameof(Int32):
                    PlayerData.SetInt(key, (int)value);
                    break;
                case nameof(Single):
                    PlayerData.SetFloat(key, (float)value);
                    break;
                case nameof(String):
                    PlayerData.SetString(key, (string)value);
                    break;
                case nameof(Boolean):
                    PlayerData.SetBool(key, (bool)value);
                    break;
                case nameof(SByte):
                    PlayerData.SetSByte(key, (sbyte)value);
                    break;
                case nameof(Byte):
                    PlayerData.SetByte(key, (byte)value);
                    break;
                case nameof(Int16):
                    PlayerData.SetShort(key, (short)value);
                    break;
                case nameof(UInt16):
                    PlayerData.SetUShort(key, (ushort)value);
                    break;
                case nameof(Int64):
                    PlayerData.SetLong(key, (long)value);
                    break;
                case nameof(UInt64):
                    PlayerData.SetULong(key, (ulong)value);
                    break;
                case nameof(UInt32):
                    PlayerData.SetUInt(key, (uint)value);
                    break;
                case nameof(Double):
                    PlayerData.SetDouble(key, (double)value);
                    break;
                case "Byte[]":
                    PlayerData.SetBytes(key, (byte[])value);
                    break;
                case nameof(Vector2):
                    PlayerData.SetVector2(key, (Vector2)value);
                    break;
                case nameof(Vector3):
                    PlayerData.SetVector3(key, (Vector3)value);
                    break;
                case nameof(Vector4):
                    PlayerData.SetVector4(key, (Vector4)value);
                    break;
                case nameof(Quaternion):
                    PlayerData.SetQuaternion(key, (Quaternion)value);
                    break;
                case nameof(Color):
                    PlayerData.SetColor(key, (Color)value);
                    break;
                case nameof(Color32):
                    PlayerData.SetColor32(key, (Color32)value);
                    break;
                case nameof(GameObject):
                    PlayerData.SetBool(key, ((GameObject)value).activeSelf);
                    break;
                case nameof(Toggle):
                    PlayerData.SetBool(key, ((Toggle)value).isOn);
                    break;
                case nameof(Slider):
                    PlayerData.SetFloat(key, ((Slider)value).value);
                    break;
                case nameof(Text):
                    PlayerData.SetString(key, ((Text)value).text);
                    break;
                case nameof(TextMeshPro):
                    PlayerData.SetString(key, ((TextMeshPro)value).text);
                    break;
                case nameof(TextMeshProUGUI):
                    PlayerData.SetString(key, ((TextMeshProUGUI)value).text);
                    break;
                default:
                    Debug.Log($"<color=red>[VketSaveData]</color> ({key}) Unsupported type: {type}");
                    break;
            }
        }

    }
}