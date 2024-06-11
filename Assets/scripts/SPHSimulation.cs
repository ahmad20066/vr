using UnityEngine;
using System.Collections.Generic;

public class SPHSimulation : MonoBehaviour
{
    public int particleCount = 1000;
    public float restDensity = 1.225f;
    public float stiffness = 1.200f;
    public float viscosity = 0.1f;
    public float smoothingRadius = 0.7f;
    public float particleMass = 0.001225f;
    public float gravity = 9.81f;
    public Vector3 gravityDirection = Vector3.down;

    public GameObject model;
    public Vector3 boxSize = new Vector3(1f, 1f, 1f);

    public Vector3 windForce = new Vector3(1f, 0f, 0f);
    public float windStrength = 10f; // Increased wind strength

    public ComputeShader sphComputeShader;
    public Material particleMaterial;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer nodeCenterBuffer;
    private ComputeBuffer nodeSizeBuffer;
    private ComputeBuffer nodeObjectCountBuffer;
    private ComputeBuffer nodeObjectBuffer;

    private Octree<int> octree;
    private KDTree kdTree;
    private Mesh simplifiedMesh;
    private Vector3[] vertices;
    private int[] triangles;

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float density;
        public float pressure;
    }

    struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
    }

    private Particle[] particles;
    float startTime;

    private void Start()
    {
        startTime = Time.time;
        InitializeParticles();
        InitializeOctree();
        InitializeKDTree();
        InitializeComputeShader();
    }

    private void Update()
    {
        if (Time.time - startTime > 5f) // Check if 10 seconds have passed
        {
            RunComputeShader();
            HandleCollisions();
        }
    }

    private void InitializeParticles()
    {
        particles = new Particle[particleCount];

        // Initialize particles at one end of the tunnel
        Vector3 initialClusterCenter = new Vector3(-boxSize.x / 2 + 0.1f, 0, 0); // Start slightly inside the box
        float clusterRadius = 0.05f; // Small radius for tight clustering

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * clusterRadius;
            Vector3 initialPosition = initialClusterCenter + randomOffset;
            Vector3 initialVelocity = Vector3.zero; // Start with zero velocity

            particles[i] = new Particle
            {
                position = initialPosition,
                velocity = initialVelocity,
                density = restDensity,
                pressure = 0
            };
        }
    }

    private void InitializeComputeShader()
    {
        particleBuffer = new ComputeBuffer(particles.Length, sizeof(float) * 8);
        particleBuffer.SetData(particles);

        Vector3[] centers, sizes;
        int[] objectCounts, objects;
        octree.GetOctreeData(out centers, out sizes, out objectCounts, out objects);

        nodeCenterBuffer = new ComputeBuffer(centers.Length, sizeof(float) * 3);
        nodeCenterBuffer.SetData(centers);

        nodeSizeBuffer = new ComputeBuffer(sizes.Length, sizeof(float) * 3);
        nodeSizeBuffer.SetData(sizes);

        nodeObjectCountBuffer = new ComputeBuffer(objectCounts.Length, sizeof(int));
        nodeObjectCountBuffer.SetData(objectCounts);

        nodeObjectBuffer = new ComputeBuffer(objects.Length, sizeof(int));
        nodeObjectBuffer.SetData(objects);

        Triangle[] triangleData = new Triangle[triangles.Length / 3];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            triangleData[i / 3] = new Triangle
            {
                v0 = model.transform.TransformPoint(vertices[triangles[i]]),
                v1 = model.transform.TransformPoint(vertices[triangles[i + 1]]),
                v2 = model.transform.TransformPoint(vertices[triangles[i + 2]])
            };
        }

        triangleBuffer = new ComputeBuffer(triangleData.Length, sizeof(float) * 9);
        triangleBuffer.SetData(triangleData);
    }

    private void InitializeOctree()
    {
        Mesh modelMesh = model.GetComponent<MeshFilter>().mesh;
        simplifiedMesh = SimplifyMesh(modelMesh, 0.001f);

        vertices = simplifiedMesh.vertices;
        triangles = simplifiedMesh.triangles;

        Debug.Log($"Original triangle count: {modelMesh.triangles.Length / 3}");
        Debug.Log($"Simplified triangle count: {triangles.Length / 3}");

        Bounds modelBounds = simplifiedMesh.bounds;
        modelBounds.center = model.transform.position;
        modelBounds.size = Vector3.Scale(modelBounds.size, model.transform.localScale);

        octree = new Octree<int>(modelBounds, 8, 4);

        Vector3[] transformedVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            transformedVertices[i] = model.transform.TransformPoint(vertices[i]);
        }

        System.Threading.Tasks.Parallel.For(0, triangles.Length / 3, i =>
        {
            int index = i * 3;
            Vector3 v0 = transformedVertices[triangles[index]];
            Vector3 v1 = transformedVertices[triangles[index + 1]];
            Vector3 v2 = transformedVertices[triangles[index + 2]];

            Bounds triangleBounds = new Bounds(v0, Vector3.zero);
            triangleBounds.Encapsulate(v1);
            triangleBounds.Encapsulate(v2);

            lock (octree)
            {
                octree.Insert(index, triangleBounds);
            }
        });
    }

    private void InitializeKDTree()
    {
        Vector3[] particlePositions = new Vector3[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            particlePositions[i] = particles[i].position;
        }

        kdTree = new KDTree(particlePositions);
    }

    private void RunComputeShader()
    {
        Vector3 boxMin = -boxSize / 2;
        Vector3 boxMax = boxSize / 2;
        int kernelID = sphComputeShader.FindKernel("CSMain");

        sphComputeShader.SetBuffer(kernelID, "particles", particleBuffer);
        sphComputeShader.SetBuffer(kernelID, "triangles", triangleBuffer);
        sphComputeShader.SetInt("particleCount", particleCount);
        sphComputeShader.SetInt("triangleCount", triangleBuffer.count);
        sphComputeShader.SetFloat("restDensity", restDensity);
        sphComputeShader.SetFloat("stiffness", stiffness);
        sphComputeShader.SetFloat("viscosity", viscosity);
        sphComputeShader.SetFloat("particleMass", particleMass);
        sphComputeShader.SetFloat("gravity", gravity);
        sphComputeShader.SetVector("gravityDirection", gravityDirection);
        sphComputeShader.SetVector("windForce", windForce);
        sphComputeShader.SetFloat("windStrength", windStrength);
        sphComputeShader.SetFloat("smoothingRadius", smoothingRadius);
        sphComputeShader.SetFloat("deltaTime", Time.deltaTime);
        sphComputeShader.SetVector("boxMin", boxMin);
        sphComputeShader.SetVector("boxMax", boxMax);

        int threadGroups = Mathf.CeilToInt(particleCount / 256.0f);
        sphComputeShader.Dispatch(kernelID, threadGroups, 1, 1);

        particleBuffer.GetData(particles);
    }

    private void HandleCollisions()
    {
        int kernelID = sphComputeShader.FindKernel("HandleCollisions");

        sphComputeShader.SetBuffer(kernelID, "particles", particleBuffer);
        sphComputeShader.SetBuffer(kernelID, "triangles", triangleBuffer);
        sphComputeShader.SetInt("particleCount", particleCount);
        sphComputeShader.SetInt("triangleCount", triangleBuffer.count);
        sphComputeShader.SetFloat("smoothingRadius", smoothingRadius);
        sphComputeShader.SetBuffer(kernelID, "triangles", triangleBuffer);
        sphComputeShader.SetBuffer(kernelID, "nodeCenters", nodeCenterBuffer);
        sphComputeShader.SetBuffer(kernelID, "nodeSizes", nodeSizeBuffer);
        sphComputeShader.SetBuffer(kernelID, "nodeObjectCounts", nodeObjectCountBuffer);
        sphComputeShader.SetBuffer(kernelID, "nodeObjects", nodeObjectBuffer);
        int threadGroups = Mathf.CeilToInt(particleCount / 256.0f);
        sphComputeShader.Dispatch(kernelID, threadGroups, 1, 1);

        particleBuffer.GetData(particles);
    }

    private void CheckPotentialParticles(Vector3 particlePosition)
    {
        List<int> potentialParticles = kdTree.RangeQuery(particlePosition, smoothingRadius);
        Debug.Log($"Number of potential particles: {potentialParticles.Count}");
    }

    private void OnDestroy()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }

        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
        }

        if (nodeCenterBuffer != null)
        {
            nodeCenterBuffer.Release();
        }

        if (nodeSizeBuffer != null)
        {
            nodeSizeBuffer.Release();
        }

        if (nodeObjectCountBuffer != null)
        {
            nodeObjectCountBuffer.Release();
        }

        if (nodeObjectBuffer != null)
        {
            nodeObjectBuffer.Release();
        }
    }


    private Mesh SimplifyMesh(Mesh mesh, float threshold)
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

    private void OnDrawGizmos()
    {
        if (simplifiedMesh != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = model.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = model.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = model.transform.TransformPoint(vertices[triangles[i + 2]]);

                Gizmos.DrawLine(v0, v1);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v0);
            }
        }

        // Draw bounding box
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Draw particles
        // if (particles != null)
        // {
        //     Gizmos.color = Color.red;
        //     foreach (var particle in particles)
        //     {
        //         Gizmos.DrawSphere(particle.position, 0.01f); // Adjust the size as needed
        //     }
        // }
    }

    private void OnRenderObject()
    {
        // Set the material
        particleMaterial.SetPass(0);

        // Set the particle buffer
        particleMaterial.SetBuffer("particles", particleBuffer);

        // Draw the particles
        Graphics.DrawProceduralNow(MeshTopology.Points, particleCount);
    }
}
