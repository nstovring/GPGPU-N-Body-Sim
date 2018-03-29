using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSimulator : MonoBehaviour {

    public ComputeShader clothComputeShader;
    public int count = 64;
    int mainClothKernelHandler;
    int springKernelHandler;

    ComputeBuffer particleBuffer;
    ComputeBuffer springBuffer;
    ComputeBuffer testParticleBuffer;

    int springCount;
    public float stiffness;
    [Range(0,1)]
    public float damping;
    [Range(1,4)]
    public float mass;
    public Transform clothHandler;

    [System.Serializable]
    public class SpringVariables
    {
        [Range(0,1)]
        public float damping = 0.5f;
        [Range(1, 100)]
        public float stiffness = 7;
    }

    [SerializeField]
    public SpringVariables structuralSpringVars;
    public SpringVariables shearSpringVars;
    public SpringVariables structuralBendingSpringVars;
    public SpringVariables shearBendingSpringVars;

    public struct testParticle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
        public int isFixed;
        public int iD;
        public int[] springs;
    };

    // Use this for initialization
    void Start () {
        mainClothKernelHandler = clothComputeShader.FindKernel("CSMain");
        springKernelHandler = clothComputeShader.FindKernel("CSSprings");
        testParticle tP = new testParticle();
        //tP.springs = new int[16];
        int testSize = sizeof(float) * 7 + sizeof(int) * 18;
        Debug.Log(testSize);
        int particleStructSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpringHandler.particle));
        Debug.Log(particleStructSize);
        int springStructSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(SpringHandler.spring));

        testParticle[] testParticles = new testParticle[count];
        testParticleBuffer = new ComputeBuffer(count, testSize, ComputeBufferType.Default);
        for (int i = 0; i < testParticles.Length; i++)
        {
            testParticles[i].springs = new int[16];
            for (int j = 0; j < 16; j++)
            {
                testParticles[i].springs[j] = j;
            }
        }

        testParticleBuffer.SetData(testParticles);

        clothComputeShader.SetBuffer(mainClothKernelHandler, "testParticles", testParticleBuffer);
        testParticleBuffer.GetData(testParticles);
        Print("TestParticle Connections", testParticles);

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
            particles[i].structutralBendingSprings.x = -1;
            particles[i].structutralBendingSprings.y = -1;
            particles[i].structutralBendingSprings.z = -1;
            particles[i].structutralBendingSprings.w = -1;
            particles[i].shearBendingSprings.x = -1;
            particles[i].shearBendingSprings.y = -1;
            particles[i].shearBendingSprings.z = -1;
            particles[i].shearBendingSprings.w = -1;
        }

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                particles[y + x * rows].mass = 1;
                particles[y + x * rows].iD = y + x * rows;
                particles[y + x * rows].position = new Vector3(x/ 512f, y/ 512f, 0);
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
            springs.Add(SpringHandler.GetSpring(x, y, rows,out connectedParticleIndex, 0,1, structuralSpringVars));
            particles[connectedParticleIndex].structuralSprings.w = springs.Count-1;
        }

        if (x < rows - 1)
        {
            particles[y + x * rows].structuralSprings.y = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows,out connectedParticleIndex, 1, 0, structuralSpringVars));
            particles[connectedParticleIndex].structuralSprings.z = springs.Count-1;
        }

        if (y < rows - 1 && x < rows - 1)
        {
            particles[y + x * rows].shearSprings.x = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 1, 1, shearSpringVars));
            particles[connectedParticleIndex].shearSprings.z = springs.Count - 1;
        }
        
        if (y < rows - 1 && x > 0)
        {
            particles[y + x * rows].shearSprings.y = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, -1, 1, shearSpringVars));
            particles[connectedParticleIndex].shearSprings.w = springs.Count - 1;
        }

        //Get shearBendingSprings
        if (y > 1 && x < rows - 2)
        {
            particles[y + x * rows].shearBendingSprings.x = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 2, -2,shearBendingSpringVars));
            particles[connectedParticleIndex].shearBendingSprings.z = springs.Count - 1;
        }
        
        if (y < rows - 2 && x < rows - 2)
        {
            particles[y + x * rows].shearBendingSprings.y = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 2, 2, shearBendingSpringVars));
            particles[connectedParticleIndex].shearBendingSprings.w = springs.Count - 1;
        }

        //Get structutralBendingSprings
        if (y < rows - 2)
        {
            particles[y + x * rows].structutralBendingSprings.x = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 0, 2, structuralBendingSpringVars));
            particles[connectedParticleIndex].structutralBendingSprings.z = springs.Count - 1;
        }
        
        if (x < rows - 2)
        {
            particles[y + x * rows].structutralBendingSprings.y = springs.Count;
            int connectedParticleIndex;
            springs.Add(SpringHandler.GetSpring(x, y, rows, out connectedParticleIndex, 2, 0, structuralBendingSpringVars));
            particles[connectedParticleIndex].structutralBendingSprings.w = springs.Count - 1;
        }

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
            DrawAllConnections(ps, ss);
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
    public bool drawStructuralSprings = false;
    public bool drawShearSprings = false;
    public bool drawStructuralBendingSprings = false;
    public bool drawShearBendingSprings = false;
    public bool showForSingleParticle = false;
    [Range(0, 511)]
    public int selectedParticle = 0;

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

    void DrawAllConnections(SpringHandler.particle[] ps, SpringHandler.spring[] ss)
    {
        if (showForSingleParticle)
        {
            DrawConnections(ps, ss, selectedParticle);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                DrawConnections(ps, ss, i);
            }
        }
    }

    void DrawConnections(SpringHandler.particle[] ps, SpringHandler.spring[] ss, int i)
    {
        if (drawStructuralSprings)
        {
            if (IsValidSpring(ps[i].structuralSprings.x))
                DrawSpring(ss[ps[i].structuralSprings.x], ref ps, Color.blue);

            if (IsValidSpring(ps[i].structuralSprings.y))
                DrawSpring(ss[ps[i].structuralSprings.y], ref ps, Color.blue);

            if (IsValidSpring(ps[i].structuralSprings.z))
                DrawSpring(ss[ps[i].structuralSprings.z], ref ps, Color.blue);

            if (IsValidSpring(ps[i].structuralSprings.w))
                DrawSpring(ss[ps[i].structuralSprings.w], ref ps, Color.blue);
        }

        if (drawShearSprings)
        {

            if (IsValidSpring(ps[i].shearSprings.x))
                DrawSpring(ss[ps[i].shearSprings.x], ref ps, Color.green);

            if (IsValidSpring(ps[i].shearSprings.y))
                DrawSpring(ss[ps[i].shearSprings.y], ref ps, Color.green);

            if (IsValidSpring(ps[i].shearSprings.z))
                DrawSpring(ss[ps[i].shearSprings.z], ref ps, Color.green);

            if (IsValidSpring(ps[i].shearSprings.w))
                DrawSpring(ss[ps[i].shearSprings.w], ref ps, Color.green);
        }

        if (drawStructuralBendingSprings)
        {
            if (IsValidSpring(ps[i].structutralBendingSprings.x))
                DrawSpring(ss[ps[i].structutralBendingSprings.x], ref ps, Color.blue);

            if (IsValidSpring(ps[i].structutralBendingSprings.y))
                DrawSpring(ss[ps[i].structutralBendingSprings.y], ref ps, Color.blue);

            if (IsValidSpring(ps[i].structutralBendingSprings.z))
                DrawSpring(ss[ps[i].structutralBendingSprings.z], ref ps, Color.blue);

            if (IsValidSpring(ps[i].structutralBendingSprings.w))
                DrawSpring(ss[ps[i].structutralBendingSprings.w], ref ps, Color.blue);
        }

        if (drawShearBendingSprings)
        {
            if (IsValidSpring(ps[i].shearBendingSprings.x))
                DrawSpring(ss[ps[i].shearBendingSprings.x], ref ps, Color.green);

            if (IsValidSpring(ps[i].shearBendingSprings.y))
                DrawSpring(ss[ps[i].shearBendingSprings.y], ref ps, Color.green);

            if (IsValidSpring(ps[i].shearBendingSprings.z))
                DrawSpring(ss[ps[i].shearBendingSprings.z], ref ps, Color.green);

            if (IsValidSpring(ps[i].shearBendingSprings.w))
                DrawSpring(ss[ps[i].shearBendingSprings.w], ref ps, Color.green);
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

    void Print(string name, testParticle[] array)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            //if ((i != 0) && (array[i - 1] > array[i]))
            //    problems += "Discontinuity found at " + i + "!! \n";

            values += array[i].springs[0] + ", " + array[i].springs[1] + ", " + array[i].springs[2] + ", " + array[i].springs[3] + ", ";
        }

        Debug.Log(name + " :  " + values + "\n" + problems);
    }

    private void OnDestroy()
    {
        particleBuffer.Release();
        springBuffer.Release();
        testParticleBuffer.Release();
    }


}
