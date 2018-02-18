using UnityEngine;
using System.Collections;

public class SortTest : MonoBehaviour
{
    private ComputeBuffer inBuffer, tempBuffer;

    void Start()
    {
        uint[] inArray = new uint[512 * 4];

        for (int i = 0; i < inArray.Length; i++)
            inArray[i] = (uint) (inArray.Length - 1 - i);

        Print("Unsorted", inArray);
        
        inBuffer = new ComputeBuffer(inArray.Length, 4);
        tempBuffer = new ComputeBuffer(inArray.Length, 4);

        inBuffer.SetData(inArray);
        GpuSort.BitonicSort32(inBuffer, tempBuffer);
        inBuffer.GetData(inArray);

        Print("Sorted", inArray);
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

    void OnDisable()
    {
        if (inBuffer != null)
            inBuffer.Dispose();
        if (tempBuffer != null)
            tempBuffer.Dispose();

        inBuffer = null;
        tempBuffer = null;
    }
}
