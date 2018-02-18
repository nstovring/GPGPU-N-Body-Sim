using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;
public class ParticleRaymarcher : MonoBehaviour {
    public Material mat;
	// Use this for initialization
	void Start () {
		
	}

    void OnRenderImage(RenderTexture input, RenderTexture destination)
    {
        Graphics.Blit(input, destination, mat);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
