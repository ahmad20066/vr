using UnityEngine;
using System.Collections.Generic;

public class MeshSplitter : MonoBehaviour
{
    [Range(0.0f, 0.1f)]
    public float mergeThreshold = 0.01f;
    public ComputeShader sphComputeShader;

    public ComputeBuffer triangleBuffer;

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter not found on " + gameObject.name);
            return;
        }

        Mesh mesh = meshFilter.mesh;
        Mesh simplifiedMesh = SimplifyMesh(mesh, mergeThreshold);

        Vector3[] vertices = simplifiedMesh.vertices;
        int[] triangles = simplifiedMesh.triangles;

        List<Vector3> triangleData = new List<Vector3>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int vertexIndex1 = triangles[i];
            int vertexIndex2 = triangles[i + 1];
            int vertexIndex3 = triangles[i + 2];

            Vector3 vertex1 = vertices[vertexIndex1];
            Vector3 vertex2 = vertices[vertexIndex2];
            Vector3 vertex3 = vertices[vertexIndex3];

            // Scale vertices by 0.5
            vertex1 *= 0.5f;
            vertex2 *= 0.5f;
            vertex3 *= 0.5f;

            triangleData.Add(vertex1);
            triangleData.Add(vertex2);
            triangleData.Add(vertex3);
        }

        triangleBuffer = new ComputeBuffer(triangleData.Count, sizeof(float) * 3);
        triangleBuffer.SetData(triangleData.ToArray());
        sphComputeShader.SetBuffer(0, "triangles", triangleBuffer);
        sphComputeShader.SetInt("triangleCount", triangleData.Count / 3);

        Debug.Log("Model sliced into individual triangle pieces.");
        Debug.Log(triangleData.Count);
    }

    void OnDestroy()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
        }
    }

    Mesh SimplifyMesh(Mesh mesh, float threshold)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        List<Vector3> simplifiedVertices = new List<Vector3>();
        List<int> simplifiedIndices = new List<int>();
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        for (int i = 0; i < vertices.Length; i++)
        {
            bool merged = false;
            for (int j = 0; j < simplifiedVertices.Count; j++)
            {
                if (Vector3.Distance(vertices[i], simplifiedVertices[j]) < threshold)
                {
                    vertexMap[i] = j;
                    merged = true;
                    break;
                }
            }
            if (!merged)
            {
                vertexMap[i] = simplifiedVertices.Count;
                simplifiedVertices.Add(vertices[i]);
            }
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int newVertexIndex1 = vertexMap[triangles[i]];
            int newVertexIndex2 = vertexMap[triangles[i + 1]];
            int newVertexIndex3 = vertexMap[triangles[i + 2]];

            if (newVertexIndex1 != newVertexIndex2 && newVertexIndex1 != newVertexIndex3 && newVertexIndex2 != newVertexIndex3)
            {
                simplifiedIndices.Add(newVertexIndex1);
                simplifiedIndices.Add(newVertexIndex2);
                simplifiedIndices.Add(newVertexIndex3);
            }
        }

        Mesh simplifiedMesh = new Mesh();
        simplifiedMesh.vertices = simplifiedVertices.ToArray();
        simplifiedMesh.triangles = simplifiedIndices.ToArray();
        simplifiedMesh.RecalculateNormals();

        return simplifiedMesh;
    }
}
