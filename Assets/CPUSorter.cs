using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPUSorter : MonoBehaviour {
    const int count = 2;
    int[] array = new int[count];
	// Use this for initialization
	void Start () {
        for (int i = 0; i < count; i++)
        {
            array[i] = (int)Random.Range(0, 512);
        }
        Print("Un Sorted", array);

        //Print("Sorted", InsertionSort(array));
        MergeSort(array, 0, array.Length-1);

        Print("Sorted",array);


    }

    void MergeSort(int[] input, int left, int right)
    {
        if (left < right)
        {
            int middle = (left + right) / 2;
            MergeSort(input, left, middle);
            MergeSort(input, middle + 1, right);
            Merge(input, left, middle, right);
        }
    }

    int[] RecursiveSort(int[] input, int p, int r)
    {
        if(p < r)
        {
            int q = (p + r) / 2;
            RecursiveSort(input, p, Mathf.CeilToInt(q));
            RecursiveSort(input, Mathf.FloorToInt(q+1), r);
            Merge(input, p, q, r);
        }
        return input;
    }


    private static void Merge(int[] input, int low, int middle, int high)
    {

        int left = low;
        int right = middle + 1;
        int[] tmp = new int[(high - low) + 1];
        int tmpIndex = 0;

        while ((left <= middle) && (right <= high))
        {
            if (input[left] < input[right])
            {
                tmp[tmpIndex] = input[left];
                left = left + 1;
            }
            else
            {
                tmp[tmpIndex] = input[right];
                right = right + 1;
            }
            tmpIndex = tmpIndex + 1;
        }

        if (left <= middle)
        {
            while (left <= middle)
            {
                tmp[tmpIndex] = input[left];
                left = left + 1;
                tmpIndex = tmpIndex + 1;
            }
        }

        if (right <= high)
        {
            while (right <= high)
            {
                tmp[tmpIndex] = input[right];
                right = right + 1;
                tmpIndex = tmpIndex + 1;
            }
        }

        for (int i = 0; i < tmp.Length; i++)
        {
            input[low + i] = tmp[i];
        }

    }

    void Merge2(int[] A, int left, int middle, int right)
    {
        int n1 = middle - left + 1;
        int n2 = right - middle;
        //Print("Input", A);
        //Debug.Log("Length: " + A[0]);
        //Debug.Log("Length: " + n2);

        int[] L = new int[n1 + 1];
        int[] R = new int[n2 + 1];
        //L[0] = A[0];
        //R[0] = A[1];
        for (int y = 1; y < n1; y++)
        {
            int key = A[left + y - 1];
            //print(left + y - 1);
            L[y] = key;
        }
        for (int x = 1; x < n2; x++)
        {
            int key = A[middle + x];
            //print(middle + x);
            R[x] = key;
        }
        Print("Input", A);
        //Print("Sub Array", L);
        //Print("Sub Array", R);
        L[n1] = 50000;
        R[n2] = 50000;
        int i = 0, j = 0;
        for (int k = left; k < right; k++)
        {
            if(L[i] < 50000 && L[i] <= R[j]){
                A[k] = L[i];
                if(i < n1-1)
                i++;
            }
            else if(R[j] < 50000)
            {
                A[k] = R[j];
                if (j < n2-1)
                    j++;
            }
        }
    }
    int[] InsertionSort(int[] input) {
        int[] tempArray = new int[count];
        input.CopyTo(tempArray, 0);
        Debug.Log("Copied");
        for (int j = 1; j < count; j++)
        {
            int key = input[j];
            int i = j - 1;
            while(i > 0 && input[i] > key)
            {
                input[i + 1] = input[i];
                i = i - 1;
            }
            input[i + 1] = key;
        }

        return tempArray;
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
    // Update is called once per frame
    void Update () {
		
	}
}
