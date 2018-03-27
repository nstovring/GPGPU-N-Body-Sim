using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSimulatorCPU : MonoBehaviour {

    // Use this for initialization
    public Transform anchor;
    public Transform thing;

    public Vector3 gravity = new Vector3(0,-0.1f,0);
    public float mass = 30;
    public Vector3 velocity;
    public float timeStep = 0.02f;
    public float stiffness = 7;
    public float damping = 2;
    void ApplyForce()
    {
        timeStep = Time.deltaTime;
        var dampingForce = damping * (-1 * velocity.normalized) * velocity.magnitude;
        var springForce = -stiffness * (thing.transform.position - anchor.transform.position);

        var force = dampingForce +springForce + mass * gravity;
        var accelerationY = force / mass;


        velocity = velocity + accelerationY * timeStep;
        thing.transform.position = thing.transform.position + velocity * timeStep;

    }

	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        ApplyForce();
	}
}
