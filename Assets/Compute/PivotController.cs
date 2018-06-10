using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PivotController : MonoBehaviour {

    public Transform child; 
        // Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        //if (Input.GetMouseButton(0))
        //{
            float yRot = Input.mousePosition.x;
            float xRot = Input.mousePosition.y;
            Quaternion rotation = Quaternion.identity;
            rotation.eulerAngles = new Vector3(xRot, yRot, 0) * 0.5f;
            transform.rotation = rotation;

            if (child != null)
                child.localPosition += new Vector3(0, 0, Input.mouseScrollDelta.y);// Vector3.Lerp(child.localPosition, new Vector3(0, 0, Input.mouseScrollDelta.y), 0.1f);
        //}
    }
}
