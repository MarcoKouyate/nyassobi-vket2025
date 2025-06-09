
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

public class TextToTexture : UdonSharpBehaviour
{
    [SerializeField] TextAsset _json;
    [SerializeField] Material[] _materials;

    void Start()
    {
        SendCustomEventDelayedSeconds(nameof(Test), 5f);
    }

    public void Test()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        var n = Convert();

        sw.Stop();
        var elapsed = (float)sw.Elapsed.TotalSeconds;
        Debug.Log($"{n} textures Loaded in {elapsed} sec");
    }

    private int Convert()
    {
        int i = 0;
        var json = _json.text;
        if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
        {
            var textures = result.DataDictionary["textures"].DataDictionary;
            var keys = textures.GetKeys();
            for (; i < keys.Count; i++)
            {
                DataToken key = keys[i].String;//"Page"
                var textureSize = textures[key].DataDictionary["textureSize"].DataList;
                var data = textures[key].DataDictionary["data"].String;
                var w = (int)textureSize[0].Double;
                var h = (int)textureSize[1].Double;
#if UNITY_ANDROID
                var tex = new Texture2D(w, h, TextureFormat.ETC_RGB4Crunched, false);
#else
                var tex = new Texture2D(w, h, TextureFormat.DXT1Crunched, false);
#endif
                byte[] data_ = System.Convert.FromBase64String(data);
                // Debug.Log($"len: {data_.Length}, w: {w}, h: {h}");
                tex.LoadRawTextureData(data_);
                tex.Apply(updateMipmaps: false);
                _materials[i].SetTexture("_MainTex", tex);
                Debug.Log($"load-image done: {key}");
            }
        } else {
            // Deserialization failed. Let's see what the error was.
            Debug.Log($"Failed to Deserialize json - {result.ToString()}");
        }
        return i;
    }
}
