using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BufferSorter : MonoBehaviour
{
    public ComputeBuffer inputcomputeBuffer;
    //public ComputeBuffer sortcomputeBuffer;
    public ComputeBuffer leafNodeBuffer, internalNodeBuffer, indexBuffer, mergeOutputBuffer, boundingLeafNodeBuffer, boundingInternalNodeBuffer;//,o ,e ,f, d;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    CommandBuffer cm;
    public ComputeShader computeShader;
    public ComputeShader sortShader;
    public Material mat;
    public Material ballmat;

    RenderTexture pointRt;
    RenderTexture velRt;
    //const int count = 10554096;
    private int cachedInstanceCount = -1;

    public const int count = 2048;
    const int groupSize = 128;
    int groupAmount = count / groupSize;

    const float size = 1f;
    [Range(0.001f, 1f)]
    public float speed = 1;
    public float angularSpeed = 0.5f;
    public float gravityMul = 1;
    [Range(0.001f, 1f)]
    public float diameter = 0.05f;
    public Vector3 gravityVec = new Vector3(0, -1, 0);


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
        public int2 bLeaves;
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
    int writeNodeDataKernelHandler;
    //int loadKernelHandler;
    public Mesh cubeMesh;
    public Material instanceMaterial;

   
    // Use this for initialization
    void Start()
    {
        //Initialize variables
        computeShader.SetFloat("radius", diameter / 2);
        computeShader.SetFloat("speed", speed);
        computeShader.SetFloat("gravity", gravityMul);
        computeShader.SetVector("gravityVec", gravityVec);
        computeShader.SetFloat("angularSpeed", angularSpeed);
        computeShader.SetFloat("DeltaTime", Time.deltaTime / integrationStep);
        //Setup buffers
        int particleStructSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(particle));
        int nodeStructSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(internalNode));
        leafNodeBuffer = new ComputeBuffer(count, nodeStructSize, ComputeBufferType.Default);
        boundingLeafNodeBuffer = new ComputeBuffer(count, nodeStructSize, ComputeBufferType.Default);
        internalNodeBuffer = new ComputeBuffer(count - 1, nodeStructSize, ComputeBufferType.Default);
        boundingInternalNodeBuffer = new ComputeBuffer(count - 1, nodeStructSize, ComputeBufferType.Default);
        inputcomputeBuffer = new ComputeBuffer(count, particleStructSize, ComputeBufferType.Default);
        mergeOutputBuffer = new ComputeBuffer(count, particleStructSize, ComputeBufferType.Default);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        indexBuffer = new ComputeBuffer(count, sizeof(int), ComputeBufferType.Default);
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
        mergeKernelHandler = computeShader.FindKernel("Merge");
        sortingKernelHandler = sortShader.FindKernel("RadixSort");
        writeNodeDataKernelHandler = computeShader.FindKernel("WriteNodeData");
        //Create MortonIDs
        computeShader.SetBuffer(mortonKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.Dispatch(mortonKernelHandler, count / groupSize, 1, 1);
        Debug.Log("Dispatched morton kernel");
        ////Sort IDs
        sortShader.SetBuffer(sortingKernelHandler, "Data", inputcomputeBuffer);
        sortShader.Dispatch(sortingKernelHandler, count / groupSize, 1, 1);

        particle[] data = new particle[count];
        inputcomputeBuffer.GetData(data);
        Print("Sorted Data", data, true);
        computeShader.SetBuffer(mergeKernelHandler, "inputPoints", inputcomputeBuffer);
        computeShader.SetBuffer(mergeKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        computeShader.Dispatch(mergeKernelHandler, count / groupSize, 1, 1);
        //}

        Debug.Log("Dispatched sorting kernel");

        data = new particle[count];
        mergeOutputBuffer.GetData(data);
        Print("Sorted Data", data, true);

        computeShader.SetBuffer(writeNodeDataKernelHandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(writeNodeDataKernelHandler, "leafNodes", leafNodeBuffer);
        computeShader.SetBuffer(writeNodeDataKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        computeShader.SetBuffer(writeNodeDataKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        computeShader.SetBuffer(writeNodeDataKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        computeShader.Dispatch(writeNodeDataKernelHandler, count / groupSize, 1, 1);
        //Create Tree
        computeShader.SetBuffer(treeConstructionKernelhandler, "inputPoints", mergeOutputBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "leafNodes", leafNodeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "mergeOutputBuffer", mergeOutputBuffer);
        computeShader.SetBuffer(treeConstructionKernelhandler, "indexBuffer", indexBuffer);
        
        computeShader.Dispatch(treeConstructionKernelhandler, count / groupSize, 1, 1);
        
        Debug.Log("Dispatched tree contruction kernel");

        boundingInternalNodeBuffer.GetData(nodeData);
        Print("Node Data", nodeData, false);
        //internalNode[] nodeData = new internalNode[count];

        boundingLeafNodeBuffer.GetData(nodeData);
        Print("Leaf Data", nodeData, false);

        //Create bounding sphere
        computeShader.SetBuffer(boundingSphereKernelHandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "leafNodes", leafNodeBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        computeShader.SetBuffer(boundingSphereKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        //computeShader.Dispatch(boundingSphereKernelHandler, count / groupSize, 1, 1);
        //Debug.Log("Dispatched bounding calculation kernel");

        //internalNodeBuffer.GetData(nodeData);
        //Print("Node Data", nodeData, false);

        //Apply Movement
        computeShader.SetBuffer(mainKernelHandler, "internalNodes", internalNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "leafNodes", leafNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        computeShader.SetBuffer(mainKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        computeShader.SetBuffer(mainKernelHandler, "inputPoints", inputcomputeBuffer);

        computeShader.Dispatch(mainKernelHandler, count / groupSize, 1, 1);
        Debug.Log("Dispatched physics kernel");

       

        //cm = new CommandBuffer();
        //cm.DispatchCompute(sortShader, sortingKernelHandler, count / 128, 1, 1);
        //cm.DispatchCompute(sortShader, mergeKernelHandler, count / 128, 1, 1);
        //cm.DispatchCompute(computeShader, treeConstructionKernelhandler, count / groupSize, 1, 1);
        //cm.DispatchCompute(computeShader, boundingSphereKernelHandler, count / groupSize, 1, 1);
        //cm.DispatchCompute(computeShader, mainKernelHandler, count / groupSize, 1, 1);
        //cm.DrawMeshInstancedIndirect(cubeMesh, 0, instanceMaterial, 0, argsBuffer);
        //Camera.main.AddCommandBuffer(CameraEvent.BeforeDepthNormalsTexture, cm);
        //internalNodeBuffer.GetData(nodeData);
        //Print("Node Data", nodeData, false);
        //leafNodeBuffer.GetData(leafData);
        //indexBuffer.GetData(indexData);
        //Print("Index Data", indexData);
        //Print("Leaf Data", leafData, true);
        //swap = false;
    }

    // Update is called once per frame
    void Update()
    {
        //computeShader.SetFloat("radius", diameter / 2);
        //computeShader.SetFloat("speed", speed);
        //computeShader.SetFloat("gravity", gravityMul);
        //computeShader.SetVector("gravityVec", gravityVec);
        //computeShader.SetFloat("angularSpeed", angularSpeed);
        //computeShader.SetFloat("DeltaTime", Time.deltaTime / integrationStep);
        //Graphics.ExecuteCommandBufferAsync(cm,ComputeQueueType.Default);
        DispatchShaders();
        Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f)), argsBuffer);
    }



    internalNode[] nodeData = new internalNode[count-1];
    internalNode[] leafData = new internalNode[count];
    particle[] particleData = new particle[count];
    int[] indexData = new int[count];

    public bool swap = false;
    int integrationStep = 8;
    void DispatchShaders()
    {
      
    

        for (int i = 0; i < integrationStep; i++)
        {
            computeShader.SetFloat("radius", diameter / 2);
            computeShader.SetFloat("speed", speed);
            computeShader.SetFloat("gravity", gravityMul);
            computeShader.SetVector("gravityVec", gravityVec);
            computeShader.SetFloat("angularSpeed", angularSpeed);
            computeShader.SetFloat("DeltaTime", Time.deltaTime / integrationStep);
            sortShader.Dispatch(sortingKernelHandler, count / groupSize, 1, 1);
            computeShader.Dispatch(mergeKernelHandler, count / groupSize, 1, 1);
            computeShader.Dispatch(writeNodeDataKernelHandler, count / groupSize, 1, 1);
            computeShader.Dispatch(treeConstructionKernelhandler, count / groupSize, 1, 1);
            //computeShader.Dispatch(boundingSphereKernelHandler, count / groupSize, 1, 1);
            computeShader.Dispatch(mainKernelHandler, count / groupSize, 1, 1);
        }

        if (swap)
        {
            internalNodeBuffer.GetData(nodeData);
            Print("Node Data", nodeData, false);
            boundingLeafNodeBuffer.GetData(leafData);
            indexBuffer.GetData(indexData);
            Print("Index Data", indexData);
            Print("Leaf Data", leafData, true);
            swap = false;
        }
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

    public bool visualizeBVHTree = false;

    void OnDrawGizmos()
    {
        if (visualizeHeirarchy)
            VisualiseBoundingHeirarchy();
        else if (visualizeBoundingBoxes)
            VisualizeBoundingBoxes();
        else if (visualizePotentialCollisions)
            VisualisePotentialCollisions();

        if (visualizeBVHTree)
        {
            VisualizeBVHTree();
            //swap = false;
        }
    }

    void VisualiseBoundingHeirarchy()
    {
        boundingLeafNodeBuffer.GetData(leafData);
        boundingInternalNodeBuffer.GetData(nodeData);
        mergeOutputBuffer.GetData(particleData);
        int[] collisions;
        TraverseBVHIterative(leafData[heirarchyLeaf], diameter / 2, out collisions);
        Gizmos.color = Color.yellow;
        Vector3 leafPos = particleData[leafData[heirarchyLeaf].objectId].position;
        Gizmos.DrawWireSphere(leafPos * GizmoPosScale, (diameter / 2) * GizmoScale);
        Gizmos.color = Color.white;
        for (int i = 0; i < collisions.Length; i++)
        {
            int col = collisions[i];
            if (col != -1)
            {
                Gizmos.DrawLine(leafPos * GizmoPosScale, particleData[leafData[col].objectId].position * GizmoPosScale);
                Gizmos.DrawWireSphere(particleData[leafData[col].objectId].position * GizmoPosScale, (diameter / 2) * GizmoScale);
            }
        }


    }

    void VisualisePotentialCollisions()
    {
        boundingLeafNodeBuffer.GetData(leafData);
        boundingInternalNodeBuffer.GetData(nodeData);
        mergeOutputBuffer.GetData(particleData);
        int[] collisions;
        int leaf = 0;
        for (int i = 0; i < leafData.Length; i++)
        {
            if(leafData[i].objectId == heirarchyLeafToCheckForCollision)
            {
                leaf = i;
            }
        }

        

        TraverseBVHIterative(leafData[leaf], diameter / 2, out collisions);
        Vector3 leafPos = particleData[leafData[leaf].objectId].position;

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
        boundingInternalNodeBuffer.GetData(nodeData);

        for (int i = boundingBoxMin; i < nodeData.Length; i++)
        {
            internalNode node = nodeData[i];
            //Gizmos.color =  Color.white * (node.overlap);
            Vector3 center = (node.minPos + node.maxPos) / 2;
            Vector3 scale = new Vector3(node.maxPos.x - node.minPos.x, node.maxPos.y - node.minPos.y, node.maxPos.z - node.minPos.z);
            Gizmos.DrawWireCube(center * GizmoPosScale, scale * GizmoScale);
        }
    }

    void DrawTreeRecursive(internalNode root, Vector3 origin, float scale)
    {
        internalNode ChildA;
        internalNode ChildB;

        GetNodeChildren(root,out ChildA, out ChildB);
        Debug.DrawLine(origin, origin + new Vector3(-2 * scale, -1, 2) );
        Debug.DrawLine(origin, origin + new Vector3(2 * scale, -1, -2));
        DrawNode(ChildA, origin + new Vector3(-2 * scale, -1, 2));
        DrawNode(ChildB, origin + new Vector3(2 * scale, -1, -2));

        if (!isLeaf(ChildA))
            DrawTreeRecursive(ChildA, origin + new Vector3(-2 * scale, -1, 2), scale * treeScale);
        if (!isLeaf(ChildB))
            DrawTreeRecursive(ChildB, origin + new Vector3(2 * scale, -1, -2) , scale * treeScale);
    }

    void DrawNode(internalNode node, Vector3 pos)
    {
        Gizmos.DrawSphere(pos, 0.5f);
    }

    [Range(0,2)]
    public float treeScale = 0.55f;
    void VisualizeBVHTree()
    {
        internalNode root = nodeData[0];
        DrawNode(root, Vector3.zero);
        DrawTreeRecursive(root, Vector3.zero, 2f * treeScale);
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
        int[] stack = new int[64];
        collisionList = new int[64];
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
            GetNodeChildren(node, out childA, out childB);

            AABBRadius = new Vector3(radius, radius, radius) * angularSpeed;
            bool overlapA = AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, childA.minPos, childA.maxPos) && childA.nodeId != -1;
            bool overlapB = AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, childB.minPos, childB.maxPos) && childB.nodeId != -1;

            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(childA.minPos * GizmoPosScale, childB.minPos * GizmoPosScale);
            //
            if (overlapA && isLeaf(childA) && childA.objectId != leaf.objectId)
            {
                Gizmos.color = Color.blue;
                scale = new Vector3(childA.maxPos.x - childA.minPos.x, childA.maxPos.y - childA.minPos.y, childA.maxPos.z - childA.minPos.z);
                Gizmos.DrawWireCube(((childA.minPos + childA.maxPos) / 2) * GizmoPosScale, AABBRadius * GizmoScale);

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
        Random.InitState(1422347532);

        for (int i = 0; i < count; i++)
        {
            points[i].position = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
            points[i].direction = Vector3.zero;// new Vector3(Random.Range(-size, size), Random.Range(-size, size), Random.Range(-size, size));
            points[i].color = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
        }
        return points;
    }
    void OnDestroy()
    {
        inputcomputeBuffer.Release();
        internalNodeBuffer.Release();
        leafNodeBuffer.Release();
        indexBuffer.Release();
        boundingLeafNodeBuffer.Release();
        boundingInternalNodeBuffer.Release();
        mergeOutputBuffer.Release();
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
            if (divided)
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
                problems += "Discontinuity found at " + i + "!! for values " + array[i - 2].mortonId + "," + array[i - 1].mortonId + "," + array[i].objectId + "\n";// + "," + array[i + 1].mortonId + "\n";
            if (leaf)
            {
                values += (int)array[i].mortonId / 10000000 + " ";
            }
            else
            {
                values += array[i].parentId + " Visited> " + array[i].visited + " Leaves>(" + array[i].leaves.x + "," + array[i].leaves.y + ")  " + " bLeaves>(" + array[i].bLeaves.x + "," + array[i].bLeaves.y + ")  " + " Nodes>(" + array[i].intNodes.x + ":" + array[i].intNodes.y + ") MaxPos ->" + array[i].maxPos.ToString() + "\n";
            }
            //values += array[i].parentId + " Visited> " + array[i].visited + " Leaves>(" + array[i].leaves.x + "," + array[i].leaves.y + ")  " + " bLeaves>(" + array[i].bLeaves.x + "," + array[i].bLeaves.y + ")  " + " Nodes>(" + array[i].intNodes.x + ":" + array[i].intNodes.y + ") MaxPos ->" + array[i].maxPos.ToString() + "\n";

            //if ((i != 0) && (array[i - 1].morton > array[i].morton))
            //    problems += "Discontinuity found at " + i + "!! \n";
            // values += "<" + array[i].sPos.ToString() + ":" + array[i].sRadius + "> " + "\n";// + "\n" + "<" + array[i].intNodes.x + ":" + array[i].intNodes.y + ">__" + "\n";
            //if (leaf)
            //values += array[i].parentId + ", "+ array[i].objectId + " , Morton ->" + array[i].mortonId +"\n";
            //else
            //

        }

        Debug.Log(name + " : \n" + values + "\n" + problems);
    }

}
