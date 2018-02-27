using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterExample : MonoBehaviour {

    public ComputeShader radixSorterShader;
    public ComputeBuffer pointBuffer;
    public ComputeBuffer mortonBuffer, tempBuffer;
    RenderTexture countTex; 
    RenderTexture offsetTex;
    const int count = 8192;
	// Use this for initialization

  
	void Start () {
        //Vector3[] points = GenerateValues();
        //RadixSort(points);
        Initialize();
    }

    void Initialize()
    {
        Vector3[] points = GenerateValues();

        pointBuffer = new ComputeBuffer(count, sizeof(float) * 3, ComputeBufferType.Default);
        mortonBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Default);

        countTex = new RenderTexture(8, 16, 0);
        offsetTex = new RenderTexture(8, 16, 0);

        pointBuffer.SetData(points);

        int MainKernel = radixSorterShader.FindKernel("CSMain");
        radixSorterShader.SetBuffer(MainKernel, "pointBuffer", pointBuffer);
        radixSorterShader.SetBuffer(MainKernel, "mortonIds", mortonBuffer);
        radixSorterShader.Dispatch(MainKernel, count / 32, 1, 1);


        Vector3[] sortedData = new Vector3[count];
        pointBuffer.GetData(sortedData);
        uint[] mortonData = new uint[count];
        mortonBuffer.GetData(mortonData);



        //uint[] inArray = new uint[count];

        //for (int i = 0; i < inArray.Length; i++)
        //    inArray[i] = (uint)(inArray.Length - 1 - i);

        Print("Unsorted", mortonData);

        mortonBuffer = new ComputeBuffer(mortonData.Length, sizeof(uint));
        tempBuffer = new ComputeBuffer(mortonData.Length, sizeof(uint));

        mortonBuffer.SetData(mortonData);
        //GpuSort.BitonicSort32(mortonBuffer, tempBuffer);
        mortonBuffer.GetData(mortonData);

        Print("Sorted", mortonData);


        //for (int i = 0; i < 20; i++)
        //{
        //    Debug.Log(mortonData[i].ToString());
        //}
    }

    void Bitonic()
    {
       

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
        x = Mathf.Min(Mathf.Max(x * 1024.0f, 0.0f), 1023.0f);
        y = Mathf.Min(Mathf.Max(y * 1024.0f, 0.0f), 1023.0f);
        z = Mathf.Min(Mathf.Max(z * 1024.0f, 0.0f), 1023.0f);
        uint xx = expandBits((uint)x);
        uint yy = expandBits((uint)y);
        uint zz = expandBits((uint)z);
        return xx * 4 + yy * 2 + zz;
    }

    struct mortonStruct {
        public uint mortonCode;
        public int sectionIndex;
        public int subsectionIndex;
    }

    Vector3[] RadixSort(Vector3[] points)
    {
        mortonStruct[] morton = new mortonStruct[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 point = points[i];
            mortonStruct mS = new mortonStruct();
            mS.mortonCode = morton3D(point.x, point.y, point.z);
            morton[i] = mS;
        }
       
        for (int i =1; i < 100; i+=5)
        {
            var binaryString = System.Convert.ToString(morton3D(1.0f / i, 1.0f / i, 1.0f / i), 2);
            Debug.Log(binaryString);
        }
        //First split the input into 8 sections
        int sections = 8;
        int chunk = 0;

       
        List<int> offsetTable = new List<int>();

        Vector3[] sortedPoints = new Vector3[count];
        return null;
    }
	
	// Update is called once per frame
	void Update () {

    }

    int GetBit(uint input, int n)
    {
        int bit0N = (int)input & (1 << n);
        return bit0N;
    }

    void OnDestroy()
    {
        if(pointBuffer != null)
        pointBuffer.Release();
        mortonBuffer.Release();
        countTex.Release();
        offsetTex.Release();
    }

    Vector3[] GenerateValues()
    {
        Random.seed = 0;
        float size = 1.0f;
        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            points[i] = new Vector3(Random.Range(0, size), Random.Range(0, size), Random.Range(0, size));
        }
        return points;
    }

}
