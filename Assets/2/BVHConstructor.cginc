#include "BVHTools.cginc"

//LVBH Construction From here on
//Count leading zeros method
int clz1(uint x)
{
    int n;
    if (x == 0)
        return 32;

    for (n = 0; ((x & 0x80000000) == 0); n++, x <<= 1);
    return n;
}
int clz3(uint x)
{
    if (x == 0)
    {
        return 32;
    }

    uint n = 0;
    if ((x & 0xFFFF0000) == 0)
    {
        n = n + 16;
        x = x << 16;
    }
    if ((x & 0xFF000000) == 0)
    {
        n = n + 8;
        x = x << 8;
    }
    if ((x & 0xF0000000) == 0)
    {
        n = n + 4;
        x = x << 4;
    }
    if ((x & 0xC0000000) == 0)
    {
        n = n + 2;
        x = x << 2;
    }
    if ((x & 0x80000000) == 0)
    {
        n = n + 1;
    }
    return n;
}
//Deeper Hierarchy functions:
int findSplit(int first, int last)
{
    uint firstCode = leafNodes[first].mortonId;
    uint lastCode = leafNodes[last].mortonId;
    
    //When morton codes are identical we want to return the first id
    //because if we split it in the middle, both will try to orientate
    //towards the same direction
    
    //to split it in the middle would be the best but since it would be harder
    //for the range detector we do it this way (both ways less instructions needed:
    //here: (first + last) >> 1 to "first" and
    //in determine range we can reduce it to seek forward instead of tetermining the
    //sub direction and stuff...
    if (firstCode == lastCode)
        //return (first + last) >> 1;
        return first;
	
    // Calculate the number of highest bits that are the same
    // for all objects, using the count-leading-zeros intrinsic.
    int commonPrefix = clz3(firstCode ^ lastCode);

    // Use binary search to find where the next bit differs.
    // Specifically, we are looking for the highest object that
    // shares more than commonPrefix bits with the first one.

    int split = first; // initial guess
    int step = last - first;
    do
    {
        step = (step + 1) >> 1; // exponential decrease
        int newSplit = split + step; // proposed new position

        if (newSplit < last)
        {
            uint splitCode = leafNodes[newSplit].mortonId;
            int splitPrefix = clz3(firstCode ^ splitCode);
            if (splitPrefix > commonPrefix)
                split = newSplit; // accept proposal
        }
    }
    while (step > 1);

    return split;
}
int2 determineRange(int index)
{
  //so we don't have to call it every time
    uint lso, stride;
    mergeOutputBuffer.GetDimensions(lso, stride);
  //uint lso = 1024;
    lso = lso - 1;
  //tadaah, it's the root node
    if (index == 0)
        return int2(0, lso);
  //direction to walk to, 1 to the right, -1 to the left
    int dir;
  //morton code diff on the outer known side of our range ... diff mc3 diff mc4 ->DIFF<- [mc5 diff mc6 diff ... ] diff .. 
    int d_min;
    int initialindex = index;
  
    uint minone = leafNodes[index - 1].mortonId;
    uint precis = leafNodes[index].mortonId;
    uint pluone = leafNodes[index + 1].mortonId;

  //AllMemoryBarrierWithGroupSync();
  //GroupMemoryBarrierWithGroupSync();

    if ((minone == precis && pluone == precis))
    {
    //set the mode to go towards the right, when the left and the right
    //object are being the same as this one, so groups of equal
    //code will be processed from the left to the right
    //and in node order from the top to the bottom, with each node X (ret.x = index)
    //containing Leaf object X and nodes from X+1 (the split func will make this split there)
    //till the end of the groups
    //(if any bit differs... DEP=32) it will stop the search
        while (index > 0 && index < lso)
        {
       //move one step into our direction
            index += 1;
            if (index >= lso)
            {
       //we hit the left end of our list
                break;
            }
	  
            if (leafNodes[index].mortonId != leafNodes[index + 1].mortonId)
            {
       //there is a diffrence
                break;
            }
        }
    //return the end of equal grouped codes
        return int2(initialindex, index);
    }
    else
    {
    //Our codes differ, so we seek for the ranges end in the binary search fashion:
        int2 lr = int2(clz3(precis ^ minone), clz3(precis ^ pluone));
    //now check wich one is higher (codes put side by side and wrote from up to down)
        if (lr.x > lr.y)
        { //to the left, set the search-depth to the right depth
            dir = -1;
            d_min = lr.y;
        }
        else
        { //to the right, set the search-depth to the left depth
            dir = 1;
            d_min = lr.x;
        }
    }
    //Now look for an range to search in (power of two)
    int l_max = 2;
    //so we don't have to calc it 3x
    int testindex = index + l_max * dir;
    while ((testindex <= lso && testindex >= 0) ? (clz3(precis ^ leafNodes[testindex].mortonId) > d_min) : (false))
    {
        l_max *= 2;
        testindex = index + l_max * dir;
    }
	
    int l = 0;
    //go from l_max/2 ... l_max/4 ... l_max/8 .......... 1 all the way down
    for (uint div = 2; l_max / div >= 1; div *= 2)
    {
      //calculate the ofset state
        int t = l_max / div;
	//calculate where to test next
        int newTest = index + (l + t) * dir;
	//test if in code range
        if (newTest <= lso && newTest >= 0)
        {
            int splitPrefix = clz3(precis ^ leafNodes[newTest].mortonId);
	    //and if the code is higher then our minimum, update the position
            if (splitPrefix > d_min)
                l = l + t;
        }
    }
    //now give back the range (in the right order, [lower|higher])
    if (dir == 1)
        return int2(index, index + l * dir);
    else
        return int2(index + l * dir, index);
    return int2(0, 0);
}