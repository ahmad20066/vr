using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public float pressure;
    public float density;
    public Vector3 viscosity;

    public float mass;
    public Vector3 currentForce;
    public Vector3 velocity;
    public Vector3 position;
    public GameObject sphere;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        // Debug.Log(position);
        // GetComponentInParent<Transform>().position = position;
        // Debug.Log("******");
        // Debug.Log(GetComponentInParent<Transform>().position);
        // transform.position = position;
    }

}