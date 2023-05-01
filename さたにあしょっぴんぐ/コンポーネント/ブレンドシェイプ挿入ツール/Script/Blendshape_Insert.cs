using LitJson;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VRC.SDKBase;

namespace satania.sataniashopping.blendshapeinsert
{
    public static class JsonUtils
    {
        public static string ToJson<T>(T obj)
        {
            var builder = new StringBuilder();
            var writer = new JsonWriter(builder)
            {
                PrettyPrint = true
            };
            JsonMapper.ToJson(obj, writer);
            return builder.ToString();
        }

        public static BlendshapeData FromJson(string json)
        {
            return JsonMapper.ToObject<BlendshapeData>(json);
        }
    }

    public class BlendshapeData
    {
        public string name;

        public int frameCount;
        public float maxweight;

        public List<BlendshapeVertex> vertexies = new List<BlendshapeVertex>();
    }

    //public class BlendshapeVertex
    //{
    //    public float frameWeight;

    //    public float weight;
    //    public Vector3[] deltaVertices;
    //    public Vector3[] deltaNormals;
    //    public Vector3[] deltaTangents;
    //}

    public class BlendshapeVertex
    {
        public float frameWeight;

        public float weight;
        public Vector3[] deltaVertices;
        public Vector3[] deltaNormals;
        public Vector4[] deltaTangents;
    }

    [ExecuteInEditMode]
    public class Blendshape_Insert : MonoBehaviour, IEditorOnly
    {
        public string json = "";
        public string mesh_path;
        public int vertexcount = 0;

        BlendshapeData blendshapedata;

        public BlendshapeData GetBlendshapeData()
        {
            return blendshapedata;
        }
        public void GetBlendShapeVertex(Mesh mesh, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents, out Vector3[] vertices, out Vector3[] normals, out Vector4[] tangents, float weight = 1.0f)
        {
            vertices = mesh.vertices;
            normals = mesh.normals;
            tangents = mesh.tangents;

            // Apply blend shape to vertices and normals
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += deltaVertices[i] * weight;
                normals[i] += deltaNormals[i] * weight;
            }

            // Apply blend shape to tangents
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 normal = normals[i];
                Vector3 tangent = tangents[i];
                Vector3 bitangent = Vector3.Cross(normal, tangent) * tangents[i].w;
                tangent += deltaTangents[i] * weight;
                tangent = Vector3.ProjectOnPlane(tangent, normal).normalized;
                tangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, Mathf.Sign(Vector3.Dot(bitangent, Vector3.Cross(normal, tangent))));
            }

            // Update mesh
            //mesh.vertices = vertices;
            //mesh.normals = normals;
            //mesh.tangents = tangents;
            //mesh.RecalculateBounds();
        }

        public void GetBlendshape(Mesh mesh, int index)
        {
            if (index == -1)
                return;

            if (mesh == null)
                return;

            blendshapedata = new BlendshapeData();

            blendshapedata.frameCount = mesh.GetBlendShapeFrameCount(index);
            blendshapedata.maxweight = mesh.GetBlendShapeFrameWeight(index, blendshapedata.frameCount - 1);
            blendshapedata.name = mesh.GetBlendShapeName(index);

            for (int i = 0; i < blendshapedata.frameCount; i++)
            {
                BlendshapeVertex vertex = new BlendshapeVertex();

                Vector3[] _deltaVertices = new Vector3[mesh.vertexCount];
                Vector3[] _deltaNormals = new Vector3[mesh.vertexCount];
                Vector3[] _deltaTangents = new Vector3[mesh.vertexCount];

                vertex.frameWeight = mesh.GetBlendShapeFrameWeight(index, i);
                mesh.GetBlendShapeFrameVertices(index, i, _deltaVertices, _deltaNormals, _deltaTangents);

                Vector3[] vertices = new Vector3[mesh.vertexCount];
                Vector3[] normals = new Vector3[mesh.vertexCount];
                Vector4[] tangents = new Vector4[mesh.vertexCount];

                GetBlendShapeVertex(mesh, _deltaVertices, _deltaNormals, _deltaTangents, out vertices, out normals, out tangents);

                vertex.deltaVertices = vertices;
                vertex.deltaNormals = normals;
                vertex.deltaTangents = tangents;

                blendshapedata.vertexies.Add(vertex);
            }
        }

        public void SetBlendshape(Mesh mesh)
        {
            if (string.IsNullOrEmpty(blendshapedata.name))
                return;

            if (mesh == null)
                return;

            for (int i = 0; i < blendshapedata.vertexies.Count; i++)
            {
                var vertex = blendshapedata.vertexies[i];
                if (vertex == null) continue;

                //mesh.AddBlendShapeFrame(blendshapedata.name, i, vertex.deltaVertices, vertex.deltaNormals, vertex.deltaTangents);
            }
        }

        private static Vector3 CalculateDeltaTangent(Vector3 originalTangent, Vector3 originalNormal, Vector3 blendshapeTangent, Vector3 deltaNormal, Vector3 originalVertex, Vector3 blendshapeVertex)
        {
            Vector3 tangent = originalTangent - Vector3.Dot(originalNormal, originalTangent) * originalNormal;

            Vector3 bitangent = Vector3.Cross(originalNormal, tangent);
            float handedness = (Vector3.Dot(bitangent, blendshapeTangent) < 0.0f) ? -1.0f : 1.0f;

            Vector3 deltaTangent = blendshapeTangent - originalTangent;
            deltaTangent -= Vector3.Dot(deltaTangent, originalNormal) * originalNormal;
            deltaTangent -= Vector3.Dot(deltaTangent, tangent) * tangent;

            float r = 1.0f / (Mathf.Abs(Vector3.Dot(Vector3.Cross(originalNormal, tangent), bitangent)) + Mathf.Epsilon);

            deltaTangent -= Vector3.Dot(deltaTangent, deltaNormal) * deltaNormal;
            deltaTangent = deltaTangent.normalized;

            // Make sure handedness is correct
            Vector3 deltaPosition = blendshapeVertex - originalVertex;
            Vector3 newBitangent = Vector3.Cross(originalNormal, tangent);
            if (Vector3.Dot(Vector3.Cross(deltaPosition, newBitangent), bitangent) < 0.0f)
            {
                handedness *= -1.0f;
            }

            return deltaTangent * handedness;
        }

        private static void GetDeltaArrays(Mesh originalMesh, Vector3[] blendshapeVertices, Vector3[] blendshapeNormals, Vector4[] blendshapeTangents, out Vector3[] deltaVertices, out Vector3[] deltaNormals, out Vector3[] deltaTangents)
        {
            Vector3[] originalVertices = originalMesh.vertices;
            Vector3[] originalNormals = originalMesh.normals;
            Vector4[] originalTangents = originalMesh.tangents;

            deltaVertices = new Vector3[originalVertices.Length];
            deltaNormals = new Vector3[originalNormals.Length];
            deltaTangents = new Vector3[originalTangents.Length];

            for (int i = 0; i < originalVertices.Length; i++)
            {
                deltaVertices[i] = blendshapeVertices[i] - originalVertices[i];
                deltaNormals[i] = blendshapeNormals[i] - originalNormals[i];

                // Calculate delta tangents using the method we used earlier
                Vector3 originalTangent = originalTangents[i];
                Vector3 originalNormal = originalNormals[i];
                Vector3 blendshapeTangent = blendshapeTangents[i];
                Vector3 deltaTangent = CalculateDeltaTangent(originalTangent, originalNormal, blendshapeTangent, deltaNormals[i], originalVertices[i], blendshapeVertices[i]);
                deltaTangents[i] = deltaTangent;
            }
        }

        public void SetBlendshape(Mesh mesh, string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (mesh == null)
                return;

            for (int i = 0; i < blendshapedata.vertexies.Count; i++)
            {
                var vertex = blendshapedata.vertexies[i];
                if (vertex == null) continue;

                float weight = vertex.frameWeight;

                Vector3[] deltavertices = new Vector3[blendshapedata.vertexies.Count];
                Vector3[] deltanormals = new Vector3[blendshapedata.vertexies.Count];
                Vector3[] deltatangnents = new Vector3[blendshapedata.vertexies.Count];

                GetDeltaArrays(mesh, vertex.deltaVertices, vertex.deltaNormals, vertex.deltaTangents, out deltavertices, out deltanormals, out deltatangnents);

                mesh.AddBlendShapeFrame(name, weight, deltavertices, deltanormals, deltatangnents);
            }
        }

        /// <summary>
        /// 引用 : https://qiita.com/Milcia/items/ff7d9e1dffa28004efb7
        /// </summary>
        /// <param name="targetObj"></param>
        /// <returns></returns>
        public string GetHierarchyPath(GameObject targetObj)
        {
            List<GameObject> objPath = new List<GameObject>();
            objPath.Add(targetObj);
            for (int i = 0; objPath[i].transform.parent != null; i++)
                objPath.Add(objPath[i].transform.parent.gameObject);
            string path = objPath[objPath.Count - 2].gameObject.name; //今回の場合avatar(先頭のオブジェクトが不要)なのでCount - 2にする。必要な場合は - 1 に変更
            for (int i = objPath.Count - 3; i >= 0; i--) //こっちもCount - 3にする。必要な場合は - 2にする
                path += "/" + objPath[i].gameObject.name;

            return path;
        }

        public GameObject GetGameObject()
        {
            return this.gameObject;
        }

        public void ChangeField(string blendshapeName, string path)
        {
            blendshapedata.name = blendshapeName;

        }

        public void OnValidate()
        {
            blendshapedata = JsonUtils.FromJson(json);
        }
    }
}
