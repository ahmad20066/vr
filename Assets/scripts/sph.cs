using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class sph : MonoBehaviour
{
    List<Particle> particles = new List<Particle>();


    public GameObject spherePrefab;
    public float spawnRadius = 5f;
    void Start()
    {
        Debug.Log("start");
        for (int i = 0; i < 100; i++)
        {
            Vector3 randomPosition = UnityEngine.Random.insideUnitSphere * spawnRadius;


            GameObject sphereObject = Instantiate(spherePrefab, randomPosition, Quaternion.identity);

            Particle particleComponent = sphereObject.AddComponent<Particle>();

            float velocityRange = 0.1f; //
            particleComponent.pressure = 0f;
            particleComponent.density = 0f;
            particleComponent.viscosity = new Vector3(
                UnityEngine.Random.Range(-velocityRange, velocityRange),
                UnityEngine.Random.Range(-velocityRange, velocityRange),
                UnityEngine.Random.Range(-velocityRange, velocityRange)
            ); ;
            particleComponent.mass = 1f;
            particleComponent.currentForce = Vector3.zero;
            particleComponent.velocity = new Vector3(
                UnityEngine.Random.Range(-velocityRange, velocityRange),
                UnityEngine.Random.Range(-velocityRange, velocityRange),
                UnityEngine.Random.Range(-velocityRange, velocityRange)
            );

            particleComponent.position = sphereObject.transform.position;
            particles.Add(particleComponent);
            sphereObject.transform.position = particleComponent.position;
            particleComponent.sphere = sphereObject;
        }
    }
    float convertDensityToPressure(float density)
    {
        float REST_DENSITY = 1.225f;
        float gasConstant = 8.314f;


        float pressure = gasConstant * (density - REST_DENSITY);

        return pressure;
    }
    // Update is called once per frame
    void Update()
    {
        calculateDensityAndPressure();

        foreach (Particle particle in particles)
        {
            // Debug.Log(particle.density);
            Vector3 totalForce = calculateTotalForce(particle);


            // particle.velocity += totalForce * Time.deltaTime;
            // Debug.Log(particle.velocity);
            particle.position += particle.velocity * Time.deltaTime;
            // Debug.Log(particle.position);
            // particle.position += ;
            particle.sphere.transform.position = particle.position;

        }

    }
    static float smoothingKernel(float radius, float distance)
    {
        float volume = ((float)(Math.PI * Math.Pow(radius, 8) / 4));
        float value = Math.Max(0, radius * radius - distance * distance);
        return value * value * value;
    }

    void calculateDensityAndPressure()
    {
        foreach (Particle particle in particles)
        {
            particle.density = 0;
            foreach (Particle particle1 in particles)
            {
                float distance = (particle.position - particle1.position).magnitude;
                // Debug.Log("*******" + distance);
                // if (distance < 20)
                // {
                particle.density = particle1.mass * StdKernel(distance * distance);

                // }
            }
            particle.pressure = convertDensityToPressure(particle.density);
            // Debug.Log("-------" + particle.pressure);
        }
    }


    Vector3 calculateTotalForce(Particle particle)
    {
        Vector3 totalForce = Vector3.zero;

        foreach (Particle neighbour in particles)
        {
            float distance = (particle.position - neighbour.position).magnitude;
            Vector3 direction = (neighbour.position - particle.position).normalized;

            // // Calculate pressure force
            Vector3 pressureForce = -neighbour.mass * (particle.pressure + neighbour.pressure) / (2 * neighbour.density) * StdKernel(distance * distance) * direction;

            // // Calculate viscosity force
            Vector3 velocityDifference = neighbour.velocity - particle.velocity;

            Vector3 viscosityForce = neighbour.viscosity.magnitude * (neighbour.mass / neighbour.density) * velocityDifference * StdKernel(distance * distance);

            totalForce += pressureForce + viscosityForce;
        }

        return totalForce;
    }
    // Kernel by Müller et al.
    private float StdKernel(float distanceSquared)
    {
        // Doyub Kim
        float x = 1.0f - distanceSquared / 20;
        return 315f / (64f * Mathf.PI * 20) * x * x * x;
    }


}
