using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class moves the camera depending on how far away the two transform objects are from each other
public class CameraMover : MonoBehaviour {
    public Transform followPoint1;
    public Transform followPoint2;
	// Use this for initialization
	void Start () {
        //The rest distance is the distance the camera wants the objects to remain from each other in clip space
       restDistance = Vector3.Distance(Camera.main.WorldToScreenPoint(followPoint1.position), Camera.main.WorldToScreenPoint(followPoint2.position));
    }
    public float restDistance = 0;
    Vector3 averagePos;
    float distanceDelta;
    Vector3 lookDirection;
	// Update is called once per frame
	void Update () {
        //The position between the two objects
        averagePos = (followPoint1.position + followPoint2.position) / 2;
        //Definition of the direction the camera looks at
        lookDirection = transform.position - averagePos;
        //Definition of a quaternion which points in that direction
        Quaternion lookQuat = new Quaternion();
        lookQuat = Quaternion.LookRotation(-lookDirection);
        //Apply the quaternion using sphericla interpolation
        transform.rotation = Quaternion.Slerp(transform.rotation, lookQuat, 0.1f);
        //Calculate the distance delta which is the how far the objects currently are from each other relative to the restDistance
        distanceDelta = Vector3.Distance(Camera.main.WorldToScreenPoint(followPoint1.position), Camera.main.WorldToScreenPoint(followPoint2.position)) - restDistance;
        //Apply movement
        transform.position += lookDirection.normalized * distanceDelta * Time.deltaTime;
    }
}
