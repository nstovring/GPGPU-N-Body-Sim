//--------------------------------------------------------------------------------------
// Imports
//--------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

//--------------------------------------------------------------------------------------
// Classes
//--------------------------------------------------------------------------------------
   
static public class GpuSort
{
    // ---- Constants ----

    private const uint BITONIC_BLOCK_SIZE = 512;
    private const uint TRANSPOSE_BLOCK_SIZE = 512;
    static public int count = 512;

    // ---- Members ----

    static private ComputeShader sort32;
    static private ComputeShader sort64;
    static private ComputeShader sort32Particle;

    static private int kSort32;
    static private int kSort64;
    static private int kSort32Particle;
    static private int kTranspose32;
    static private int kTranspose64;
    static private int kTranspose32Particle;
    static private bool init;

    // ---- Structures ----


    // ---- Methods ----

    static private void Init()
    {
        // Acquire compute shaders.
        sort32 = (ComputeShader) Resources.Load("GpuSort/GpuSort32", typeof(ComputeShader));
        sort64 = (ComputeShader) Resources.Load("GpuSort/GpuSort64", typeof(ComputeShader));
        sort32Particle = (ComputeShader) Resources.Load("GpuSort/GpuSort32Particle", typeof(ComputeShader));
        // If they were not found, crash!
        if (sort32 == null) Debug.LogError("GpuSort32 not found.");
        if (sort64 == null) Debug.LogError("GpuSort64 not found.");
        if (sort32Particle == null) Debug.LogError("GpuSort32Particle not found.");

        // Find kernels
        kSort32 = sort32.FindKernel("BitonicSort");
        kSort64 = sort64.FindKernel("BitonicSort");
        kSort32Particle = sort32Particle.FindKernel("RadixSort");
        kTranspose32 = sort32.FindKernel("MatrixTranspose");
        kTranspose64 = sort64.FindKernel("MatrixTranspose");
        kTranspose32Particle = sort32Particle.FindKernel("MatrixTranspose");

        // Done
        init = true;
    }

    static public void BitonicSort32(ComputeBuffer inBuffer, ComputeBuffer tmpBuffer)
    {
        if (!init) Init();
        BitonicSortGeneric(sort32, kSort32, kTranspose32, inBuffer, tmpBuffer);   
    }
    static public void BitonicSortParticle32(ComputeBuffer inBuffer, ComputeBuffer tmpBuffer)
    {
        if (!init) Init();
        BitonicSortGeneric(sort32Particle, kSort32Particle, kTranspose32Particle, inBuffer, tmpBuffer);
    }

    static public void BitonicSort64(ComputeBuffer inBuffer, ComputeBuffer tmpBuffer)
    {
        if (!init) Init();
        BitonicSortGeneric(sort64, kSort64, kTranspose64, inBuffer, tmpBuffer);
    }

    static private void BitonicSortGeneric(ComputeShader shader, int kSort, int kTranspose, ComputeBuffer inBuffer, ComputeBuffer tmpBuffer)
    {
        // Determine if valid.
        if ((inBuffer.count % BITONIC_BLOCK_SIZE) != 0)
            Debug.LogError("Input array size should be multiple of the Bitonic block size!");

        // Determine parameters.
        int NUM_ELEMENTS =  inBuffer.count;
        //uint MATRIX_WIDTH = BITONIC_BLOCK_SIZE;
        //uint MATRIX_HEIGHT = 0;// NUM_ELEMENTS / BITONIC_BLOCK_SIZE;
        shader.SetInt("count", NUM_ELEMENTS);
        shader.SetBuffer(kSort, "Data", inBuffer);
        shader.Dispatch(kSort, (int)((NUM_ELEMENTS / BITONIC_BLOCK_SIZE)),1, 1);
        return;

        //// Sort the data
        //// First sort the rows for the levels <= to the block size
        //for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        //{
        //    SetConstants(shader, level, level, MATRIX_HEIGHT, MATRIX_WIDTH);
        //    
        //    // Sort the row data
        //    shader.SetBuffer(kSort, "Data", inBuffer);
        //    shader.Dispatch(kSort, (int) (NUM_ELEMENTS / BITONIC_BLOCK_SIZE), (int)(NUM_ELEMENTS / BITONIC_BLOCK_SIZE), 1);
        //}
        //
        //// Then sort the rows and columns for the levels > than the block size
        //// Transpose. Sort the Columns. Transpose. Sort the Rows.
        //for (uint level = (BITONIC_BLOCK_SIZE << 1); level <= NUM_ELEMENTS; level <<= 1)
        //{
        //    // Transpose the data from buffer 1 into buffer 2
        //    //SetConstants(shader, (level / BITONIC_BLOCK_SIZE), (level & ~NUM_ELEMENTS) / BITONIC_BLOCK_SIZE, MATRIX_WIDTH, MATRIX_HEIGHT);
        //    //shader.SetBuffer(kTranspose, "Input", inBuffer);
        //    //shader.SetBuffer(kTranspose, "Data", tmpBuffer);
        //    //shader.Dispatch(kTranspose, (int) (MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE), (int) (MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE), 1);
        //
        //    shader.SetBuffer(kSort, "Data", tmpBuffer);
        //    shader.Dispatch(kSort, (int) (NUM_ELEMENTS / BITONIC_BLOCK_SIZE), 1, 1);
        //
        //    //// Transpose the data from buffer 2 back into buffer 1
        //    //SetConstants(shader, BITONIC_BLOCK_SIZE, level, MATRIX_HEIGHT, MATRIX_WIDTH);
        //    //shader.SetBuffer(kTranspose, "Input", tmpBuffer);
        //    //shader.SetBuffer(kTranspose, "Data", inBuffer);
        //    //shader.Dispatch(kTranspose, (int) (MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE), (int) (MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE), 1);
        //
        //    shader.SetBuffer(kSort, "Data", inBuffer);
        //    shader.Dispatch(kSort, (int) ((NUM_ELEMENTS / TRANSPOSE_BLOCK_SIZE)), (int)((NUM_ELEMENTS / TRANSPOSE_BLOCK_SIZE)), 1);
        //}
    }

    static private void SetConstants(ComputeShader shader, uint iLevel, uint iLevelMask, uint iWidth, uint iHeight)
    {
        shader.SetInt("g_iLevel", (int) iLevel);
        shader.SetInt("g_iLevelMask", (int) iLevelMask);
        shader.SetInt("g_iWidth", (int) iWidth);
        shader.SetInt("g_iHeight", (int) iHeight);
    }
}
