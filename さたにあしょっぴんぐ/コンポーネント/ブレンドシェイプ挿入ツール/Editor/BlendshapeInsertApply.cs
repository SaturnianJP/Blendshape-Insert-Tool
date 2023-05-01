using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace satania.sataniashopping.blendshapeinsert
{
    public partial class ApplyTool : EditorWindow
    {

        /// <summary>
        /// エディタのタイトル
        /// </summary>
        public static string EditorTitle = "ブレンドシェイプ追加ツール";

        [MenuItem("さたにあしょっぴんぐ/ブレンドシェイプ系/ブレンドシェイプ追加ツール", priority = 13)]
        private static void Init()
        {
            //ウィンドウのインスタンスを生成
            ApplyTool window = GetWindow<ApplyTool>();

            //ウィンドウサイズを固定
            window.maxSize = window.minSize = new Vector2(512, 512);

            //タイトルを変更
            window.titleContent = new GUIContent(EditorTitle);
        }

        private void ShowGUI()
        {

        }

        /// <summary>
        /// GUI描画用
        /// </summary>
        public void OnGUI()
        {
            ShowGUI();
        }
    }
}