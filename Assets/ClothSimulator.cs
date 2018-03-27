using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSimulator : MonoBehaviour {

    public ComputeShader clothComputeShader;
    public int count = 64;
    int mainClothKernelHandler;
    int springKernelHandler;

    [Range(0, 63)]
    public int selectedParticle = 0;

    ComputeBuffer particleBuffer;
    ComputeBuffer springBuffer;

    int springCount;
    public float stiffness;
    public float damping;
    public float mass;
    public Transform clothHandler;
    // Use this for initialization
    void Start () {
        mainClothKernelHandler = clothComputeShader.FindKernel("CSMain");
        springKernelHandler = clothComputeShader.FindKernel("CSSprings");
        int particleStructSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpringHandler.particle));
        int springStructSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpringHandler.spring));


        SpringHandler.particle[] particles = new SpringHandler.particle[count];
        List<SpringHandler.spring> springs = new List<SpringHandler.spring>();

        int rows = (int) Mathf.Sqrt(count);

        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].structuralSprings.x = -1;
            particles[i].structuralSprings.y = -1;
            particles[i].structuralSprings.z = -1;
            particles[i].structuralSprings.w = -1;
            particles[i].shearSprings.x = -1;
            particles[i].shearSprings.y = -1;
            particles[i].shearSprings.z = -1;
            particles[i].shearSprings.w = -1;
            particles[i].bendingSprings.x = -1;
            particles[i].bendingSprings.y = -1;
            particles[i].bendingSprings.z = -1;
            particles[i].bendingSprings.w = -1;
        }

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                particles[y + x * rows].mass = 1;
                particles[y + x * rows].iD = y + x * rows;
                particles[y + x * rows].position = new Vector3(x/ 20, y/ 20, 0);
                particles[y + x * rows].isFixed = 0;

                AddSprings(x, y, rows, particles,ref springs);
            }
        }
        particles[0].isFixed = 1;
        particles[(rows * (rows -1))].isFixed = 1;

        //Print("Links", particles);

        particleBuffer = new ComputeBuffer(count, particleStructSize, ComputeBufferType.Default);
        springBuffer = new ComputeBuffer(springs.Count, springStructSize, ComputeBufferType.Default);

        springCount = springs.Count;

        particleBuffer.SetData(particles);
        springBuffer.SetData(springs.ToArray());

        clothComputeShader.SetBuffer(mainClothKernelHandler, "particles", particleBuffer);
        clothComputeShader.SetBuffer(mainClothKernelHandler, "springs", springBuffer);
        clothComputeShader.SetBuffer(springKernelHandler, "particles", particleBuffer);
        clothComputeShader.SetBuffer(springKernelHandler, "springs", springBuffer);

    }

    void AddSprings(int x, int y, int rows, SpringHandler.particle[] particles,ref List<SpringHandler.spring> springs)
    {
        if (y < rows - 1)
        {
            particles[y + x * rows].structuralSprings.x = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows,out connectedParticleIndex, 0,1));
            particles[connectedParticleIndex].structuralSprings.w = springs.Count-1;
        }

        if (x < rows - 1)
        {
            particles[y + x * rows].structuralSprings.y = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows,out connectedParticleIndex, 1, 0));
            particles[connectedParticleIndex].structuralSprings.z = springs.Count-1;
        }

        if (y < rows - 1 && x < rows - 1)
        {
            particles[y + x * rows].shearSprings.x = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 1, 1));
            particles[connectedParticleIndex].shearSprings.z = springs.Count - 1;
        }
        
        if (y < rows - 1 && x > 0)
        {
            particles[y + x * rows].shearSprings.y = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, -1, 1));
            particles[connectedParticleIndex].shearSprings.w = springs.Count - 1;
        }

        //if (y < rows - 2 && x > 1)
        //{
        //    particles[y + x * rows].bendingSprings.x = springs.Count;
        //    int connectedParticleIndex;
        //    springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, -2, 2));
        //    particles[connectedParticleIndex].bendingSprings.z = springs.Count - 1;
        //}
        //
        //if (y < rows - 2 && x < rows - 2)
        //{
        //    particles[y + x * rows].bendingSprings.y = springs.Count;
        //    int connectedParticleIndex;
        //    springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 2, 2));
        //    particles[connectedParticleIndex].bendingSprings.w = springs.Count - 1;
        //}
        
        if (y < rows - 2)
        {
            particles[y + x * rows].bendingSprings.x = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 0, 2));
            particles[connectedParticleIndex].bendingSprings.z = springs.Count - 1;
        }
        
        if (x < rows - 2)
        {
            particles[y + x * rows].bendingSprings.y = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 2, 0));
            particles[connectedParticleIndex].bendingSprings.w = springs.Count - 1;
        }

    }
    private void OnDestroy()
    {
        particleBuffer.Release();
        springBuffer.Release();
    }

    // Update is called once per frame
   

    bool IsValidSpring(int x)
    {
        if (x != -1)
            return true;
        return false;
    }

    void DrawSelectedParticle(SpringHandler.particle[] ps, SpringHandler.spring[] ss)
    {
        if (IsValidSpring(ps[selectedParticle].structuralSprings.x))
            DrawSpring(ss[ps[selectedParticle].structuralSprings.x], ref ps);

        if (IsValidSpring(ps[selectedParticle].structuralSprings.y))
            DrawSpring(ss[ps[selectedParticle].structuralSprings.y], ref ps);

        if (ps[selectedParticle].structuralSprings.z != -1)
            DrawSpring(ss[ps[selectedParticle].structuralSprings.z], ref ps);

        if (ps[selectedParticle].structuralSprings.w != -1)
            DrawSpring(ss[ps[selectedParticle].structuralSprings.w], ref ps);
    }

    void FixedUpdate () {

        if(clothHandler != null)
        {
            clothComputeShader.SetVector("fixedPos", clothHandler.transform.position);
        }
        clothComputeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        clothComputeShader.SetFloat("stiffness", stiffness);
        clothComputeShader.SetFloat("damping", damping);
        clothComputeShader.SetFloat("mass", mass);

        clothComputeShader.Dispatch(mainClothKernelHandler, count / 8, 1, 1);

        SpringHandler.particle[] ps = new SpringHandler.particle[count];
        SpringHandler.spring[] ss = new SpringHandler.spring[springCount];
        particleBuffer.GetData(ps);
        springBuffer.GetData(ss);

        //ApplyForce(ref ps, ss);
        if (drawCloth)
        {
            DrawConnections(ps, ss);
            DrawVelocity(ps);
        }

        if (printDebug)
        {
            Print("Velocities", ps);
            printDebug = false;
        }

        particleBuffer.SetData(ps);

    }
    public bool printDebug = false;
    public bool drawCloth = true;
    void ApplyForce(ref SpringHandler.particle[] ps, SpringHandler.spring[] ss)
    {
        for (int i = 0; i < count; i++)
        {
            SpringHandler.particle p = ps[i];
            if (!(p.isFixed == 1))
            {
                Vector3 gravity = new Vector3(0, -5f, 0);
                float mass = p.mass;
                Vector3 dampingForce = new Vector3(0, 0, 0);
                Vector3 springForce = new Vector3(0, 0, 0);
                Vector3 tempSpringForce = new Vector3(0, 0, 0);
                Vector3 tempdampingForce = new Vector3(0, 0, 0);
        
                if (ps[i].structuralSprings.x != -1) {
                    SpringHandler.GetSpringForce(ref ss,ref ps,out tempdampingForce, out tempSpringForce, ps[i], ps[i].structuralSprings.x);
                    dampingForce += tempdampingForce;
                    springForce += tempSpringForce;
                }
        
                if (ps[i].structuralSprings.y != -1) {
                    SpringHandler.GetSpringForce(ref ss, ref ps, out tempdampingForce, out tempSpringForce, ps[i], ps[i].structuralSprings.y);
                    dampingForce += tempdampingForce;
                    springForce += tempSpringForce;
                }
        
                if (ps[i].structuralSprings.z != -1) {
                    SpringHandler.GetSpringForce(ref ss, ref ps, out tempdampingForce, out tempSpringForce, ps[i], ps[i].structuralSprings.z);
                    dampingForce += tempdampingForce;
                    springForce += tempSpringForce;
                }
        
                if (ps[i].structuralSprings.w != -1) {
                    SpringHandler.GetSpringForce(ref ss, ref ps, out tempdampingForce, out tempSpringForce, ps[i], ps[i].structuralSprings.w);
                    dampingForce += tempdampingForce;
                    springForce += tempSpringForce;
                }
        
                Vector3 force = dampingForce + springForce + mass * gravity;
                Vector3 acceleration = force / mass;
        
        
                p.velocity = p.velocity + acceleration * Time.deltaTime;
                p.position = p.position + p.velocity * Time.deltaTime;
        
                ps[i].position = p.position;
                ps[i].velocity = p.velocity;
        
            }
        
        }
        
        
    }

    void DrawConnections(SpringHandler.particle[] ps, SpringHandler.spring[] ss)
    {
        for (int i = 0; i < count; i++)
        {
            if (IsValidSpring(ps[i].structuralSprings.x))
                DrawSpring(ss[ps[i].structuralSprings.x], ref ps);
            
            if (IsValidSpring(ps[i].structuralSprings.y))
                DrawSpring(ss[ps[i].structuralSprings.y], ref ps);
            
            if (IsValidSpring(ps[i].structuralSprings.z))
                DrawSpring(ss[ps[i].structuralSprings.z], ref ps);
            
            if (IsValidSpring(ps[i].structuralSprings.w))
                DrawSpring(ss[ps[i].structuralSprings.w], ref ps);
            
            if (IsValidSpring(ps[i].shearSprings.x))
                DrawSpring(ss[ps[i].shearSprings.x], ref ps, Color.green);
            
            if (IsValidSpring(ps[i].shearSprings.y))
                DrawSpring(ss[ps[i].shearSprings.y], ref ps, Color.green);
            
            if (IsValidSpring(ps[i].shearSprings.z))
                DrawSpring(ss[ps[i].shearSprings.z], ref ps, Color.green);
            
            if (IsValidSpring(ps[i].shearSprings.w))
                DrawSpring(ss[ps[i].shearSprings.w], ref ps, Color.green);

            //if (IsValidSpring(ps[i].bendingSprings.x))
            //    DrawSpring(ss[ps[i].bendingSprings.x], ref ps, Color.red);
            //
            //if (IsValidSpring(ps[i].bendingSprings.y))
            //    DrawSpring(ss[ps[i].bendingSprings.y], ref ps, Color.red);
            //
            //if (IsValidSpring(ps[i].bendingSprings.z))
            //    DrawSpring(ss[ps[i].bendingSprings.z], ref ps, Color.red);
            //
            //if (IsValidSpring(ps[i].bendingSprings.w))
            //    DrawSpring(ss[ps[i].bendingSprings.w], ref ps, Color.red);
        }
    }

    void DrawVelocity(SpringHandler.particle[] ps)
    {
        foreach (var item in ps)
        {
            Debug.DrawRay(item.position, item.velocity, Color.white);
        }
    }

    void DrawSpring(SpringHandler.spring ss, ref SpringHandler.particle[] ps)
    {
        Debug.DrawLine(ps[ss.connectionA].position, ps[ss.connectionB].position, Color.blue);
    }

    void DrawSpring(SpringHandler.spring ss, ref SpringHandler.particle[] ps, Color col)
    {
        Debug.DrawLine(ps[ss.connectionA].position, ps[ss.connectionB].position, col);
    }


    void Print(string name, SpringHandler.particle[] array)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            //if ((i != 0) && (array[i - 1] > array[i]))
            //    problems += "Discontinuity found at " + i + "!! \n";

            values +=  array[i].structuralSprings.x + ", " + array[i].structuralSprings.y + ", " + array[i].structuralSprings.z + ", " + array[i].structuralSprings.w + ", ";
        }

        Debug.Log(name + " :  " + values + "\n" + problems);
    }

}
