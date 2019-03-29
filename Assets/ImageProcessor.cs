using System;
using UnityEngine;

/// <summary>
/// Copies Image bytes into byte array
/// </summary>
public class ImageProcessor
{
    private static byte[] s_ImageBuffer = new byte[0];
    private static int s_ImageBufferSize = 0;


    public static bool ProcessImage(byte[] outputImage, IntPtr inputImage, int width, int height, int rowStride)
    {
        if (outputImage.Length < width * height)
        {
            Debug.Log("Input buffer is too small!");
            return false;
        }

        int bufferSize = rowStride * height;
        if (bufferSize != s_ImageBufferSize || s_ImageBuffer.Length == 0)
        {
            s_ImageBufferSize = bufferSize;
            s_ImageBuffer = new byte[bufferSize];
        }

        // Move raw data into managed buffer.
        System.Runtime.InteropServices.Marshal.Copy(inputImage, s_ImageBuffer, 0, bufferSize);

        for (int j = 1; j < height - 1; j++)
        {
            for (int i = 1; i < width - 1; i++)
            {
                int offset = (j * rowStride) + i;
                outputImage[(j * width) + i] = s_ImageBuffer[offset];
            }
        }

        return true;
    }

}

