using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManipulator : MonoBehaviour {

    public int TextureResolution = 256;
    public ParticleSystem particleSystem;
    public ComputeShader shader, shaderCopy;


    Renderer rend;
    RenderTexture myRt, myRtCopy;
	// Use this for initialization
	void Start () {
        myRt = new RenderTexture(TextureResolution, TextureResolution, 0);
        myRt.enableRandomWrite = true;
        myRt.Create();

        myRtCopy = new RenderTexture(TextureResolution, TextureResolution, 0);
        myRtCopy.enableRandomWrite = true;
        myRtCopy.Create();

        int shaderKernel = shader.FindKernel("CSMain");
        shader.SetTexture(shaderKernel, "tex", myRt);
        shader.Dispatch(shaderKernel, TextureResolution / 8, TextureResolution / 8, 1);

        int shaderCopyKernel = shaderCopy.FindKernel("CSMain");

        shaderCopy.SetTexture(shaderCopyKernel, "tex", myRt);
        shaderCopy.SetTexture(shaderCopyKernel, "texCopy", myRtCopy);
        shaderCopy.Dispatch(shaderCopyKernel, TextureResolution / 8, TextureResolution / 8, 1);

    }

    void OnGUI()
    {
        int w = Screen.width / 2;
        int h = Screen.height / 2;
        int s = 512;
        GUI.DrawTexture(new Rect(w - s / 2, h - s / 2, s, s), myRtCopy);
    }

    void OnDestroy()
    {
        myRt.Release();
        myRtCopy.Release();
    }

    void UpdateParticlesFromCompute()
    {
        int shaderKernel = shader.FindKernel("CSMain");
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[TextureResolution* TextureResolution];
        particleSystem.GetParticles(particles);
        //qparticles[1].
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
