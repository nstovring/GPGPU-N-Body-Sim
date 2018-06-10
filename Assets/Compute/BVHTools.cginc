struct particle
{
    float3 position;
    float3 direction;
    float3 color;
    float radius;
    float density;
    float pressure;
    float mass;
    uint mortonId;
};

struct internalNode
{
    int objectId;
    int nodeId;
    int parentId;
    int2 intNodes;
    int2 leaves;
    int2 bLeaves;
    float3 minPos;
    float3 maxPos;
    float sRadius;
    int visited;
    uint mortonId;
};


#define ThreadX 64
#define MainKernelThreadX 64
#define SortMergeTreadX 128

static const float PI = 3.14159265f;

RWStructuredBuffer<particle> inputPoints : register(u0);
RWStructuredBuffer<particle> mergeOutputBuffer : register(u1);
RWStructuredBuffer<internalNode> internalNodes : register(u2);
RWStructuredBuffer<internalNode> leafNodes : register(u3);
RWStructuredBuffer<internalNode> boundingInternalNodes : register(u4);
RWStructuredBuffer<internalNode> boundingLeafNodes : register(u5);
RWStructuredBuffer<int> parentIds : register(u6);

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
uint morton3D(float x, float y, float z)
{
    x = min(max(x * 1024.0f, 0.0f), 1023.0f);
    y = min(max(y * 1024.0f, 0.0f), 1023.0f);
    z = min(max(z * 1024.0f, 0.0f), 1023.0f);
    uint xx = expandBits((uint) x);
    uint yy = expandBits((uint) y);
    uint zz = expandBits((uint) z);
    return xx * 4 + yy * 2 + zz;
}



bool isLeaf(internalNode node)
{
    if (node.objectId != -1)
        return true;
    return false;
}

void GetChildren(internalNode node, out internalNode childA, out internalNode childB)
{
    int2 leaves = node.leaves;
    int2 intNodes = node.intNodes;
    childA.nodeId = -1;
    childB.nodeId = -1;

    if (leaves.x != -1)
    {
        childA = boundingLeafNodes[leaves.x];
        //boundingInternalNodes[leaves.x].parentId = node.nodeId;
    }
    if (leaves.y != -1)
    {
        childB = boundingLeafNodes[leaves.y];
        //boundingLeafNodes[leaves.y].parentId = node.nodeId;
    }
    if (intNodes.x != -1)
    {
        childA = boundingInternalNodes[intNodes.x];
        //boundingLeafNodes[intNodes.x].parentId = node.nodeId;
    }
    if (intNodes.y != -1)
    {
        childB = boundingInternalNodes[intNodes.y];
        //boundingInternalNodes[intNodes.y].parentId = node.nodeId;
    }
}


bool AABBOverlap(float3 minA, float3 maxA, float3 minB, float3 maxB)
{
    return (minA.x <= maxB.x && maxA.x >= minB.x) &&
         (minA.y <= maxB.y && maxA.y >= minB.y) &&
         (minA.z <= maxB.z && maxA.z >= minB.z);
}

internalNode GetRoot()
{
    return boundingInternalNodes[0];
}

