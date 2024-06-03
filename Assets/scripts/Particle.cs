// Particle.cs
using UnityEngine;

public class Particle : MonoBehaviour
{
    public int id;
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 predictedPosition;
    public Vector3 predictedVelocity;
    public float density;
    public float pressure;
    public Vector3 force;

    public Particle(Vector3 initialPosition, Vector3 initialVelocity, int particleId)
    {
        id = particleId;
        position = initialPosition;
        velocity = initialVelocity;
        predictedPosition = initialPosition;
        predictedVelocity = initialVelocity;
        density = 0f;
        pressure = 0f;
        force = Vector3.zero;
    }
}
