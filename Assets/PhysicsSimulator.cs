using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using PhysicsTools;
using System;

public class PhysicsSimulator : MonoBehaviour
{
    public ComputeBuffer inputcomputeBuffer;
    public ComputeBuffer leafNodeBuffer, internalNodeBuffer, mergeOutputBuffer, boundingLeafNodeBuffer, boundingInternalNodeBuffer;
    public ComputeBuffer AABBParentIdsBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    CommandBuffer cm;
    public ComputeShader traversalShader, sortShader, mergeShader, bvhShader, AABBShader;

    private int cachedInstanceCount = -1;

    public const int count = 131072;// 131072;//98304;//65536;//32768;//16384;
    const int groupSize = 64;
    const int mainKernelGroupSize = 64;
    const int sortMergeGroupSize = 128;
    [Range(0.001f, 0.01f)]
    public float simulationTimeStep;
    int groupAmount = count / groupSize;

    public Transform sphereCollider;

    const float size = 1f;
    [Range(0.001f, 1f)]
    public float speed = 1;
    public float angularSpeed = 0.5f;
    [Range(0,5)]
    public float viscosity = 1;
    public float gravityMul = 1;
    [Range(0.001f, 1f)]
    public float diameter = 0.05f;
    [Range(0.011f, 5f)]
    public float mass = 0.011f;
    public Vector3 gravityVec = new Vector3(0, -1, 0);

    int mainKernelHandler;
    int mortonKernelHandler;
    int treeConstructionKernelhandler;
    int boundingSphereKernelHandler;
    int unionKerneHandler;
    int sortingKernelHandler;
    int mergeKernelHandler;
    int writeNodeDataKernelHandler;
    int traversalKernelKernelHandler;

    internalNode[] nodeData = new internalNode[count - 1];
    internalNode[] leafData = new internalNode[count];
    particle[] particleData = new particle[count];

    public bool swap = false;
    int integrationStep = 8;

    //int loadKernelHandler;
    public Mesh particleMesh;
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
        AABBParentIdsBuffer = new ComputeBuffer(count, sizeof(int), ComputeBufferType.Default);
        //Apply buffer for instanced mesh drawing 
        instanceMaterial.SetBuffer("positionBuffer", inputcomputeBuffer);
        uint numIndices = (particleMesh != null) ? (uint)particleMesh.GetIndexCount(0) : 0;
        args[0] = numIndices;
        args[1] = (uint)count;
        argsBuffer.SetData(args);
        cachedInstanceCount = count;

        //Generate Initial Particle Values
        particle[] points = PUtility.GetParticlePoints(count, size, diameter/2, mass);
        inputcomputeBuffer.SetData(points);
        //Apply 
        //Get kernels
        mainKernelHandler = traversalShader.FindKernel("CSMain");
        mortonKernelHandler = traversalShader.FindKernel("CSAssignMortonIDs");
        treeConstructionKernelhandler = bvhShader.FindKernel("CSCreateBVH");
        boundingSphereKernelHandler = AABBShader.FindKernel("CSGenerateBoundingBoxes");
        mergeKernelHandler = mergeShader.FindKernel("Merge");
        sortingKernelHandler = sortShader.FindKernel("RadixSort");
        writeNodeDataKernelHandler = traversalShader.FindKernel("WriteNodeData");
        traversalKernelKernelHandler = traversalShader.FindKernel("TraversalKernel");
        unionKerneHandler = AABBShader.FindKernel("CSUnion");
        //Create MortonIDs
        traversalShader.SetBuffer(mortonKernelHandler, "inputPoints", inputcomputeBuffer);
        traversalShader.Dispatch(mortonKernelHandler, count / groupSize, 1, 1);
        Debug.Log("Dispatched morton kernel");
        //Sort & Merge IDs
        sortShader.SetBuffer(sortingKernelHandler, "Data", inputcomputeBuffer);
        mergeShader.SetBuffer(mergeKernelHandler, "inputPoints", inputcomputeBuffer);
        mergeShader.SetBuffer(mergeKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);

        traversalShader.SetBuffer(writeNodeDataKernelHandler, "internalNodes", internalNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "leafNodes", leafNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        traversalShader.SetBuffer(writeNodeDataKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);

        //Create Tree
        bvhShader.SetBuffer(treeConstructionKernelhandler, "inputPoints", inputcomputeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "internalNodes", internalNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "leafNodes", leafNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "mergeOutputBuffer", mergeOutputBuffer);
        bvhShader.SetBuffer(treeConstructionKernelhandler, "parentIds", AABBParentIdsBuffer);

        Debug.Log("Dispatched tree contruction kernel");
      

        //Create bounding sphere
        AABBShader.SetBuffer(boundingSphereKernelHandler, "internalNodes", internalNodeBuffer);
        AABBShader.SetBuffer(boundingSphereKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        AABBShader.SetBuffer(boundingSphereKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        AABBShader.SetBuffer(boundingSphereKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        AABBShader.SetBuffer(boundingSphereKernelHandler, "parentIds", AABBParentIdsBuffer);

        AABBShader.SetBuffer(unionKerneHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        AABBShader.SetBuffer(unionKerneHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        AABBShader.SetBuffer(unionKerneHandler, "parentIds", AABBParentIdsBuffer);


        traversalShader.SetBuffer(traversalKernelKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        traversalShader.SetBuffer(traversalKernelKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        traversalShader.SetBuffer(traversalKernelKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);

        //Apply Movement
        traversalShader.SetBuffer(mainKernelHandler, "boundingInternalNodes", boundingInternalNodeBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "boundingLeafNodes", boundingLeafNodeBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "mergeOutputBuffer", mergeOutputBuffer);
        traversalShader.SetBuffer(mainKernelHandler, "inputPoints", inputcomputeBuffer);

        //CommandBuffer cm = new CommandBuffer();
        //CommandBuffer cm2 = new CommandBuffer();
        //CommandBuffer cm3 = new CommandBuffer();
        //CommandBuffer cm4 = new CommandBuffer();
        //
        //
        //cm.DispatchCompute(sortShader, sortingKernelHandler, count / groupSize, 1, 1);
        //cm.DispatchCompute(mergeShader, mergeKernelHandler, count / groupSize, 1, 1);
        //cm2.DispatchCompute(traversalShader, writeNodeDataKernelHandler, count / groupSize, 1, 1);
        //cm2.DispatchCompute(bvhShader, treeConstructionKernelhandler, count / groupSize, 1, 1);
        //cm2.DispatchCompute(AABBShader, boundingSphereKernelHandler, count / groupSize, 1, 1);
        //cm4.DispatchCompute(traversalShader, traversalKernelKernelHandler, count / groupSize, 1, 1);
        //cm4.DispatchCompute(traversalShader, mainKernelHandler, count / groupSize, 1, 1);
        //
        //Camera.main.AddCommandBuffer(CameraEvent.BeforeSkybox, cm);
        //Camera.main.AddCommandBuffer(CameraEvent.BeforeSkybox, cm2);
        //Camera.main.AddCommandBuffer(CameraEvent.AfterSkybox, cm4);
        //Camera.main.AddCommandBuffer(CameraEvent.AfterSkybox, cm4);
    }

    // Update is called once per frame
    void Update()
    {
        DispatchShaders();
        Graphics.DrawMeshInstancedIndirect(particleMesh, 0, instanceMaterial, new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f)), argsBuffer);
    }

   
    void DispatchShaders()
    {
        traversalShader.SetFloat("radius", diameter / 2);
        traversalShader.SetFloat("speed", speed);
        traversalShader.SetFloat("gravity", gravityMul);
        traversalShader.SetVector("gravityVec", gravityVec);
        traversalShader.SetFloat("angularSpeed", angularSpeed);
        traversalShader.SetFloat("viscosity", viscosity);

        traversalShader.SetFloat("DeltaTime", simulationTimeStep);
        if (sphereCollider != null)
        {
            traversalShader.SetVector("sphereColliderPos", sphereCollider.transform.position);
            traversalShader.SetFloat("sphereRadius", sphereCollider.transform.lossyScale.y/2);
        }

       

        sortShader.Dispatch(sortingKernelHandler, count / sortMergeGroupSize, 1, 1);
        mergeShader.Dispatch(mergeKernelHandler, count / sortMergeGroupSize, 1, 1);
        traversalShader.Dispatch(writeNodeDataKernelHandler, count / groupSize, 1, 1);
        bvhShader.Dispatch(treeConstructionKernelhandler, count / groupSize, 1, 1);
        AABBShader.Dispatch(boundingSphereKernelHandler, count / groupSize, 1, 1);

        //Cpu AABB implementation here
        //boundingInternalNodeBuffer.GetData(nodeData);
        //boundingLeafNodeBuffer.GetData(leafData);
        //CreateBoundingBoxes(ref nodeData,ref leafData);
        //boundingInternalNodeBuffer.SetData(nodeData);

        traversalShader.Dispatch(traversalKernelKernelHandler, count / mainKernelGroupSize, 1, 1);
        traversalShader.Dispatch(mainKernelHandler, count / mainKernelGroupSize, 1, 1);
    }

    void CreateBoundingBox(internalNode node,ref internalNode[] internalNodes,ref internalNode[] leafNodes)
    {
        internalNode ChildA;
        internalNode ChildB;
        PhysicsDebugger.GetNodeChildren(node, out ChildA, out ChildB, ref leafNodes, ref internalNodes);

        if (!PhysicsDebugger.isLeaf(ChildA))
        {
            CreateBoundingBox(ChildA,ref internalNodes,ref leafNodes);
        }
        if (!PhysicsDebugger.isLeaf(ChildB))
        {
            CreateBoundingBox(ChildB,ref internalNodes,ref leafNodes);
        }

        Vector3 minPoint;
        Vector3 maxPoint;
        PhysicsDebugger.CalculateAABB(node, out minPoint, out maxPoint, ref internalNodes, ref leafNodes);
        internalNodes[node.nodeId].minPos = minPoint;
        internalNodes[node.nodeId].maxPos = maxPoint;
    }


    void CreateBoundingBoxes(ref internalNode[] internalNodes, ref internalNode[] leafNodes)
    {
        CreateBoundingBox(internalNodes[0],ref internalNodes,ref leafNodes);
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

    public bool debuggingEnabled = false;

    void OnDrawGizmos()
    {
        debuggingEnabled = visualizeHeirarchy || visualizeBoundingBoxes || visualizePotentialCollisions || visualizeBVHTree;
        if (!debuggingEnabled)
            return;
        if (boundingInternalNodeBuffer != null)
            boundingInternalNodeBuffer.GetData(nodeData);
        if (boundingLeafNodeBuffer != null)
            boundingLeafNodeBuffer.GetData(leafData);
        if (mergeOutputBuffer != null)
            mergeOutputBuffer.GetData(particleData);

        if(showVelocities)
            PhysicsDebugger.ShowVelocities(ref particleData);
        if (visualizeHeirarchy)
            PhysicsDebugger.FindRoot(ref leafData, ref nodeData, ref particleData, heirarchyLeaf);
        if (visualizeBoundingBoxes)
            PhysicsDebugger.VisualizeBoundingBoxes(ref nodeData, ref leafData, boundingBoxMin);
        if (visualizePotentialCollisions)
            PhysicsDebugger.VisualisePotentialCollisions(ref leafData, ref nodeData, ref particleData, heirarchyLeafToCheckForCollision, diameter);
        if (visualizeBVHTree)
            PhysicsDebugger.VisualizeBVHTree(ref nodeData, ref leafData, treeScale, leafData[heirarchyLeafToCheckForCollision]);
    }

    void OnDestroy()
    {
        if(inputcomputeBuffer != null)
            inputcomputeBuffer.Release();

        internalNodeBuffer.Release();
        leafNodeBuffer.Release();
        boundingLeafNodeBuffer.Release();
        boundingInternalNodeBuffer.Release();
        mergeOutputBuffer.Release();
        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
        AABBParentIdsBuffer.Release();
    }

    private void Print(string name, Vector3[] array)
    {
        string values = "";

        for (int i = 0; i < array.Length; i++)
        {
            values += array[i] + " ";
        }

        Debug.Log(name + " :  " + values + "\n");
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
        }

        Debug.Log(name + " : \n" + values + "\n" + problems);
    }

}
