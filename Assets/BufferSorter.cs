using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferSorter : MonoBehaviour {
    public ComputeBuffer inputcomputeBuffer;
    public ComputeBuffer sortcomputeBuffer;
    public ComputeBuffer mortonBuffer, tempBuffer, leafNodeBuffer, internalNodeBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    
    public ComputeShader computeShader;
    public Material mat;
    public Material ballmat;

    public ParticleSystem particleSystem;

    RenderTexture pointRt;
    RenderTexture velRt;
    //const int count = 10554096;
    private int cachedInstanceCount = -1;
    public const int count = 512;
    const float size = 1f;
    [Range(0.001f, 1f)]
    public float speed = 1;
    public float angularSpeed = 0.5f;
    public float gravityMul = 1;
    [Range(0.001f,1f)]
    public float radius = 0.05f;
    public Vector3 gravityVec = new Vector3(0, -1,0);
    struct particle
    {
        public Vector3 position;
        public Vector3 direction;
        float radius;
        public uint morton;
        public int collision;
    }
    struct internalNode
    {
        public int objectId;
        public int nodeId;
        public int parentId;
        public int2 intNodes;
        public int2 leaves;
        public Vector3 sPos;
        public Vector3 maxPos;
        public float sRadius;
        public int visited;
    };
   
    int mainKernelHandler;
    int mortonKernelHandler;
    int treeConstructionKernelhandler;
    int boundingSphereKernelHandler;
    public Mesh cubeMesh;
    public Material instanceMaterial;
    // Use this for initialization
    void Start () {
        //Setup buffers
        leafNodeBuffer = new ComputeBuffer(count, sizeof(int) * 8 + sizeof(float) * 7 , ComputeBufferType.Default);
        internalNodeBuffer = new ComputeBuffer(count -1, sizeof(int) * 8 + sizeof(float) * 7, ComputeBufferType.Default);
        inputcomputeBuffer = new ComputeBuffer(count, (sizeof(float) * 3 * 2) + sizeof(uint) + sizeof(int) + sizeof(float), ComputeBufferType.Default);
        sortcomputeBuffer = new ComputeBuffer(count,(sizeof(float) * 3 * 2) + sizeof(uint) + sizeof(int) + sizeof(float) , ComputeBufferType.Default);
        mortonBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Default);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        
        //Apply buffer for instanced mesh drawing 
        instanceMaterial.SetBuffer("positionBuffer", inputcomputeBuffer);
        uint numIndices = (cubeMesh != null) ? (uint)cubeMesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = (uint)count;
        argsBuffer.SetData(args);
        cachedInstanceCount = count;
        
        //Generate Initial Particle Values
        particle[] points = GetParticlePoints();
        inputcomputeBuffer.SetData(points);
        //Apply 
        //mat.SetBuffer("computeBuffer", inputcomputeBuffer);
        //ballmat.SetBuffer("computeBuffer", inputcomputeBuffer);
        //Get kernels
        mainKernelHandler = computeShader.FindKernel("CSMain");
        mortonKernelHandler = computeShader.FindKernel("CSSortMortonIDs");
        treeConstructionKernelhandler = computeShader.FindKernel("CSCreateBVH");
        boundingSphereKernelHandler = computeShader.FindKernel("CSGenerateBoundingSpheres");
        //Create MortonIDs
        computeShader.SetBuffer(mortonKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(mortonKernelHandler, "mortonIds", mortonBuffer);
        computeShader.Dispatch(mortonKernelHandler, count / 32, 1, 1);
        //Sort IDs
        tempBuffer = new ComputeBuffer(count, sizeof(uint));
        GpuSort.BitonicSortParticle32(inputcomputeBuffer, tempBuffer);
        //Create Tree
        computeShader.SetBuffer(treeConstructionKernelhandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "leafNodes", leafNodeBuffer);
        computeShader.Dispatch(treeConstructionKernelhandler, count / 512, 1, 1);

        internalNode[] nodeData = new internalNode[count];
        //internalNodeBuffer.GetData(nodeData);
        //Print("", nodeData);
        //Create bounding sphere
        computeShader.SetBuffer(boundingSphereKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "leafNodes", leafNodeBuffer);
        computeShader.Dispatch(boundingSphereKernelHandler, count / 512, 1, 1);
        internalNodeBuffer.GetData(nodeData);
        Print("", nodeData);
        //Apply Movement
        computeShader.SetBuffer(mainKernelHandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "leafNodes", leafNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "mortonIds", mortonBuffer);
        computeShader.Dispatch(mainKernelHandler, count / 512, 1, 1);

    }
	
	// Update is called once per frame
	void Update () {
        DispatchShaders();
        Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f)), argsBuffer);
    }

    internalNode[] nodeData = new internalNode[count];
    particle[] particleData = new particle[count];

    void DispatchShaders()
    {
        computeShader.Dispatch(mortonKernelHandler, count / 32, 1, 1);
        GpuSort.BitonicSortParticle32(inputcomputeBuffer, tempBuffer);
        computeShader.Dispatch(treeConstructionKernelhandler, count / 512, 1, 1);
        computeShader.Dispatch(boundingSphereKernelHandler, count / 512, 1, 1);
        computeShader.SetFloat("speed", speed);
        computeShader.SetFloat("gravity", gravityMul);
        computeShader.SetVector("gravityVec", gravityVec);
        computeShader.SetFloat("angularSpeed", angularSpeed);
        computeShader.SetFloat("DeltaTime", Time.deltaTime);
        computeShader.SetFloat("radius", radius);
        computeShader.Dispatch(mainKernelHandler, count / 32, 1, 1);

        inputcomputeBuffer.GetData(particleData);
        internalNodeBuffer.GetData(nodeData);
        //VisualizeBoundingSpheres(nodeData);
//        Print("", nodeData);

    }

    public float GizmoPosScale = 1;
    public float GizmoScale = 1;
    public bool visualizeBoundingBoxes = false;
    public bool visualizePotentialCollisions = false;

    void OnDrawGizmos()
    {
        if(visualizeBoundingBoxes)
        VisualizeBoundingBoxes();
        if (visualizePotentialCollisions)
            VisualisePotentialCollisions();
    }

    void VisualisePotentialCollisions()
    {
        foreach (var leaf in particleData)
        {
            int collisionID = leaf.collision;
            if (collisionID != -1 && collisionID != -2)
                Gizmos.DrawLine(leaf.position *GizmoPosScale, particleData[leaf.collision].position * GizmoPosScale);
        }

    }

    void VisualizeBoundingBoxes()
    {
        foreach (var node in nodeData)
        {
            //Gizmos.color =  Color.white * (node.overlap);
            Vector3 center = (node.sPos + node.maxPos) / 2;
            Vector3 scale = new Vector3(node.maxPos.x - node.sPos.x, node.maxPos.y - node.sPos.y, node.maxPos.z - node.sPos.z);
            Gizmos.DrawWireCube(center * GizmoPosScale, scale * GizmoScale);
        }
    }

    Vector3[] GetVectorPoints()
    {
        Vector3[] points = new Vector3[count];
        Random.seed = 0;

        for (int i = 0; i < count; i++)
        {
            points[i] = new Vector3(Random.Range(-size, size), Random.Range(0, size * 2), 0);

        }
        return points;
    }

    particle[] GetParticlePoints()
    {
        particle[] points = new particle[count];
        Random.seed = 1422347532;

        for (int i = 0; i < count; i++)
        {
            points[i].position = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
            points[i].direction = new Vector3(Random.Range(-size, size), Random.Range(-size, size), Random.Range(-size, size));
        }
        return points;
    }
    void OnPostRender()
    {
        //mat.SetPass(0);
       // Graphics.DrawProcedural(MeshTopology.Points, count, 1);

    }
    void OnDestroy()
    {
        inputcomputeBuffer.Release();
        sortcomputeBuffer.Release();
        tempBuffer.Release();
        mortonBuffer.Release();
        internalNodeBuffer.Release();
        leafNodeBuffer.Release();
        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
        //rt.Release();
    }

    void Print(string name, uint[] array)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1] > array[i]))
                problems += "Discontinuity found at " + i + "!! \n";

            values += array[i] + " ";
        }

        Debug.Log(name + " :  " + values + "\n" + problems);
    }


    void Print(string name, particle[] array)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1].morton > array[i].morton))
                problems += "Discontinuity found at " + i + "!! \n";

            values += (int)array[i].morton/(10000000) + " ";
        }

        Debug.Log(name + " :  " + values + "\n" + problems);
    }
    void Print(string name, internalNode[] array)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            //if ((i != 0) && (array[i - 1].morton > array[i].morton))
            //    problems += "Discontinuity found at " + i + "!! \n";
           // values += "<" + array[i].sPos.ToString() + ":" + array[i].sRadius + "> " + "\n";// + "\n" + "<" + array[i].intNodes.x + ":" + array[i].intNodes.y + ">__" + "\n";

            values += "<" + array[i].sPos.ToString() + ":" + array[i].sRadius + "> "+ "<" + array[i].leaves.x + ":" + array[i].leaves.y + " Leaves > " + "<" + array[i].intNodes.x + ":" + array[i].intNodes.y + " Internal Nodes >  Visited ->" + array[i].visited +"\n";
        }

        Debug.Log(name + " :  " + values + "\n" + problems);
    }

    // Inverse of Part1By1 - "delete" all odd-indexed bits
    uint Compact1By1(uint x)
    {
        x &= 0x55555555;                  // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
        x = (x ^ (x >> 1)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
        x = (x ^ (x >> 2)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
        x = (x ^ (x >> 4)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
        x = (x ^ (x >> 8)) & 0x0000ffff; // x = ---- ---- ---- ---- fedc ba98 7654 3210
        return x;
    }

    // Inverse of Part1By2 - "delete" all bits not at positions divisible by 3
    uint Compact1By2(uint x)
    {
        x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
        x = (x ^ (x >> 2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
        x = (x ^ (x >> 4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
        x = (x ^ (x >> 8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
        x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
        return x;
    }

    uint DecodeMorton3X(uint code)
    {
        return Compact1By2(code >> 0);
    }

    uint DecodeMorton3Y(uint code)
    {
        return Compact1By2(code >> 1);
    }

    uint DecodeMorton3Z(uint code)
    {
        return Compact1By2(code >> 2);
    }

    Vector3 DecodeMortornToVector(uint code)
    {
        uint x = DecodeMorton3X(code);
        uint y = DecodeMorton3Y(code);
        uint z = DecodeMorton3Z(code);
        return new Vector3(x, y, z);
    }


    // Expands a 10-bit integer into 30 bits
    // by inserting 2 zeros after each bit.
    uint expandBits(uint v)
    {
        v = (v * 0x00010001u) & 0xFF0000FFu;
        v = (v * 0x00000101u) & 0x0F00F00Fu;
        v = (v * 0x00000011u) & 0xC30C30C3u;
        v = (v * 0x00000005u) & 0x49249249u;
        return v;
    }

    // Calculates a 30-bit Morton code for the
    // given 3D point located within the unit cube [0,1].
    uint Morton3D(float x, float y, float z)
    {
        x = Mathf.Min(Mathf.Max(x * 1024.0f, 0.0f), 1023.0f);
        y = Mathf.Min(Mathf.Max(y * 1024.0f, 0.0f), 1023.0f);
        z = Mathf.Min(Mathf.Max(z * 1024.0f, 0.0f), 1023.0f);
        uint xx = expandBits((uint)x);
        uint yy = expandBits((uint)y);
        uint zz = expandBits((uint)z);
        return xx * 4 + yy * 2 + zz;
    }
}
