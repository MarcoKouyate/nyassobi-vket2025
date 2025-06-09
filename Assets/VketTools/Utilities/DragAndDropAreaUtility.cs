//  DragAndDropAreaUtility.cs
//  http://kan-kikuchi.hatenablog.com/entry/DragAndDropAreaUtility
//
//  Created by kan.kikuchi on 2021.01.17.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VketTools.Utilities
{
    /// <summary>
    /// ドラック&ドロップでオブジェクトを取得するGUIを表示するクラス
    /// </summary>
    public static class DragAndDropAreaUtility
    {
        private static GUIStyle _titleStyle;

        /// <summary>
        /// ドラック&ドロップでオブジェクトを取得するGUI表示(取得してない時はnullが返る)
        /// </summary>
        public static T GetObject<T>(string areaTitle = "Drag & Drop", Color? areaColor = null, Color? textColor = null, float widthMin = 0, float height = 50)
            where T : Object
        {
            //ドラックドロップされたオブジェクト取得
            var objectReferences = GetObjects(areaTitle, areaColor, textColor, widthMin, height);

            //ドロップされたオブジェクトに対象の物があれば返す
            return objectReferences?.FirstOrDefault(o => o is T) as T;
        }

        /// <summary>
        /// ドラック&ドロップで複数のオブジェクトを取得するGUI表示(取得した時だけtrueが返る、取得した物はtargetListにaddされる)
        /// </summary>
        public static bool GetObjects<T>(List<T> targetList, string areaTitle = "Drag & Drop", Color? areaColor = null, Color? textColor = null, float widthMin = 0,
            float height = 50) where T : Object
        {
            //ドラックドロップされたオブジェクトがなければスルー
            var objectReferences = GetObjects(areaTitle, areaColor, textColor, widthMin, height);
            if (objectReferences == null)
            {
                return false;
            }

            //ドロップされたオブジェクトに対象の物が無ければスルー
            var targetObjectReferences = objectReferences.OfType<T>().ToList();
            if (targetObjectReferences.Count == 0)
            {
                return false;
            }

            //対象を登録
            targetList.AddRange(targetObjectReferences);
            return true;
        }

        //ドラックドロップで複数のオブジェクトを取得するGUI表示(取得してない時はnullが返る)
        private static Object[] GetObjects(string areaTitle = "Drag & Drop", Color? areaColor = null, Color? textColor = null, float widthMin = 0, float height = 50)
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    alignment = TextAnchor.UpperCenter
                };
            }
            
            _titleStyle.normal.textColor = textColor ?? GUI.skin.textField.normal.textColor;
            
            //D&D出来る場所を描画
            var dropArea = GUILayoutUtility.GetRect(widthMin, height, GUILayout.ExpandWidth(true));
            //GUI.Box(dropArea, areaTitle);
            EditorGUI.DrawRect(dropArea, areaColor ?? Color.gray);
            EditorGUI.LabelField(dropArea, areaTitle, _titleStyle);

            //マウスの位置がD&Dの範囲になければスルー
            if (!dropArea.Contains(Event.current.mousePosition))
            {
                return null;
            }

            //現在のイベントを取得
            var eventType = Event.current.type;

            //ドラッグ＆ドロップで操作が更新された時でも、実行した時でもなければスルー
            if (eventType != EventType.DragUpdated && eventType != EventType.DragPerform)
            {
                return null;
            }

            //カーソルに+のアイコンを表示
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            //ドラッグ＆ドロップで無ければスルー
            if (eventType != EventType.DragPerform)
            {
                return null;
            }

            //ドラッグを受け付ける(ドラッグしてカーソルにくっ付いてたオブジェクトが戻らなくなるので)
            DragAndDrop.AcceptDrag();

            //イベントを使用済みにする
            Event.current.Use();

            return DragAndDrop.objectReferences;
        }
    }
}