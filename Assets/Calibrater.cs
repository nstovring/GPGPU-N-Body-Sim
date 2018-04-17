using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calibrater : MonoBehaviour {

    //Tracker Struct contains position and zeroAngle
    struct tracker
    {
        public Vector3 position;
        public float zeroAngle;
    }

    //Tracker Definitions
    tracker Left;
    tracker Right;

    //Unity Transform Definitions
    public Transform leftObject;
    public Transform rightObject;

	// Use this for initialization
	void Start () {
        Left.position = leftObject.position;
        Right.position = rightObject.position;
	}

    //Calculates the angle between to vectors
    float GetAngle(Vector3 pos, Vector3 origin)
    {

        Vector3 direction = pos - origin;
        float angle = Mathf.Acos(dot(Vector3.up, normalize(direction)));
        return angle * Mathf.Rad2Deg;
    }

    //Normalizes The Vector
    Vector3 normalize(Vector3 x)
    {
        return x / magnitude(x, x);
    }

    //Calculates the dot product
    float dot(Vector3 v1, Vector3 v2)
    {
        return (v1.x * v2.x + v1.y * v2.y + v1.z * v2.z);
    }

    //Returns the length of a vector
    float magnitude(Vector3 x, Vector3 y)
    {
        return Mathf.Sqrt(dot(x,y));
    }

  

    //Calculates the angle between the position of the input tracker and the center object subtracted from the zero angle
    float GetZeroAngle(tracker i)
    {
        return i.zeroAngle - GetAngle(i.position, transform.position);
    }

    //Assigns the transform values to the tracker structs and recalculates the angle
    void UpdateAngles()
    {
        Left.position = leftObject.position;
        Right.position = rightObject.position;
        leftAngle = GetZeroAngle(Left);
        rightAngle = GetZeroAngle(Right);
    }

    bool calibrated = false;
    public float leftAngle;
    public float rightAngle;

    float timer = 0;
    float rightAngles;
    float leftAngles;
    int counter = 0;
    //Calibartion Ienumerator used for calibration
    IEnumerator Calibrator()
    {
        while (true)
        {
            //Only run this statement if 5 seconds have not passed
            if (timer < 5f)
            {
                //Sum up all the angles between the trackers and the center
                rightAngles += GetAngle(rightObject.position, transform.position);
                leftAngles += GetAngle(leftObject.position, transform.position);
                //Count summations
                counter++;
                timer += Time.deltaTime;
            }
            //if five seconds have passed
            if (timer >= 5f)
            {
                Debug.Log("Calibration done");
                //Get the mean angle by dividing by the amount of summations
                Left.zeroAngle = leftAngles / counter;
                Right.zeroAngle = rightAngles / counter;
                calibrated = true;
                //Break the while loop to stop the Ienumerator
                break;
            }
            yield return new WaitForEndOfFrame();
        }
    }

	void Update () {
        //When calibrated the user can move the right and left trackers up and down with QA, ED
        if (calibrated)
        {
            UpdateAngles();
            if (Input.GetKeyDown(KeyCode.D))
            {
                rightObject.transform.position += new Vector3(0, 1f, 0) * Time.deltaTime;
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                leftObject.transform.position += new Vector3(0, 1f, 0) * Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                rightObject.transform.position -= new Vector3(0, 1f, 0) * Time.deltaTime;
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                leftObject.transform.position -= new Vector3(0, 1f, 0) * Time.deltaTime;
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            StartCoroutine(Calibrator());
            Debug.Log("Calibrating");
        }
    }
}
