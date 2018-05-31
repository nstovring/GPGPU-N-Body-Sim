using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhysicsTools;
public static class PhysicsDebugger {

    public static void FindRoot(ref internalNode[] leafData, ref internalNode[] nodeData, ref particle[] particleData, int heirarchyLeaf)
    {
        int parentId = leafData[heirarchyLeaf].parentId;
        int maxCount = 0;
        int childId = parentId;
        while (maxCount < 32)
        {
            if (parentId == -1)
            {
                Debug.Log("Reached Root");
                break;
            }
            if (parentId >= nodeData.Length || parentId < 0)
            {
                Debug.Log("Failed Root " + nodeData[childId].parentId + " Visited> " + nodeData[childId].visited + " Leaves>(" + nodeData[childId].leaves.x + "," + nodeData[childId].leaves.y + ")  " + " bLeaves>(" + nodeData[childId].bLeaves.x + "," + nodeData[childId].bLeaves.y + ")  " + " Nodes>(" + nodeData[childId].intNodes.x + ":" + nodeData[childId].intNodes.y + ") MaxPos ->" + nodeData[childId].maxPos.ToString() + "\n");
                break;
            }
            childId = parentId;
            parentId = nodeData[parentId].parentId;
          
          
            maxCount++;
        }
    }

    public static void ShowVelocities(ref particle[] particleData)
    {
        for (int i = 0; i < particleData.Length; i++)
        {
            particle p = particleData[i];
            Debug.DrawRay(p.position * GizmoPosScale, p.direction);
        }
    }

    public static void VisualisePotentialCollisions(ref internalNode[] leafData, ref internalNode[] nodeData, ref particle[] particleData, int heirarchyLeaf, float diameter)
    {
        int[] collisions;
        int leaf = 0;
        for (int i = 0; i < leafData.Length; i++)
        {
            if (leafData[i].objectId == heirarchyLeaf)
            {
                leaf = i;
            }
        }

        TraverseBVHIterative(leafData[leaf], diameter, out collisions,ref nodeData, ref leafData);
        Vector3 leafPos = particleData[leafData[leaf].objectId].position;
        Vector3 AABBRadius = new Vector3(diameter / 2, diameter / 2, diameter / 2);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(((leafData[leaf].minPos + leafData[leaf].maxPos) / 2) * GizmoPosScale, AABBRadius * GizmoScale);
        Gizmos.color = Color.white;
        int collisionCount = 0;
        string output = "";
        for (int i = 0; i < collisions.Length; i++)
        {
            int col = collisions[i];
            if (col != -1)
            {
                Gizmos.DrawLine(leafPos * GizmoPosScale, particleData[col].position * GizmoScale);
                Gizmos.DrawWireSphere(particleData[col].position * GizmoPosScale, (diameter / 2) * GizmoScale);
                output +=  col + ",";
                //Debug.Log(AABBOverlap(leafPos - AABBRadius, leafPos + AABBRadius, particleData[col].position - AABBRadius, particleData[col].position + AABBRadius));
            }
        }

        //Debug.Log("Collision Count " + output);
    }

 

    public static void GetNodeChildren(internalNode node, out internalNode childA, out internalNode childB, ref internalNode[] leafData, ref internalNode[] nodeData)
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

    public static void DrawCube(internalNode node)
    {
        Vector3 center = (node.minPos + node.maxPos) / 2;
        Vector3 scale = new Vector3(node.maxPos.x - node.minPos.x, node.maxPos.y - node.minPos.y, node.maxPos.z - node.minPos.z);
        Gizmos.DrawWireCube(center * GizmoPosScale, scale * GizmoPosScale);
    }
    static bool AABBOverlap(Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
    {
        bool x = minA.x <= maxB.x && minB.x <= maxA.x;
        bool y = minA.y <= maxB.y && minB.y <= maxA.y;
        bool z = minA.z <= maxB.z && minB.z <= maxA.z;

        return (x && y && z);// || (!x && !y && !z);

        return (minA.x <= maxB.x && maxA.x >= minB.x) &&
            (minA.y <= maxB.y && maxA.y >= minB.y) &&
            (minA.z <= maxB.z && maxA.z >= minB.z);
    }

    public static bool isLeaf(internalNode node)
    {
        if (node.objectId != -1)
            return true;
        return false;
    }

    static void TraverseBVHIterative(internalNode leaf, float radius, out int[] collisionList, ref internalNode[] internalNodeData, ref internalNode[] leafData)
    {
        internalNode node = internalNodeData[0];
        int[] stack = new int[64];
        collisionList = new int[64];
        for (uint i = 0; i < 64; i++)
        {
            stack[i] = -2;
            collisionList[i] = -1;
        }

        int traversalCount = 0;
        int collisionCount = 0;
        int maxLoop = 0;
        Gizmos.color = Color.green;
        Vector3 AABBRadius = new Vector3(radius, radius, radius) * 1;
        Vector3 scale = AABBRadius;

        do
        {
            internalNode childA;
            internalNode childB;
            GetNodeChildren(node, out childA, out childB, ref leafData,ref internalNodeData);

            AABBRadius = new Vector3(radius, radius, radius) * 0;
            bool overlapA = AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, childA.minPos - AABBRadius, childA.maxPos +AABBRadius);
            bool overlapB = AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, childB.minPos - AABBRadius, childB.maxPos + AABBRadius);

            if (overlapB)
            {
                Gizmos.color = Color.green;
                DrawCube(childB);
            }
            if (overlapA)
            {
                Gizmos.color = Color.red;
                DrawCube(childA);
            }

            if (overlapA && isLeaf(childA) )
            {
                collisionList[collisionCount] = childA.objectId;
                collisionCount++;
            }
            if (overlapB && isLeaf(childB) )
            {
                collisionList[collisionCount] = childB.objectId;
                collisionCount++;
            }

            bool traverseA = (overlapA && !isLeaf(childA));
            bool traverseB = (overlapB && !isLeaf(childB));

            if (!traverseA && !traverseB)
            {
                stack[traversalCount] = -1;
                traversalCount--;
                traversalCount = traversalCount <= 0 ? 0 : traversalCount;
                //Debug.Log("Popping Stack: " + traversalCount);
                if (stack[traversalCount] == -1)
                {
                    return;
                }
                node = internalNodeData[stack[traversalCount]];
            }
            else
            {
                if (traverseA)
                {
                    node = childA;
                    //Gizmos.color = Color.red;
                    //DrawCube(node);
                }
                else
                {
                    node = childB;
                    //Gizmos.color = Color.green;
                    //DrawCube(node);
                }

                if (traverseA && traverseB)
                {
                    stack[traversalCount] = childB.nodeId;
                    traversalCount++;
                    //Debug.Log("Pushing Stack");
                }
            }
            maxLoop++;
        } while (stack[traversalCount] != -1);//traversing && traversalCount < 64);
    }


    public static float GizmoPosScale;
    public static float GizmoScale;

    public static void VisualizeBoundingBoxes(ref internalNode[] nodeData, ref internalNode[] leafData, int boundingBoxMin)
    {
        //CreateBoundingBoxes(ref nodeData,ref leafData);
        sortedNodeData = new List<internalNode>();
        DrawBoundingRecursive(nodeData[0], ref nodeData, ref leafData);
        Vector3 offset = Vector3.zero;
        Vector3 scale = Vector3.zero;
     
        for (int i = boundingBoxMin; i < sortedNodeData.Count; i++)
        {
            internalNode node = sortedNodeData[i];
            //offset += new Vector3(scale.x, 0, 0);
            //Gizmos.color =  Color.white * (node.overlap);
            Vector3 center = (node.minPos + node.maxPos) / 2 + offset;
            scale = new Vector3(node.maxPos.x - node.minPos.x, node.maxPos.y - node.minPos.y, node.maxPos.z - node.minPos.z);
            Gizmos.DrawWireCube(center * GizmoPosScale, scale * GizmoScale);
        }
    }

   
    static List<internalNode> sortedNodeData;
    static void DrawBoundingRecursive(internalNode node,ref internalNode[] nodeData, ref internalNode[] leafData)
    {
        sortedNodeData.Add(node);
        internalNode ChildA;
        internalNode ChildB;

        //Vector3 center = (node.minPos + node.maxPos) / 2;
        //Vector3 scale = new Vector3(node.maxPos.x - node.minPos.x, node.maxPos.y - node.minPos.y, node.maxPos.z - node.minPos.z);
        //Gizmos.DrawWireCube(center * GizmoPosScale, scale * GizmoScale);

        GetNodeChildren(node, out ChildA, out ChildB, ref leafData, ref nodeData);
        //Debug.DrawLine(origin, origin + new Vector3(-2 * scale, -1, 2));
        //Debug.DrawLine(origin, origin + new Vector3(2 * scale, -1, -2));
        //DrawNode(ChildA, origin + new Vector3(-2 * scale, -1, 2));
        //DrawNode(ChildB, origin + new Vector3(2 * scale, -1, -2));

        if (!isLeaf(ChildA))
            DrawBoundingRecursive(ChildA, ref nodeData, ref leafData);
        if (!isLeaf(ChildB))
            DrawBoundingRecursive(ChildB, ref nodeData, ref leafData);
    }

    static void DrawTreeRecursive(internalNode root, Vector3 origin, float scale, ref internalNode[] nodeData, ref internalNode[] leafData, internalNode sampleNode)
    {
        internalNode ChildA;
        internalNode ChildB;

        GetNodeChildren(root, out ChildA, out ChildB, ref leafData, ref nodeData);
        Vector3 AABBRadius = new Vector3(sampleNode.sRadius, sampleNode.sRadius, sampleNode.sRadius);
        if (AABBOverlap(sampleNode.minPos, sampleNode.maxPos, root.minPos, root.maxPos))
            Gizmos.color = Color.blue;
        else
            Gizmos.color = Color.white;

        Vector3 nScale = new Vector3(root.maxPos.x - root.minPos.x, root.maxPos.y - root.minPos.y, root.maxPos.z - root.minPos.z);
        Gizmos.DrawWireCube(origin, nScale);

        Color semiTransparent = new Color(1, 1, 1, 0.5f);
        Debug.DrawLine(origin, origin + new Vector3(-2 * scale, -1, 2), semiTransparent);
        Debug.DrawLine(origin, origin + new Vector3(2 * scale, -1, -2), semiTransparent);
        DrawNode(ChildA, origin + new Vector3(-2 * scale, -1, 2), ChildA.maxPos - ChildA.minPos);
        DrawNode(ChildB, origin + new Vector3(2 * scale, -1, -2), ChildB.maxPos - ChildB.minPos);

        if (!isLeaf(ChildA))
            DrawTreeRecursive(ChildA, origin + new Vector3(-2 * scale, -1, 2), scale * 0.55f, ref nodeData, ref leafData, sampleNode);
        if (!isLeaf(ChildB))
            DrawTreeRecursive(ChildB, origin + new Vector3(2 * scale, -1, -2), scale * 0.55f, ref nodeData, ref leafData, sampleNode);
    }


    static void DrawTreeRecursive(internalNode root, Vector3 origin, float scale, ref internalNode[] nodeData, ref internalNode[] leafData)
    {
        internalNode ChildA;
        internalNode ChildB;

        GetNodeChildren(root, out ChildA, out ChildB, ref leafData, ref nodeData);

        Gizmos.color = Color.blue;
        Vector3 nScale = new Vector3(root.maxPos.x - root.minPos.x, root.maxPos.y - root.minPos.y, root.maxPos.z - root.minPos.z);
        Gizmos.DrawWireCube(origin,  nScale);

        Color semiTransparent = new Color(1, 1, 1, 0.5f);
        Debug.DrawLine(origin, origin + new Vector3(-2 * scale, -1, 2),semiTransparent);
        Debug.DrawLine(origin, origin + new Vector3(2 * scale, -1, -2), semiTransparent);
        DrawNode(ChildA, origin + new Vector3(-2 * scale, -1, 2), ChildA.maxPos - ChildA.minPos);
        DrawNode(ChildB, origin + new Vector3(2 * scale, -1, -2), ChildB.maxPos - ChildB.minPos);

        if (!isLeaf(ChildA))
            DrawTreeRecursive(ChildA, origin + new Vector3(-2 * scale, -1, 2), scale * 0.55f, ref nodeData, ref leafData);
        if (!isLeaf(ChildB))
            DrawTreeRecursive(ChildB, origin + new Vector3(2 * scale, -1, -2), scale * 0.55f, ref nodeData, ref leafData);
    }

    public static void VisualizeBVHTree(ref internalNode[] nodeData, ref internalNode[] leafData, float treeScale)
    {
        internalNode root = nodeData[0];
        DrawNode(root, Vector3.zero, root.maxPos - root.minPos);
        DrawTreeRecursive(root, Vector3.zero, 2f * treeScale, ref nodeData, ref leafData);
    }


    public static void VisualizeBVHTree(ref internalNode[] nodeData, ref internalNode[] leafData, float treeScale, internalNode sampleNode)
    {
        
        internalNode root = nodeData[0];
        DrawNode(root, Vector3.zero, root.maxPos - root.minPos);
        DrawTreeRecursive(root, Vector3.zero, 2f * treeScale, ref nodeData, ref leafData, sampleNode);
    }

    static void DrawNode(internalNode node, Vector3 pos)
    {
        Gizmos.color = Color.white;// / node.visited;
        Gizmos.DrawSphere(pos, 0.5f);
    }

    static void DrawNode(internalNode node, Vector3 pos, Vector3 AABBVector)
    {
        Gizmos.color = Color.red * AABBVector.magnitude;// new Color(AABBVector.normalized.x, AABBVector.normalized.y, AABBVector.normalized.z) * 2;// / node.visited;
        if (isLeaf(node))
            Gizmos.color = Color.green;// / node.visited;
        Gizmos.DrawSphere(pos, 0.5f);
    }

    static void CreateBoundingBoxes(ref internalNode[] nodeData, ref internalNode[] leafData)
    {
        for (int j = 0; j < leafData.Length; j++)
        {
            int parentId = leafData[j].parentId;
            internalNode parent;
            //[unroll(log2(count))]
            for (int i = 0; i < 64; i++)
            {
                //InterlockedAdd(boundingInternalNodes[parentId].visited, 1);
                nodeData[parentId].visited++;
                //if (nodeData[parentId].visited < 2)
                //{
                //    return;
                //}

                parent = nodeData[parentId];

                Vector3 Min;
                Vector3 Max;
                CalculateAABB(parent,out Min, out Max,ref nodeData,ref leafData);

                Vector3 center = (parent.minPos + parent.maxPos) / 2;
                Vector3 scale = new Vector3(Max.x - Min.x, Max.y - Min.y, Max.z - Min.z);
                Gizmos.DrawWireCube(center * GizmoPosScale, scale * GizmoScale);

                nodeData[parentId] = parent;
                nodeData[parentId].minPos = Min;
                nodeData[parentId].maxPos = Max;
                parentId = parent.parentId;

                //DeviceMemoryBarrierWithGroupSync();

                if (parentId == -1)
                {
                    break;
                }
            }
        }
    }

    public static void CalculateAABB(Vector3 inMinA, Vector3 inMaxA, Vector3 inMinB, Vector3 inMaxB, out Vector3 minPoint, out Vector3 maxPoint)
    {
        Vector3 posAA = inMinA;
        Vector3 posAB = inMaxA;
        Vector3 posBA = inMinB;
        Vector3 posBB = inMaxB;
        float xmin = Mathf.Min(Mathf.Min(posBA.x, posBB.x), Mathf.Min(posAA.x, posAB.x));
        float ymin = Mathf.Min(Mathf.Min(posBA.y, posBB.y), Mathf.Min(posAA.y, posAB.y));
        float zmin = Mathf.Min(Mathf.Min(posBA.z, posBB.z), Mathf.Min(posAA.z, posAB.z));

        float xmax = Mathf.Max(Mathf.Max(posBA.x, posBB.x), Mathf.Max(posAA.x, posAB.x));
        float ymax = Mathf.Max(Mathf.Max(posBA.y, posBB.y), Mathf.Max(posAA.y, posAB.y));
        float zmax = Mathf.Max(Mathf.Max(posBA.z, posBB.z), Mathf.Max(posAA.z, posAB.z));

        minPoint = new Vector3(xmin, ymin, zmin);
        maxPoint = new Vector3(xmax, ymax, zmax);
    }

    public static void CalculateAABB(internalNode node, out Vector3 minPoint, out Vector3 maxPoint, ref internalNode[] nodeData, ref internalNode[] leafData)
    {

        internalNode childA;
        internalNode childB;
        GetNodeChildren(node, out childA, out childB, ref leafData, ref nodeData);
        Vector3 posAA = childA.minPos;
        Vector3 posBA = childB.minPos;

        Vector3 posAB = childA.maxPos;
        Vector3 posBB = childB.maxPos;
        float xmin = Mathf.Min(posAA.x, posBA.x);//Mathf.Min(Mathf.Min(posBA.x, posBB.x), Mathf.Min(posAA.x, posAB.x));
        float ymin = Mathf.Min(posAA.y, posBA.y);//Mathf.Min(Mathf.Min(posBA.y, posBB.y), Mathf.Min(posAA.y, posAB.y));
        float zmin = Mathf.Min(posAA.z, posBA.z);//Mathf.Min(Mathf.Min(posBA.z, posBB.z), Mathf.Min(posAA.z, posAB.z));

        float xmax = Mathf.Max(posAB.x, posBB.x);//Mathf.Max(Mathf.Max(posBA.x, posBB.x), Mathf.Max(posAA.x, posAB.x));
        float ymax = Mathf.Max(posAB.y, posBB.y);//Mathf.Max(Mathf.Max(posBA.y, posBB.y), Mathf.Max(posAA.y, posAB.y));
        float zmax = Mathf.Max(posAB.z, posBB.z);//Mathf.Max(Mathf.Max(posBA.z, posBB.z), Mathf.Max(posAA.z, posAB.z));

        minPoint = new Vector3(xmin, ymin, zmin);
        maxPoint = new Vector3(xmax, ymax, zmax);
    }

}
