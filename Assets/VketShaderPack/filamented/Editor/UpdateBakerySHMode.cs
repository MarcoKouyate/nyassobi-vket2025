/*
This file is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to 
https://www.gamedevblog.com/2012/12/unity-editor-script-for-replacing-shaders.html
*/
using UnityEngine;

using UnityEditor;

using System.Collections;

// UpdateBakerySHMode 
// Gathers all the materials used in the scene, 
// and then sets their Bakery directional mode. 

namespace SilentTools
{
public class UpdateBakerySHMode : ScriptableWizard
{
	// Show only the currently supported types, but match to Bakery's list in ftRenderLightmaps.cs.
    private enum BakeryRenderDirMode
    {
        None = 0,
        BakedNormalMaps = 1,
        DominantDirection = 2,
        RNM = 3,
        SH = 4,
        MonoSH = 6
    };

	//public BakeryRenderDirMode oldShader;
	[SerializeField]
	private BakeryRenderDirMode newDirectionalMode;
	
	[MenuItem("Tools/Silent/Update Material Directional Mode for Bakery")]

	static void CreateWizard ()
	{
		ScriptableWizard.DisplayWizard<UpdateBakerySHMode>  ("Update Material Directional Mode for Bakery", "Update");
	}

	private void ClearBakeryKeywords(Material m)
	{
		m.DisableKeyword("_BAKERY_RNM");
		m.DisableKeyword("_BAKERY_SH");
		m.DisableKeyword("_BAKERY_MONOSH");
		m.SetFloat("_Bakery", 0);
	}

	void OnWizardCreate ()
	{
		int totalMaterials = 0;
		int updatedMaterials = 0;

		Renderer[] renderers = GameObject.FindSceneObjectsOfType (typeof(Renderer)) as Renderer[];
		foreach (var renderer in renderers) {
			foreach (Material m in renderer.sharedMaterials) {
				totalMaterials++;
				// Probably only works with Filamented or its templates. Sorry!
				if (m != null && m.HasProperty("_Bakery")) {
					switch(newDirectionalMode)
					{
						// Placeholders, but clearing is important. 
						case (BakeryRenderDirMode.None):
						ClearBakeryKeywords(m);
						break;
						case (BakeryRenderDirMode.BakedNormalMaps):
						ClearBakeryKeywords(m);
						break;
						case (BakeryRenderDirMode.DominantDirection):
						ClearBakeryKeywords(m);
						break;

						// Note: Matched to [KeywordEnum(None, SH, RNM, MonoSH)] in Filamented
						case (BakeryRenderDirMode.SH):
						ClearBakeryKeywords(m);
						m.EnableKeyword("_BAKERY_SH");
						m.SetFloat("_Bakery", 1);
						break;
						
						case (BakeryRenderDirMode.RNM):
						ClearBakeryKeywords(m);
						m.EnableKeyword("_BAKERY_RNM");
						m.SetFloat("_Bakery", 2);
						break;
						
						case (BakeryRenderDirMode.MonoSH):
						ClearBakeryKeywords(m);
						m.EnableKeyword("_BAKERY_MONOSH");
						m.SetFloat("_Bakery", 3);
						break;

					}
					updatedMaterials++;
				}
			}

		}
		Debug.Log("Updated " + updatedMaterials + " materials of " + totalMaterials + ".");
	}
}
}