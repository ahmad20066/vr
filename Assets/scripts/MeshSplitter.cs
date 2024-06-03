using UnityEngine;
using System.Collections.Generic;

public class MeshSplitter : MonoBehaviour
{
    [Range(0.0f, 0.1f)]
    public float mergeThreshold = 0.01f;

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

        GameObject triangleParent = new GameObject(gameObject.name + "_TrianglePieces");
        triangleParent.transform.position = transform.position;
        triangleParent.transform.rotation = transform.rotation;
        triangleParent.transform.localScale = transform.localScale;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int vertexIndex1 = triangles[i];
            int vertexIndex2 = triangles[i + 1];
            int vertexIndex3 = triangles[i + 2];

            Vector3 vertex1 = vertices[vertexIndex1];
            Vector3 vertex2 = vertices[vertexIndex2];
            Vector3 vertex3 = vertices[vertexIndex3];

            vertex1 = transform.TransformPoint(vertex1);
            vertex2 = transform.TransformPoint(vertex2);
            vertex3 = transform.TransformPoint(vertex3);

            Mesh triangleMesh = new Mesh();
            triangleMesh.vertices = new Vector3[] { vertex1, vertex2, vertex3 };
            triangleMesh.triangles = new int[] { 0, 1, 2 };

            GameObject triangleObject = new GameObject("Triangle_" + (i / 3));
            triangleObject.transform.SetParent(triangleParent.transform);

            MeshFilter triangleMeshFilter = triangleObject.AddComponent<MeshFilter>();
            triangleMeshFilter.mesh = triangleMesh;

            MeshRenderer triangleMeshRenderer = triangleObject.AddComponent<MeshRenderer>();
            triangleMeshRenderer.material = GetComponent<MeshRenderer>().material;

            triangleObject.transform.position = (vertex1 + vertex2 + vertex3) / 3.0f;

        }

        Debug.Log("Model sliced into individual triangle pieces.");
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
