using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace satania.sataniashopping.blendshapeinsert
{
    public static class MeshExtensions
    {
        public static Mesh Clone(this Mesh original, bool copyBlendshape = true)
        {
            Mesh mesh = new Mesh();
            mesh.name = original.name + " Clone";
            mesh.vertices = original.vertices;
            mesh.boneWeights = original.boneWeights;
            mesh.bindposes = original.bindposes;
            mesh.normals = original.normals;
            mesh.bounds = original.bounds;
            mesh.triangles = original.triangles;
            mesh.colors = original.colors;
            //mesh.subMeshCount = original.subMeshCount;
            mesh.colors32 = original.colors32;
            mesh.tangents = original.tangents;
            
            mesh.bounds = original.bounds;
            mesh.indexFormat = original.indexFormat;
      
            mesh.uv = original.uv;
            mesh.uv2 = original.uv2;
            mesh.uv3 = original.uv3;
            mesh.uv4 = original.uv4;
            mesh.uv5 = original.uv5;
            mesh.uv6 = original.uv6;
            mesh.uv7 = original.uv7;
            mesh.uv8 = original.uv8;


            if (copyBlendshape)
            {
                for (int i = 0; i < original.blendShapeCount; i++)
                {
                    string shapeName = original.GetBlendShapeName(i);
                    int frameCount = original.GetBlendShapeFrameCount(i);
                    Vector3[] deltaVertices = new Vector3[original.vertexCount];
                    Vector3[] deltaNormals = new Vector3[original.vertexCount];
                    Vector3[] deltaTangents = new Vector3[original.vertexCount];
                    for (int j = 0; j < frameCount; j++)
                    {
                        original.GetBlendShapeFrameVertices(i, j, deltaVertices, deltaNormals, deltaTangents);
                        mesh.AddBlendShapeFrame(shapeName, original.GetBlendShapeFrameWeight(i, j), deltaVertices, deltaNormals, deltaTangents);
                    }
                }
            }
            mesh.UploadMeshData(true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    public static class FileSaveUtility
    {
        public static UnityEngine.Object SaveFile(UnityEngine.Object asset, string filepath, string ex)
        {
            Type type = asset.GetType();

            string AssetPath = AssetDatabase.GetAssetPath(asset);
            if (AssetPath == null || string.IsNullOrEmpty(AssetPath))
                AssetDatabase.CreateAsset(asset, filepath + $".{ex}");
            else
                AssetDatabase.CopyAsset(AssetPath, filepath + $".{ex}");

            if (!File.Exists(filepath + $".{ex}"))
                return default;

            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath(filepath + $".{ex}", type);
        }
    }

    [CustomEditor(typeof(Blendshape_Insert))]
    public class BlendShape_Insert_Editor : Editor
    {
        //スクロール位置
        private Vector2 _scrollPosition = Vector2.zero;

        private string[] blendshapeNames = new string[0];
        
 
        private ReorderableList stringList;

        string generatepath = "Assets/さたにあしょっぴんぐ/コンポーネント/ブレンドシェイプ挿入ツール/Generated/";

        /// <summary>
        /// 配布するブレンドシェイプキーのIndex
        /// </summary>
        public int selectedBlendshape = 0;

        /// <summary>
        /// 取得するレンダラー
        /// </summary>
        public SkinnedMeshRenderer renderer;

        /// <summary>
        /// 開発者用オプションを表示するか
        /// </summary>
        public bool showField = false;

        /// <summary>
        /// 適応するアバター
        /// </summary>
        public GameObject _avatar = null;

        /// <summary>
        /// Popupで表示するためにブレンドシェイプの一覧を取得
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public string[] GetBlendshapeNames(Mesh mesh)
        {
            if (mesh == null)
                return null;

            string[] names = new string[mesh.blendShapeCount];

            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                names[i] = mesh.GetBlendShapeName(i);
            }

            return names;
        }

        /// <summary>
        /// OKのみのメッセージボックス
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="ok"></param>
        public void MessageBox(string message, string ok)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayDialog("ブレンドシェイプ挿入ツール", message, ok);
#endif
        }

        public override void OnInspectorGUI()
        {
            // targetを変換して対象スクリプトの参照を取得する
            Blendshape_Insert _Blendshape_Insert = target as Blendshape_Insert;

            _avatar = EditorGUILayout.ObjectField("アバター", _avatar, typeof(GameObject), true) as GameObject;

            if (GUILayout.Button("ブレンドシェイプを追加"))
            {
                //もしデータが無い場合は返す
                if (string.IsNullOrEmpty(_Blendshape_Insert.GetBlendshapeData().name))
                    return;

                if (_avatar == null)
                    return;

                //指定されたメッシュを取得
                var avatar_mesh = _avatar.transform.Find(_Blendshape_Insert.mesh_path);

                if (avatar_mesh)
                {
                    var skinnedmeshrenderer = avatar_mesh.GetComponent<SkinnedMeshRenderer>();

                    if (skinnedmeshrenderer && skinnedmeshrenderer.sharedMesh)
                    {
                        if (skinnedmeshrenderer.sharedMesh.vertexCount != _Blendshape_Insert.vertexcount)
                        {
                            MessageBox("頂点数が違うメッシュが選択されています。\n正しいアバターを入れているか確認してください。", "はい");
                        }

                        if (!Directory.Exists(generatepath + $"{_avatar.name}" + "/"))
                            Directory.CreateDirectory(generatepath + $"{_avatar.name}" + "/");

                        Mesh mesh = skinnedmeshrenderer.sharedMesh;

                        var path = generatepath + $"{_avatar.name}" + "/" + $"{mesh.name}.asset";

                        mesh = Instantiate(mesh);
                        //mesh = mesh.Clone(true);

                        List<Action> actions = new List<Action>();
                        for (int i = 0; i < mesh.blendShapeCount; i++)
                        {
                            var name = mesh.GetBlendShapeName(i);
                            if (name == _Blendshape_Insert.GetBlendshapeData().name)
                                continue;

                            var frameCount = mesh.GetBlendShapeFrameCount(i);
                            var maxweight = mesh.GetBlendShapeFrameWeight(i, frameCount - 1);


                            for (int k = 0; k < frameCount; k++)
                            {
                                Vector3[] deltaVertices = new Vector3[mesh.vertexCount];
                                Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
                                Vector3[] deltaTangents = new Vector3[mesh.vertexCount];

                                float frameWeight = mesh.GetBlendShapeFrameWeight(i, k);
                                mesh.GetBlendShapeFrameVertices(i, k, deltaVertices, deltaNormals, deltaTangents);

                                actions.Add(() =>
                                {
                                    mesh.AddBlendShapeFrame(name, frameWeight, deltaVertices, deltaNormals, deltaTangents);
                                });
                            }
                        }

                        mesh.ClearBlendShapes();

                        foreach (var act in actions)
                        {
                            act.Invoke();
                        }

                        AssetDatabase.CreateAsset(mesh, path);
                        AssetDatabase.Refresh();


                        _Blendshape_Insert.SetBlendshape(mesh, _Blendshape_Insert.GetBlendshapeData().name);
                        //Vector3[]

                        //_Blendshape_Insert.ApplyBlendShape(mesh, _Blendshape_Insert.GetBlendshapeData().vertexies[0].deltaVertices, _Blendshape_Insert.GetBlendshapeData().vertexies[0].deltaNormals, _Blendshape_Insert.GetBlendshapeData().vertexies[0].deltaTangents, 1.0f);
                        skinnedmeshrenderer.sharedMesh = mesh;

                        EditorUtility.SetDirty(_avatar);

                        MessageBox("追加しました！", "OK");
                    }
                }
            }

            showField = EditorGUILayout.ToggleLeft("開発者用オプション", showField);

            if (showField)
            {
                EditorGUI.BeginChangeCheck();
                renderer = EditorGUILayout.ObjectField("SkinnedMeshRenderer", renderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;

                if (EditorGUI.EndChangeCheck())
                {
                    if (renderer && renderer.sharedMesh != null)
                    {
                        blendshapeNames = GetBlendshapeNames(renderer.sharedMesh);
                    }
                }

                if (_Blendshape_Insert.GetBlendshapeData() != null)
                {
                    EditorGUILayout.IntField("ブレンドシェイプのフレーム数 ※変更不可", _Blendshape_Insert.GetBlendshapeData().frameCount);
                    EditorGUILayout.FloatField("Weightの最大値 ※変更不可", _Blendshape_Insert.GetBlendshapeData().maxweight);
                    EditorGUILayout.IntField("△頂点数", _Blendshape_Insert.vertexcount);
                    _Blendshape_Insert.GetBlendshapeData().name = EditorGUILayout.TextField("ブレンドシェイプの名前", _Blendshape_Insert.GetBlendshapeData().name);
                }
                _Blendshape_Insert.mesh_path = EditorGUILayout.TextField("メッシュのパス", _Blendshape_Insert.mesh_path);


                if (renderer)
                {
                    var mesh = renderer.sharedMesh;

                    if (mesh)
                    {
                        //描画範囲が足りなければスクロール出来るように
                        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));

                        var radioStyle = new GUIStyle(EditorStyles.radioButton);
                        radioStyle.richText = true;
                        selectedBlendshape = GUILayout.SelectionGrid(selectedBlendshape, blendshapeNames, 1, radioStyle);

                        //selectedBlendshape = EditorGUILayout.Popup(selectedBlendshape, blendshapeNames);

                        //スクロール箇所終了
                        EditorGUILayout.EndScrollView();
                    }

                    if (GUILayout.Button("ブレンドシェイプを取得"))
                    {
                        if (mesh == null)
                            return;

                        string path = _Blendshape_Insert.GetHierarchyPath(renderer.gameObject);
                        //Debug.Log(path);

                        _Blendshape_Insert.mesh_path = path;
                        _Blendshape_Insert.GetBlendshape(mesh, selectedBlendshape);

                        _Blendshape_Insert.json = JsonUtils.ToJson(_Blendshape_Insert.GetBlendshapeData());
                        _Blendshape_Insert.vertexcount = mesh.vertexCount;

                        EditorUtility.SetDirty(_Blendshape_Insert);
                        EditorUtility.SetDirty(_Blendshape_Insert.gameObject);

                        MessageBox("取得しました！", "OK");

                        //string jsonstr = JsonUtility.ToJson(_Blendshape_Insert.blendshapedata);
                    }

                    if (GUILayout.Button("保存"))
                    {
                        _Blendshape_Insert.mesh_path = _Blendshape_Insert.GetHierarchyPath(renderer.gameObject);
                        _Blendshape_Insert.GetBlendshape(mesh, selectedBlendshape);

                        _Blendshape_Insert.json = JsonUtils.ToJson(_Blendshape_Insert.GetBlendshapeData());

                        //if (!Directory.Exists(generatepath + $"{renderer.transform.root.name}/"))
                        //    Directory.CreateDirectory(generatepath + $"{renderer.transform.root.name}/");

                        File.WriteAllText(generatepath + "Blendshape.json", _Blendshape_Insert.json);

                        EditorUtility.SetDirty(_Blendshape_Insert);
                        EditorUtility.SetDirty(_Blendshape_Insert.gameObject);

                        MessageBox("保存しました！", "OK");
                    }
                }
            }
        }
    }
}
