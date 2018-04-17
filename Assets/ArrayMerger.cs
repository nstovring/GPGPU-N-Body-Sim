using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrayMerger : MonoBehaviour {

    // Use this for initialization
    List<int> arrays = new List<int>();
    int[] array;
    int[] sortedArray;
    const int count = 2048;
	void Start () {
        sortedArray = new int[count];
        int step = 8;
        //8 Subarrays size 256 = 2048 points
        for (int i = 0; i < step; i++)
        {
            for (int j = i; j < count; j += step)
            {
                arrays.Add(j + 1);
            }
        }
        

        array = arrays.ToArray();

        Printer.Print("Created List",arrays.ToArray(), false);

        for (int i = 0; i < arrays.Count; i++)
        {
            int index = BinarySearch(i, array[i]);
            sortedArray[index] = array[i];
        }

        Printer.Print("Merged List", sortedArray, false);
    }
    int BinarySearch(int idx, int mortonCode)
    {
        
        //int count = 1024;
        int arraySize = 256;
        int normalizedIndex = idx % arraySize;

        int groupAmount = count / arraySize;
        int rank = normalizedIndex;

        for (int i = 1; i < groupAmount; i++)
        {
            int direction = 0;
            int center = ((idx + arraySize * i) % count) - normalizedIndex + arraySize / 2;
            int step = center - 1;
            float stepScale = 0.25f;
            int maxLoop = 0;
            do
            {
                step += direction;

                int middle = array[step];
                int pluOne = array[step + 1];
                int normalizedStep = step % arraySize;

                if ((arraySize) * stepScale < 1 && middle < mortonCode && pluOne < mortonCode)
                {
                    rank += normalizedStep + 2;
                    break;
                }

                if (mortonCode < middle)
                {
                    direction = (int) (-1 * (arraySize) * stepScale);
                    stepScale *= 0.5f;
                }
                else if (pluOne < mortonCode)
                {
                    direction = (int)(1 * (arraySize) * stepScale);
                    stepScale *= 0.5f;
                }
                else if (middle < mortonCode && pluOne > mortonCode)
                {
                    rank += ((normalizedStep) + 1) % count;
                    break;
                }
                maxLoop++;
            } while ((int)(arraySize) * stepScale > 0 && maxLoop < 64);

        }
        return rank;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
