#include "BVHTools.cginc"


void TraverseBVHIterative(internalNode leaf, float radius, out int collisionList[32])
{
    internalNode node = GetRoot();
    int stack[64];

    for (uint i = 0; i < 64; i++)
    {
        stack[i] = -2;
        collisionList[i % 32] = -1;
    }

    int traversalCount = 0;
    int collisionCount = 0;
    int maxLoop = 0;
    bool solved = false;

    for (int j = 0; j < 512; j++)
    {
        if (!solved)
        {
            internalNode childA;
            internalNode childB;
            GetChildren(node, childA, childB);

            float3 AABBRadius = float3(radius, radius, radius) * 1;

            bool overlapA = AABBOverlap(leaf.minPos, leaf.maxPos, childA.minPos, childA.maxPos);
            bool overlapB = AABBOverlap(leaf.minPos, leaf.maxPos, childB.minPos, childB.maxPos);

            //if (!isLeaf(childA) && AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, leafNodes[childA.bLeaves.y].minPos, leafNodes[childA.bLeaves.y].maxPos))
            //    overlapA = false;
            //
            //if (!isLeaf(childB) && AABBOverlap(leaf.minPos - AABBRadius, leaf.maxPos + AABBRadius, leafNodes[childB.bLeaves.y].minPos, leafNodes[childB.bLeaves.y].maxPos))
            //    overlapB = false;

            if (overlapA && isLeaf(childA) && childA.objectId != leaf.objectId)
            {
                collisionList[collisionCount] = childA.objectId;
                collisionCount++;
            }
            if (overlapB && isLeaf(childB) && childB.objectId != leaf.objectId)
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
                if (stack[traversalCount] == -1)
                {
                    solved = true;
                    break;
                }
                node = boundingInternalNodes[stack[traversalCount]];
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
                    traversalCount++;
                }
            }
        }
    }
}
