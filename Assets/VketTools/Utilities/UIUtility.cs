using UnityEditor;
using UnityEngine;

namespace VketTools.Utilities
{
    public static class UIUtility
    {
        private static Texture2D progressBarBGTexture;
        private static Texture2D progressBarValTexture;
        private static GUIContent[] waitSpins;

        private static GUIStyle GetLinkStyle(GUIStyle style)
        {
            var ul = new GUIStyle(style);
            var state = new GUIStyleState();
            state.textColor = new Color(184 / 255, 184 / 255, 255 / 255);
            ul.normal = state;
            return ul;
        }

        /// <summary>
        /// 開けるリンクを表示
        /// </summary>
        /// <param name="url">開くURL</param>
        /// <param name="rect">表示する座標・サイズ</param>
        /// <param name="style">指定GUIStyle</param>
        /// <param name="label">表示するラベル[Option]</param>
        public static void GUILink(string url, Rect rect, GUIStyle style, string label = "")
        {
            label = string.IsNullOrEmpty(label) ? url : label;
            var content = new GUIContent(label);
            var ul = GetLinkStyle(style);
            rect.width = rect.width == 0 ? ul.CalcSize(content).x : rect.width;
            rect.height = rect.height == 0 ? ul.CalcSize(content).y : rect.height;
            if (GUI.Button(rect, content, ul))
            {
                Application.OpenURL(url);
            }
        }

        /// <summary>
        /// EditorGUI用の開けるリンクを表示
        /// </summary>
        /// <param name="url">開くURL</param>
        /// <param name="style">指定GUIStyle</param>
        /// <param name="label">表示するラベル[Option]</param>
        public static void EditorGUILink(string url, GUIStyle style, string label = "")
        {
            label = string.IsNullOrEmpty(label) ? url : label;
            var content = new GUIContent(label);
            var ul = GetLinkStyle(style);
            if (GUILayout.Button(content, ul))
            {
                Application.OpenURL(url);
            }
        }

        /// <summary>
        /// 指定したGUIStyleを指定した幅に収まるフォントサイズに設定して返す
        /// </summary>
        /// <param name="content">表示するGUIContent</param>
        /// <param name="style">指定GUIStyle</param>
        /// <param name="size">指定横幅</param>
        /// <returns></returns>
        public static GUIStyle GetContentSizeFitStyle(GUIContent content, GUIStyle style, float size)
        {
            if (style == null)
                return null;
            
            GUIStyle result = new GUIStyle(style);
            while(result.fontSize != 1 && result.CalcSize(content).x > size)
            {
                result.fontSize--;
            }

            return result;
        }

        /// <summary>
        /// EditorGUI用の読み込みアイコンを表示
        /// </summary>
        /// <param name="spinRate">回転速度</param>
        /// <param name="size">サイズ</param>
        public static void EditorGUIWaitSpin(float spinRate = 10, float size = 16)
        {
            if (waitSpins == null)
            {
                waitSpins = new GUIContent[12];
                for (int i = 0; i < waitSpins.Length; ++i)
                {
                    waitSpins[i] = EditorGUIUtility.IconContent($"WaitSpin{i:00}");
                }
            }

            GUILayout.Box(waitSpins[(int)Mathf.Repeat(Time.realtimeSinceStartup * spinRate, 11.99f)], GUIStyle.none, GUILayout.Width(size), GUILayout.Height(size));
        }

        /// <summary>
        /// EditorGUI用のプログレスバー
        /// </summary>
        /// <param name="value">現在値。0～1で指定</param>
        /// <param name="options">追加のGUILayoutOptioinのリスト</param>
        public static void EditorGUIProgressBar(float value, params GUILayoutOption[] options)
        {
            value = Mathf.Clamp01(value);
            var bgStyle = new GUIStyle(GUI.skin.horizontalSlider);
            var valStyle = new GUIStyle(GUI.skin.horizontalSlider);

            if (progressBarBGTexture == null)
            {
                progressBarBGTexture = GetStyleTexture(GUI.skin.horizontalSlider);
            }

            if (progressBarValTexture == null)
            {
                progressBarValTexture = GetStyleTexture(GUI.skin.horizontalSlider);
                var cols = progressBarValTexture.GetPixels();
                for (int i = 0; i < cols.Length; ++i)
                {
                    var hsv = HSV.FromRGBA(cols[i]);
                    var gr = HSV.FromRGBA(Color.green);
                    hsv.h = gr.h;
                    hsv.s = gr.s;
                    hsv.v += 0.3f;
                    cols[i] = hsv.ToRGBA();
                }
                progressBarValTexture.SetPixels(cols);
                progressBarValTexture.Apply();
            }

            var bgState = bgStyle.normal;
            bgState.background = progressBarBGTexture;
            bgStyle.normal = bgState;
            bgStyle.fixedHeight = 25;
            var valState = valStyle.normal;
            valState.background = progressBarValTexture;
            valStyle.normal = valState;
            valStyle.fixedHeight = 25;
            valStyle.border.left = 0;
            valStyle.border.right = 0;

            var bgRect = GUILayoutUtility.GetRect(new GUIContent("mmmm"), bgStyle, options);
            var valRect = new Rect(bgRect);
            valRect.width = valRect.width * value;

            if (Event.current.type == EventType.Repaint)
            {
                bgStyle.Draw(bgRect, false, false, false, false);
                valStyle.Draw(valRect, false, false, false, false);
            }
        }
        
        /// <summary>
        /// ヘルプボックスの高さを取得
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static float GetHelpBoxHeight(string message, MessageType type = MessageType.Info, float margineWidth = 0)
        {
            var style   = new GUIStyle( "HelpBox" );
            var content = new GUIContent( message );
            return Mathf.Max( style.CalcHeight( content, Screen.width - (type != MessageType.None ? 53 : 21) - margineWidth), 40);
        }
        
        public static int GetOptimalFontSize(string text, GUIStyle style, float maxWidth, float maxHeight = 60)
        {
            int fontSize = 14;
            style.fontSize = fontSize;

            while (style.CalcHeight(new GUIContent(text), maxWidth) > maxHeight && fontSize > 6)
            {
                fontSize--;
                style.fontSize = fontSize;
            }

            return fontSize;
        }

        private static Texture2D GetStyleTexture(GUIStyle style)
        {
            var tex = style.normal.background;
            var rt = RenderTexture.GetTemporary(tex.width, tex.height);
            Graphics.Blit(tex, rt);
            var rtOld = RenderTexture.active;
            RenderTexture.active = rt;
            var bg = new Texture2D(rt.width, rt.height);
            bg.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            bg.Apply();
            RenderTexture.active = rtOld;
            RenderTexture.ReleaseTemporary(rt);
            return bg;
        }
        
        public static Texture2D CreateColorTexture(int width, int height, Color color = default)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x,y,color);
                }
            }
            texture.Apply();
            return texture;
        }

        private class HSV
        {
            public float h;
            public float s;
            public float v;
            public float a;

            public HSV(float h, float s, float v, float a = 1)
            {
                this.h = h;
                this.s = s;
                this.v = v;
                this.a = a;
            }

            public static HSV FromRGBA(Color col)
            {
                float h, s, v;
                Color.RGBToHSV(new Color(col.r, col.g, col.b), out h, out s, out v);
                return new HSV(h, s, v, col.a);
            }

            public Color ToRGBA()
            {
                var col = Color.HSVToRGB(h, s, v);
                col.a = a;
                return col;
            }
        }
    }
}