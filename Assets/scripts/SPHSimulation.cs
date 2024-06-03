using UnityEngine;

public class SPHSimulation : MonoBehaviour
{
    public ComputeShader sphComputeShader;
    public Shader particleRenderShader;
    public Material particleRenderMaterial;

    public int particleCount = 1000;
    public float restDensity = 1.225f;
    public float stiffness = 1.200f;
    public float viscosity = 0.1f;
    public float particleMass = 0.001225f;
    public float gravity = 9.81f;
    public Vector3 gravityDirection = Vector3.down;
    public Vector3 windForce = new Vector3(1f, 0f, 0f);
    public float windStrength = 1;
    public float smoothingRadius = 1.2f;
    public Vector3 boxMin = new Vector3(-1f, -1f, -1f);
    public Vector3 boxMax = new Vector3(1f, 1f, 1f);
    public float pointSize = 10.0f;
    public Color particleColor = Color.white;

    private ComputeBuffer particleBuffer;
    private Particle[] particles;

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float density;
        public float pressure;
    }

    private void Start()
    {
        InitializeParticles();
        InitializeComputeShader();
        InitializeRenderShader();
    }

    private void Update()
    {
        RunComputeShader();
        RenderParticles();
    }

    private void InitializeParticles()
    {
        particles = new Particle[particleCount];

        int gridSize = Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f));
        float spacing = 0.1f;

        int index = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    if (index >= particleCount) break;

                    Vector3 initialPosition = new Vector3(
                        x * spacing - gridSize * spacing / 2,
                        y * spacing - gridSize * spacing / 2,
                        z * spacing - gridSize * spacing / 2
                    );

                    particles[index] = new Particle
                    {
                        position = initialPosition,
                        velocity = Vector3.zero,
                        density = restDensity,
                        pressure = 0
                    };

                    index++;
                }
            }
        }

        particleBuffer = new ComputeBuffer(particles.Length, sizeof(float) * 8);
        particleBuffer.SetData(particles);
    }

    private void InitializeComputeShader()
    {
        int kernelID = sphComputeShader.FindKernel("CSMain");

        sphComputeShader.SetBuffer(kernelID, "particles", particleBuffer);
        sphComputeShader.SetInt("particleCount", particleCount);
        sphComputeShader.SetFloat("restDensity", restDensity);
        sphComputeShader.SetFloat("stiffness", stiffness);
        sphComputeShader.SetFloat("viscosity", viscosity);
        sphComputeShader.SetFloat("particleMass", particleMass);
        sphComputeShader.SetFloat("gravity", gravity);
        sphComputeShader.SetVector("gravityDirection", gravityDirection);
        sphComputeShader.SetVector("windForce", windForce);
        sphComputeShader.SetFloat("windStrength", windStrength);
        sphComputeShader.SetFloat("smoothingRadius", smoothingRadius);
        sphComputeShader.SetVector("boxMin", boxMin);
        sphComputeShader.SetVector("boxMax", boxMax);
    }

    private void InitializeRenderShader()
    {
        // particleRenderMaterial = new Material(particleRenderShader);
        particleRenderMaterial.SetFloat("_PointSize", pointSize);
        particleRenderMaterial.SetColor("_Color", particleColor);
    }

    private void RunComputeShader()
    {
        int kernelID = sphComputeShader.FindKernel("CSMain");
        sphComputeShader.SetFloat("deltaTime", Time.deltaTime);

        int threadGroups = Mathf.CeilToInt(particleCount / 256.0f);
        sphComputeShader.Dispatch(kernelID, threadGroups, 1, 1);

        // Retrieve updated particle data from compute shader
        particleBuffer.GetData(particles);
        for (int i = 0; i < Mathf.Min(particleCount, 5); i++)
        {
            Debug.Log($"Particle {i}: {particles[i].position}");
        }
    }

    private void RenderParticles()
    {
        particleRenderMaterial.SetPass(0);
        particleRenderMaterial.SetBuffer("particles", particleBuffer);

        // Set view projection matrix
        Camera cam = Camera.main;
        Matrix4x4 viewProj = cam.projectionMatrix * cam.worldToCameraMatrix;
        particleRenderMaterial.SetMatrix("uViewProjection", viewProj);

        Graphics.DrawProceduralNow(MeshTopology.Points, particleCount);

    }

    private void OnDestroy()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }
    }

    private void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = Color.white;
        foreach (var particle in particles)
        {
            Gizmos.DrawSphere(particle.position, 0.05f); // Adjust size as needed
        }
    }
}
