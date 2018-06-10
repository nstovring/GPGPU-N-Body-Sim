using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferExample : MonoBehaviour {

    public ComputeBuffer inputcomputeBuffer;
    public ComputeBuffer sortcomputeBuffer;

    public ComputeShader computeShader;


    public Material mat;


    RenderTexture pointRt;
    RenderTexture velRt;
    //const int count = 10554096;

    public int count = 108096;
    const float size = 0.5f;

    public float speed = 1;
    public float angularSpeed = 0.5f;
    public float gravityMul = 1;
    struct particle
    {
        public Vector3 position;
        public Vector3 direction;
    }

	// Use this for initialization
	void Start () {

        inputcomputeBuffer = new ComputeBuffer(count, sizeof(float) * 3 * 2, ComputeBufferType.Default);
        sortcomputeBuffer = new ComputeBuffer(count, sizeof(float) * 3 * 2, ComputeBufferType.Default);

        particle[] points = GetParticlePoints();
        inputcomputeBuffer.SetData(points);




        mat.SetBuffer("computeBuffer", inputcomputeBuffer);
        //computeShader.SetTexture(0, "VolumeMap", rt);
        int mainKernelHandler = computeShader.FindKernel("CSMain");
        int mortonKernelHandler = computeShader.FindKernel("CSMorton");

        computeShader.SetBuffer(mainKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(mortonKernelHandler, "inputPoints", inputcomputeBuffer);

        //computeShader.SetBuffer(0, "returnPoints", outputcomputeBuffer);
        computeShader.Dispatch(mortonKernelHandler, count / 32, 1, 1);
        computeShader.Dispatch(mainKernelHandler, count / 32, 1, 1);

    }

  
    void Update()
    {
        DispatchShaders();
    }

    void DispatchShaders()
    {
        computeShader.SetFloat("speed", speed);
        computeShader.SetFloat("gravity", gravityMul);
        computeShader.SetFloat("angularSpeed", angularSpeed);
        computeShader.SetFloat("DeltaTime", Time.deltaTime);
        computeShader.Dispatch(0, count / 64, 1, 1);
    }

    float[] GetPoints()
    {
        float[] points = new float[count * 3];
        Random.InitState(0);

        for (int i = 0; i < count; i++)
        {
            points[i * 3 + 0] = Random.Range(0, size * 2);
            points[i * 3 + 1] = Random.Range(0, size * 2);
            points[i * 3 + 2] = 0;

        }
        return points;
    }


    Vector3[] GetVectorPoints()
    {
        Vector3[] points = new Vector3[count];
        Random.InitState(0);

        for (int i = 0; i < count; i++)
        {
            points[i] = new Vector3(Random.Range(-size, size), Random.Range(0, size * 2), 0);

        }
        return points;
    }

    particle[] GetParticlePoints()
    {
        particle[] points = new particle[count];
        Random.InitState(42);


        for (int i = 0; i < count; i++)
        {
            points[i].position = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
            points[i].direction = new Vector3(Random.Range(-size, size), Random.Range(-size, size), Random.Range(-size, size));
        }
        return points;
    }


    void ApplyParticlePositions(particle[] points)
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];
        for (int i = 0; i < count; i++)
        {
            particles[i] = new ParticleSystem.Particle();
            particles[i].remainingLifetime = 5;
            particles[i].position = points[i].position;
            particles[i].velocity = points[i].direction;
        }

    }

    void OnPostRender()
    {
        mat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Points, count, 1);
    }

    void OnDestroy()
    {
        inputcomputeBuffer.Release();
        sortcomputeBuffer.Release();
        //rt.Release();
    }
	
}
