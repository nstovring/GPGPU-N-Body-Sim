using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferSorter : MonoBehaviour {
    public ComputeBuffer inputcomputeBuffer;
    //public ComputeBuffer sortcomputeBuffer;
    public ComputeBuffer leafNodeBuffer, internalNodeBuffer;//,o ,e ,f, d;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    
    public ComputeShader computeShader;
    public ComputeShader sortShader;
    public Material mat;
    public Material ballmat;

    RenderTexture pointRt;
    RenderTexture velRt;
    //const int count = 10554096;
    private int cachedInstanceCount = -1;

    public const int count = 2048;

    const float size = 1f;
    [Range(0.001f, 1f)]
    public float speed = 1;
    public float angularSpeed = 0.5f;
    public float gravityMul = 1;
    [Range(0.001f,1f)]
    public float diameter = 0.05f;
    public Vector3 gravityVec = new Vector3(0, -1,0);

   
    struct particle
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 color;
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
        public Vector3 minPos;
        public Vector3 maxPos;
        public float sRadius;
        public int visited;
        public uint mortonId;
    };
   
    int mainKernelHandler;
    int mortonKernelHandler;
    int treeConstructionKernelhandler;
    int boundingSphereKernelHandler;
    int sortingKernelHandler;
    int mergeKernelHandler;
    //int loadKernelHandler;
    public Mesh cubeMesh;
    public Material instanceMaterial;
    // Use this for initialization
    void Start () {
        //Setup buffers
        int particleStructSize = (sizeof(float) * 3 * 3) + sizeof(uint) + sizeof(int) + sizeof(float);
        int nodeStructSize = sizeof(int) * 8 + sizeof(float) * 7 + sizeof(uint);
        leafNodeBuffer = new ComputeBuffer(count, nodeStructSize, ComputeBufferType.Default);
        internalNodeBuffer = new ComputeBuffer(count -1, nodeStructSize, ComputeBufferType.Default);
        inputcomputeBuffer = new ComputeBuffer(count, particleStructSize, ComputeBufferType.Default);
        //sortcomputeBuffer = new ComputeBuffer(count, particleStructSize, ComputeBufferType.Default);
        //mortonBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Default);
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
        //Get kernels
        mainKernelHandler = computeShader.FindKernel("CSMain");
        mortonKernelHandler = computeShader.FindKernel("CSSortMortonIDs");
        treeConstructionKernelhandler = computeShader.FindKernel("CSCreateBVH");
        boundingSphereKernelHandler = computeShader.FindKernel("CSGenerateBoundingSpheres");
        //mergeKernelHandler = computeShader.FindKernel("CSMerge");
        sortingKernelHandler = sortShader.FindKernel("RadixSort");
        //Create MortonIDs
        computeShader.SetBuffer(mortonKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.Dispatch(mortonKernelHandler, count / 32, 1, 1);
        Debug.Log("Dispatched morton kernel");
        ////Sort IDs
        sortShader.SetInt("count", count);
        sortShader.SetBuffer(sortingKernelHandler, "Data", inputcomputeBuffer);
        sortShader.Dispatch(sortingKernelHandler, count / 512, 1, 1);
        Debug.Log("Dispatched sorting kernel");
        
        particle[] data = new particle[count];
        inputcomputeBuffer.GetData(data);
        //Print("Sorted Data", data, true);
        
        ////Create Tree
        computeShader.SetBuffer(treeConstructionKernelhandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "leafNodes", leafNodeBuffer);
        computeShader.Dispatch(treeConstructionKernelhandler, count / 32, 1, 1);
        Debug.Log("Dispatched tree contruction kernel");
        
        internalNode[] nodeData = new internalNode[count];
        internalNodeBuffer.GetData(nodeData);
        Print("Node Data", nodeData, false);
        
        leafNodeBuffer.GetData(nodeData);
        Print("Leaf Data", nodeData, true);

        //Create bounding sphere
        computeShader.SetBuffer(boundingSphereKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "leafNodes", leafNodeBuffer);
        computeShader.Dispatch(boundingSphereKernelHandler, count / 32, 1, 1);
        Debug.Log("Dispatched bounding calculation kernel");
        
        
        //Apply Movement
        computeShader.SetBuffer(mainKernelHandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "leafNodes", leafNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.Dispatch(mainKernelHandler, count / 256, 1, 1);
        Debug.Log("Dispatched physics kernel");
    }

    // Update is called once per frame
    void Update () {
        DispatchShaders();
        Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f)), argsBuffer);
    }

    private void FixedUpdate()
    {
    }

    internalNode[] nodeData = new internalNode[count];
    internalNode[] leafData = new internalNode[count];
    particle[] particleData = new particle[count];

    bool swap = true;
    void DispatchShaders()
    {
        computeShader.SetFloat("speed", speed);
        computeShader.SetFloat("gravity", gravityMul);
        computeShader.SetVector("gravityVec", gravityVec);
        computeShader.SetFloat("angularSpeed", angularSpeed);
        computeShader.SetFloat("DeltaTime", Time.deltaTime);
        computeShader.SetFloat("radius", diameter / 2);
        sortShader.Dispatch(sortingKernelHandler, count / 512, 1, 1);
        computeShader.Dispatch(treeConstructionKernelhandler, count / 32, 1, 1);
        computeShader.Dispatch(boundingSphereKernelHandler, count / 32, 1, 1);
        computeShader.Dispatch(mainKernelHandler, count / 256, 1, 1);

    }

    public float GizmoPosScale = 1;
    public float GizmoScale = 1;
    public bool visualizeBoundingBoxes = false;
    [Range(0, count - 1)]
    public int boundingBoxMin = 1;
    [Range(0, count - 1)]
    public int boundingBoxInt = 0;

    public bool visualizePotentialCollisions = false;
    [Range(0, count - 1)]
    public int heirarchyLeafToCheckForCollision = 0;

    public bool visualizeHeirarchy = false;
    [Range(0, count - 1)]
    public int heirarchyLeaf = 0;


    


    void OnDrawGizmos()
    {
        if (visualizeHeirarchy)
            VisualiseBoundingHeirarchy();
        else if (visualizeBoundingBoxes)
            VisualizeBoundingBoxes();
        else if (visualizePotentialCollisions)
            VisualisePotentialCollisions();
    }

    void VisualiseBoundingHeirarchy()
    {
        leafNodeBuffer.GetData(leafData);
        internalNodeBuffer.GetData(nodeData);
        inputcomputeBuffer.GetData(particleData);
        int[] collisions;
        TraverseBVHIterative(leafData[heirarchyLeaf], diameter / 2, out collisions);
        Gizmos.color = Color.yellow;
        Vector3 leafPos = particleData[leafData[heirarchyLeaf].objectId].position;
        Gizmos.DrawWireSphere(leafPos * GizmoPosScale, (diameter / 2) * GizmoScale);
        Gizmos.color = Color.white;
        for (int i = 0; i < collisions.Length; i++)
        {
            int col = collisions[i];
            if(col != -1)
            {
                Gizmos.DrawLine(leafPos * GizmoPosScale, particleData[leafData[col].objectId].position * GizmoPosScale);
                Gizmos.DrawWireSphere(particleData[leafData[col].objectId].position * GizmoPosScale, (diameter / 2) * GizmoScale);
            }
        }


    }

    void VisualisePotentialCollisions()
    {
        leafNodeBuffer.GetData(leafData);
        internalNodeBuffer.GetData(nodeData);
        inputcomputeBuffer.GetData(particleData);
        int[] collisions;
        TraverseBVHIterative(leafData[heirarchyLeafToCheckForCollision], diameter / 2, out collisions);
        Vector3 leafPos = particleData[leafData[heirarchyLeafToCheckForCollision].objectId].position;

        for (int i = 0; i < collisions.Length; i++)
        {
            int col = collisions[i];
            if (col != -1)
            {
                Gizmos.DrawLine(leafPos * GizmoPosScale, particleData[col].position * GizmoPosScale);
                Gizmos.DrawWireSphere(particleData[col].position * GizmoPosScale, (diameter / 2) * GizmoScale);
            }
        }
    }

   
    void VisualizeBoundingBoxes()
    {
        int recursionCount = boundingBoxInt;
        internalNodeBuffer.GetData(nodeData);

        for (int i = boundingBoxMin; i < nodeData.Length; i++)
        {
            internalNode node = nodeData[i];
            //Gizmos.color =  Color.white * (node.overlap);
            Vector3 center = (node.minPos + node.maxPos) / 2;
            Vector3 scale = new Vector3(node.maxPos.x - node.minPos.x, node.maxPos.y - node.minPos.y, node.maxPos.z - node.minPos.z);
            Gizmos.DrawWireCube(center * GizmoPosScale, scale * GizmoScale);
        }
    }
    void GetNodeChildren(internalNode node, out internalNode childA, out internalNode childB)
    {
        int2 leaves = node.leaves;
        int2 intNodes = node.intNodes;
        childA = new internalNode();
        childB = new internalNode();
        childA.nodeId = -1;
        childB.nodeId = -1;

        if (leaves.x != -1)
            childA = leafData[leaves.x];
        if (leaves.y != -1)
            childB = leafData[leaves.y];
        if (intNodes.x != -1)
            childA = nodeData[intNodes.x];
        if (intNodes.y != -1)
            childB = nodeData[intNodes.y];
    }

    public int currentCollisions = 0;

    void TraverseBVHIterative(internalNode leaf, float radius, out int[] collisionList)
    {
        internalNode node = nodeData[0];
        int[] stack = new int[32];
        collisionList = new int[32];
        for (uint i = 0; i < 32; i++)
        {
            stack[i] = -2;
            collisionList[i] = -1;
        }

        
        int traversalCount = 0;
        int collisionCount = 0;
        int maxLoop = 0;
        Gizmos.color = Color.green;
        Vector3 AABBRadius = new Vector3(radius, radius, radius) * angularSpeed;
        Vector3 scale = AABBRadius;// new Vector3(leaf.maxPos.x - leaf.minPos.x, leaf.maxPos.y - leaf.minPos.y, leaf.maxPos.z - leaf.minPos.z);
        Gizmos.DrawWireCube(((leaf.minPos + leaf.maxPos) / 2) * GizmoPosScale, scale * GizmoScale);
    
        do
        {
            internalNode childA;
            internalNode childB;
            GetNodeChildren(node, out childA,out childB);

            AABBRadius = new Vector3(radius, radius, radius) * angularSpeed;
            bool overlapA = AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, childA.minPos, childA.maxPos)&& childA.nodeId != -1;
            bool overlapB = AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, childB.minPos, childB.maxPos)&& childB.nodeId != -1;

            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(childA.minPos * GizmoPosScale, childB.minPos * GizmoPosScale);
            //
            if(overlapA && isLeaf(childA) && childA.objectId != leaf.objectId)
            {
                Gizmos.color = Color.blue;
                scale = new Vector3(childA.maxPos.x - childA.minPos.x, childA.maxPos.y - childA.minPos.y, childA.maxPos.z - childA.minPos.z);
                Gizmos.DrawWireCube(((childA.minPos + childA.maxPos) / 2) * GizmoPosScale, AABBRadius * GizmoScale );
            
            }
            if (overlapB && isLeaf(childB) && childA.objectId != leaf.objectId)
            {
                Gizmos.color = Color.red;
                scale = new Vector3(childB.maxPos.x - childB.minPos.x, childB.maxPos.y - childB.minPos.y, childB.maxPos.z - childB.minPos.z);
                Gizmos.DrawWireCube(((childB.minPos + childB.maxPos) / 2) * GizmoPosScale, AABBRadius * GizmoScale);
            }
            Gizmos.color = Color.white;

            //Gizmos.DrawWireCube(((node.minPos + node.maxPos) / 2) * GizmoPosScale, scale * GizmoScale);
            if (overlapA && isLeaf(childA) && childA.objectId != leaf.objectId)
            {
                collisionList[collisionCount] = childA.nodeId;
                collisionCount++;
            }
            if (overlapB && isLeaf(childB) && childB.objectId != leaf.objectId)
            {
                collisionList[collisionCount] = childB.nodeId;
                collisionCount++;
            }
            currentCollisions = collisionCount;

            bool traverseA = (overlapA && !isLeaf(childA));
            bool traverseB = (overlapB && !isLeaf(childB));
            //Debug.Log(stack[traversalCount]);

            if (!traverseA && !traverseB)
            {
                stack[traversalCount] = -1;
                traversalCount--;
                traversalCount = traversalCount <= 0 ? 0 : traversalCount;
                if (stack[traversalCount] == -1)
                {
                    //Debug.Log("Popping Stack : MaxLoop," + maxLoop);
                    return;
                }
                node = nodeData[stack[traversalCount]];
            }
            else
            {
                if (traverseA)
                    node = childA;
                else
                    node = childB;

                if (traverseA && traverseB)
                {
                    stack[traversalCount] = childB.nodeId;
                    //Gizmos.color = Color.red;
                    //scale = new Vector3(childB.maxPos.x - childB.minPos.x, childB.maxPos.y - childB.minPos.y, childB.maxPos.z - childB.minPos.z);
                    //Gizmos.DrawWireCube(((childB.minPos + childB.maxPos) / 2) * GizmoPosScale, scale * GizmoScale);

                    traversalCount++;
                    //Debug.Log("Pushing Stack");
                }
            }
            maxLoop++;
        } while (stack[traversalCount] != -1 && maxLoop < 64);//traversing && traversalCount < 64);

    }
    bool AABBOverlap(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
    {
        return (minA.x <= maxB.x && maxA.x >= minB.x) &&
            (minA.y <= maxB.y && maxA.y >= minB.y) &&
            (minA.z <= maxB.z && maxA.z >= minB.z);
    }

    bool isLeaf(internalNode node)
    {
        if (node.objectId != -1)
            return true;
        return false;
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
            points[i].color = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
        }
        return points;
    }
    void OnDestroy()
    {
        inputcomputeBuffer.Release();
        internalNodeBuffer.Release();
        leafNodeBuffer.Release();
        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
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
    void Print(string name, int[] array)
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

    void Print(string name, particle[] array, bool divided)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1].morton > array[i].morton))
                problems += "Discontinuity found at " + i + "!! \n";
            if(divided)
            values += (int)array[i].morton / 10000000 + " ";
            else
            values += (int)array[i].morton / 1000 + " ";
        }

        Debug.Log(name + " :  " + values + "\n" + problems);
    }
    void Print(string name, internalNode[] array, bool leaf)
    {
        string values = "";
        string problems = "";

        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1].mortonId > array[i].mortonId))
                problems += "Discontinuity found at " + i + "!! \n";
            if (leaf)
                values += (int)array[i].mortonId / 10000000 + " ";
            else
                values += (int)array[i].mortonId / 1000 + " ";

            //if ((i != 0) && (array[i - 1].morton > array[i].morton))
            //    problems += "Discontinuity found at " + i + "!! \n";
            // values += "<" + array[i].sPos.ToString() + ":" + array[i].sRadius + "> " + "\n";// + "\n" + "<" + array[i].intNodes.x + ":" + array[i].intNodes.y + ">__" + "\n";
            //if (leaf)
            //values += array[i].parentId + ", "+ array[i].objectId + " , Morton ->" + array[i].mortonId +"\n";
            //else
            //values += array[i].parentId + " Leaves>(" + array[i].leaves.x + "," + array[i].leaves.y + ")  " + " Nodes>(" + array[i].intNodes.x + ":" + array[i].intNodes.y + ") Morton ->" + array[i].mortonId + "\n";

        }

        Debug.Log(name + " : \n" + values + "\n" + problems);
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
