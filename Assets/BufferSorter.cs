using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using PhysicsTools;
using System;

public class BufferSorter : MonoBehaviour
{
    public ComputeBuffer inputcomputeBuffer;
    //public ComputeBuffer sortcomputeBuffer;
    public ComputeBuffer leafNodeBuffer, internalNodeBuffer, indexBuffer, mergeOutputBuffer, boundingLeafNodeBuffer, boundingInternalNodeBuffer, velocityBuffer;//,o ,e ,f, d;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    CommandBuffer cm;
    public ComputeShader traversalShader, sortShader, mergeShader, bvhShader, AABBShader;
    public Material mat;
    public Material ballmat;

    RenderTexture pointRt;
    RenderTexture velRt;
    //const int count = 10554096;
    private int cachedInstanceCount = -1;

    public const int count = 256;//32768;
    const int groupSize = 256;
    const int mainKernelGroupSize = 32;
    int groupAmount = count / groupSize;

    const float size = 1f;
    [Range(0.001f, 1f)]
    public float speed = 1;
    public float angularSpeed = 0.5f;
    public float gravityMul = 1;
    [Range(0.001f, 1f)]
    public float diameter = 0.05f;
    public Vector3 gravityVec = new Vector3(0, -1, 0);



    int mainKernelHandler;
    int mortonKernelHandler;
    int treeConstructionKernelhandler;
    int boundingSphereKernelHandler;
    int sortingKernelHandler;
    int mergeKernelHandler;
    int writeNodeDataKernelHandler;
    int traversalKernelKernelHandler;

    //int loadKernelHandler;
    public Mesh cubeMesh;
    public Material instanceMaterial;

   
    // Use this for initialization
    void Start()
    {
        PhysicsDebugger.GizmoPosScale = GizmoPosScale;
        PhysicsDebugger.GizmoScale = GizmoScale;

        //Initialize variables
        traversalShader.SetFloat("radius", diameter / 2);
        traversalShader.SetFloat("speed", speed);
        traversalShader.SetFloat("gravity", gravityMul);
        traversalShader.SetVector("gravityVec", gravityVec);
        traversalShader.SetFloat("angularSpeed", angularSpeed);
        traversalShader.SetFloat("DeltaTime", Time.deltaTime / integrationStep);
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
        velocityBuffer = new ComputeBuffer(count, sizeof(float) * 3, ComputeBufferType.Default);
        //Apply buffer for instanced mesh drawing 
        instanceMaterial.SetBuffer("positionBuffer", inputcomputeBuffer);
        uint numIndices = (cubeMesh != null) ? (uint)cubeMesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = (uint)count;
        argsBuffer.SetData(args);
        cachedInstanceCount = count;

        //Generate Initial Particle Values
        particle[] points = PUtility.GetParticlePoints(count, size);
        inputcomputeBuffer.SetData(points);
        //Apply 
        //Get kernels
        mainKernelHandler = traversalShader.FindKernel("CSMain");
        mortonKernelHandler = traversalShader.FindKernel("CSSortMortonIDs");
        treeConstructionKernelhandler = bvhShader.FindKernel("CSCreateBVH");
        boundingSphereKernelHandler = AABBShader.FindKernel("CSGenerateBoundingBoxes");
        mergeKernelHandler = mergeShader.FindKernel("Merge");
        sortingKernelHandler = sortShader.FindKernel("RadixSort");
        writeNodeDataKernelHandler = traversalShader.FindKernel("WriteNodeData");
        traversalKernelKernelHandler = traversalShader.FindKernel("TraversalKernel");
        //Create MortonIDs
        traversalShader.SetBuffer(mortonKernelHandler, "inputPoints", inputcomputeBuffer);
        traversalShader.Dispatch(mortonKernelHandler, count / groupSize, 1, 1);
        Debug.Log("Dispatched morton kernel");
        ////Sort IDs
        sortShader.SetBuffer(sortingKernelHandler, "Data", inputcomputeBuffer);
        sortShader.Dispatch(sortingKernelHandler, count / groupSize, 1, 1);

        mergeShader.SetBuffer(mergeKernelHandler, "inputPoints", inputcomputeBuffer);
        mergeShader.SetBuffer(mergeKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        mergeShader.Dispatch(mergeKernelHandler, count / groupSize, 1, 1);

        Debug.Log("Dispatched sorting kernel");


        traversalShader.SetBuffer(writeNodeDataKernelHandler, "internalNodes", internalNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "leafNodes", leafNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);

     

        traversalShader.Dispatch(writeNodeDataKernelHandler, count / groupSize, 1, 1);
        //Create Tree
        bvhShader.SetBuffer(treeConstructionKernelhandler, "inputPoints", inputcomputeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "internalNodes", internalNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "leafNodes", leafNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "mergeOutputBuffer", mergeOutputBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "indexBuffer", indexBuffer);

        bvhShader.Dispatch(treeConstructionKernelhandler, (count) / groupSize, 1, 1);
        
        Debug.Log("Dispatched tree contruction kernel");
        
        //boundingInternalNodeBuffer.GetData(nodeData);
        //Print("Node Data", nodeData, false);
        ////internalNode[] nodeData = new internalNode[count];
        //
        //boundingLeafNodeBuffer.GetData(nodeData);
        //Print("Leaf Data", nodeData, false);

        //Create bounding sphere
        AABBShader.SetBuffer(boundingSphereKernelHandler, "internalNodes", internalNodeBuffer);
        //computeShader.SetBuffer(boundingSphereKernelHandler, "leafNodes", leafNodeBuffer);
        AABBShader.SetBuffer(boundingSphereKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        AABBShader.SetBuffer(boundingSphereKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        AABBShader.SetBuffer(boundingSphereKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        AABBShader.Dispatch(boundingSphereKernelHandler, count / groupSize, 1, 1);
        Debug.Log("Dispatched bounding calculation kernel");

        //internalNodeBuffer.GetData(nodeData);
        //Print("Node Data", nodeData, false);


        traversalShader.SetBuffer(traversalKernelKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        traversalShader.SetBuffer(traversalKernelKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        traversalShader.SetBuffer(traversalKernelKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        traversalShader.SetBuffer(traversalKernelKernelHandler, "velocityBuffer", velocityBuffer);

        traversalShader.Dispatch(traversalKernelKernelHandler, count / mainKernelGroupSize, 1, 1);

        //Apply Movement
        //computeShader.SetBuffer(mainKernelHandler, "internalNodes", internalNodeBuffer);
        //computeShader.SetBuffer(mainKernelHandler, "leafNodes", leafNodeBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "inputPoints", inputcomputeBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "velocityBuffer", velocityBuffer);

        traversalShader.Dispatch(mainKernelHandler, count / mainKernelGroupSize, 1, 1);
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
    Vector3[] velocityData = new Vector3[count];
    int[] indexData = new int[count];

    public bool swap = false;
    int integrationStep = 8;
    void DispatchShaders()
    {
        traversalShader.SetFloat("radius", diameter / 2);
        traversalShader.SetFloat("speed", speed);
        traversalShader.SetFloat("gravity", gravityMul);
        traversalShader.SetVector("gravityVec", gravityVec);
        traversalShader.SetFloat("angularSpeed", angularSpeed);
        traversalShader.SetFloat("DeltaTime", Time.deltaTime / integrationStep);
        sortShader.Dispatch(sortingKernelHandler, count / groupSize, 1, 1);
        mergeShader.Dispatch(mergeKernelHandler, count / groupSize, 1, 1);
        traversalShader.Dispatch(writeNodeDataKernelHandler, count / groupSize, 1, 1);
        bvhShader.Dispatch(treeConstructionKernelhandler, (count) / groupSize, 1, 1);
        AABBShader.Dispatch(boundingSphereKernelHandler, count / groupSize, 1, 1);
        traversalShader.Dispatch(traversalKernelKernelHandler, count / mainKernelGroupSize, 1, 1);
        traversalShader.Dispatch(mainKernelHandler, count / mainKernelGroupSize, 1, 1);

        if (swap)
        {
            velocityBuffer.GetData(velocityData);
            Print("Velocity data", velocityData);
            internalNodeBuffer.GetData(nodeData);
            Print("Node Data", nodeData, false);
            boundingInternalNodeBuffer.GetData(nodeData);
            Print("Bounding Node Data", nodeData, false);
            boundingLeafNodeBuffer.GetData(leafData);
            indexBuffer.GetData(indexData);
            Print("Index Data", indexData);
            Print("Leaf Data", leafData, false);
            swap = false;
        }

        if (checkSorted)
        {
            mergeOutputBuffer.GetData(particleData);
            Debug.Log(Sorted(particleData));
            Debug.Log("Duplicates: " + Duplicates(particleData));
        }

    }

    private void Print(string name, Vector3[] array)
    {
        string values = "";

        for (int i = 0; i < array.Length; i++)
        {
            values += array[i] + " ";
        }

        Debug.Log(name + " :  " + values + "\n" );
    }

    [Header("Debug")]
    public bool checkSorted = false;
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
    public bool showVelocities = false;
    [Range(0, 2)]
    public float treeScale = 0.55f;
    public int currentCollisions = 0;

    void OnDrawGizmos()
    {
        boundingInternalNodeBuffer.GetData(nodeData);
        boundingLeafNodeBuffer.GetData(leafData);
        mergeOutputBuffer.GetData(particleData);
        if(showVelocities)
        PhysicsDebugger.ShowVelocities(ref particleData);
        if (visualizeHeirarchy)
        {
            PhysicsDebugger.FindRoot(ref leafData, ref nodeData, ref particleData, heirarchyLeaf);
        }
        else if (visualizeBoundingBoxes)
        {
            PhysicsDebugger.VisualizeBoundingBoxes(ref nodeData, ref leafData, boundingBoxMin);
        }
        else if (visualizePotentialCollisions)
        {
            PhysicsDebugger.VisualisePotentialCollisions(ref leafData, ref nodeData, ref particleData, heirarchyLeafToCheckForCollision, diameter);
        }

        if (visualizeBVHTree)
        {
            PhysicsDebugger.VisualizeBVHTree(ref nodeData, ref leafData, treeScale);
            //swap = false;
        }
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
        velocityBuffer.Release();
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

    int Duplicates(particle[] array)
    {
        int count = 0;
        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1].morton == array[i].morton))
            {
                count++;
            }
        }
        return count;
    }

    bool Sorted(internalNode[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1].mortonId > array[i].mortonId))
            {
                return false;
            }
        }
        return true;
    }
    bool Sorted(particle[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if ((i != 0) && (array[i - 1].morton > array[i].morton))
            {
                return false;
            }
        }
        return true;
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
