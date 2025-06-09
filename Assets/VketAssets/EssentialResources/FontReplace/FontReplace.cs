using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif
public class FontReplace : MonoBehaviour
{
    [Header("VRChat FallBack Font '_Empty'")]
    public TMP_FontAsset _Empty;
    [Header("指定TMP_FontAsset")]
    public List<TMP_FontAsset> thinkReplaceFont;
    public bool enableStandaloneReplace;
    public bool enableAndroidReplace=true;


}

#if UNITY_EDITOR

[CustomEditor(typeof(FontReplace))]
public class FontReplaceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FontReplace fontReplace = (FontReplace)target;

        if (GUILayout.Button("シーン内で使用されているすべてのTMP_FontAssetを検索して入力"))
        {
            FindAndFillTMPFontAssets(fontReplace);
        }
    }

    private void FindAndFillTMPFontAssets(FontReplace fontReplace)
    {
        List<TMP_FontAsset> foundFonts = new List<TMP_FontAsset>();

        foreach (var item in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                if (text.font != null&&text.font!=fontReplace._Empty && !foundFonts.Contains(text.font))
                {
                    foundFonts.Add(text.font);
                }
            }
        }

        fontReplace.thinkReplaceFont = foundFonts;
        EditorUtility.SetDirty(fontReplace);
    }
}
#endif
