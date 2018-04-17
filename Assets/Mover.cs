using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour {
    public Transform posA;
    public Transform posB;
    Vector3 target;
    bool Switch = false;
    Vector3 velocity;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if(posA && posB)
        {
            if (Vector3.Distance(transform.position, target) > 0.1f)
            {
                Vector3 directionVector = (target - transform.position).normalized * Time.fixedDeltaTime;
                velocity = GetVelocity(directionVector);
                Debug.DrawRay(transform.position,velocity);
                transform.position += velocity * Time.fixedDeltaTime;
            }
            else
            {
                Switch = !Switch;
                if (Switch)
                {
                    target = posA.transform.position;
                }
                else
                {
                    target = posB.position;
                }
            }
        }
	}

    Vector3 GetVelocity(Vector3 directionVector)
    {
        return transform.position - (transform.position + directionVector);
    }
}
