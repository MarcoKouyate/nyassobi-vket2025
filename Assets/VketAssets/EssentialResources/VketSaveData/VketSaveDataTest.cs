
using UdonSharp;
using UnityEngine;
using Vket.EssentialResources.Attribute;
using Vket.EssentialResources.VketSaveData;
using Vket.EssentialResources.VketSaveData.Attribute;
using Vket.EssentialResources.VketSaveData.Interface;

public class VketSaveDataTest : UdonSharpBehaviour
    #if UNITY_EDITOR && !COMPILER_UDONSHARP
    ,IVketSaveData
    #endif
{
    [SerializeField,Save(mulitInstance:true)]
    public uint testInt;
    [SerializeField,Save]
    public float testFloat;
    [SerializeField,Save]
    public string testString;
    [SerializeField,Save]
    public bool testBool;
    [SerializeField,Save]
    public sbyte testSByte;
    [SerializeField,Save]
    public byte testByte;
    [SerializeField,Save]
    public short testShort;
    [SerializeField,Save]
    public ushort testUShort;
    [SerializeField,Save]
    public long testLong;
    [SerializeField,Save]
    public ulong testULong;
    [SerializeField,Save]
    public uint testUInt;
    [SerializeField,Save]
    public double testDouble;
    [SerializeField,Save]
    public byte[] testBytes=new byte[0];
    [SerializeField,Save]
    public Vector2 testVector2;
    [SerializeField,Save]
    public Vector3 testVector3;
    [SerializeField,Save]
    public Vector4 testVector4;
    [SerializeField,Save]
    public Quaternion testQuaternion;
    [SerializeField,Save]
    public Color testColor;
    [SerializeField,Save]
    public Color32 testColor32;
    // [SerializeField,Save]
    // GameObject testGameObject;
    // [SerializeField,Save]
    // Toggle testToggle;
    // [SerializeField,Save]
    // Slider testSlider;
    // [SerializeField,Save]
    // Text testText;

    [SerializeField,SceneSingleton] VketSaveData vketSaveData;

    public void LoadedSaveData()
    {
        Debug.Log("LoadedSaveData");
    }


    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            vketSaveData.SaveAll();
        }
    }
}