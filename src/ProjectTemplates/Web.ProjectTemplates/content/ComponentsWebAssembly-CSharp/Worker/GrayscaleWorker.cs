// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace ComponentsWebAssembly_CSharp.Worker;

/// <summary>
/// Image processing worker that runs in a WebWorker.
/// Applies grayscale effect to images with adjustable intensity.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class GrayscaleWorker
{
    /// <summary>
    /// Applies grayscale effect to an image with adjustable intensity.
    /// This method is called from JavaScript in the worker thread.
    /// </summary>
    /// <param name="imageBytes">Raw image bytes (assumes RGBA format, 4 bytes per pixel)</param>
    /// <param name="intensity">Grayscale intensity from 0.0 (original) to 1.0 (full grayscale)</param>
    /// <returns>Processed image bytes in RGBA format</returns>
    [JSExport]
    internal static byte[] ApplyGrayscale(byte[] imageBytes, double intensity)
    {
        // Clamp intensity to valid range
        intensity = Math.Clamp(intensity, 0.0, 1.0);
        
        // Process RGBA pixels (4 bytes per pixel)
        byte[] result = new byte[imageBytes.Length];
        
        for (int i = 0; i < imageBytes.Length; i += 4)
        {
            byte r = imageBytes[i];
            byte g = imageBytes[i + 1];
            byte b = imageBytes[i + 2];
            byte a = imageBytes[i + 3];
            
            // Calculate grayscale using luminance formula (human eye perception)
            // ITU-R BT.709: Y = 0.2126*R + 0.7152*G + 0.0722*B
            byte gray = (byte)(0.2126 * r + 0.7152 * g + 0.0722 * b);
            
            // Blend between original color and grayscale based on intensity
            result[i] = (byte)(r + (gray - r) * intensity);     // R
            result[i + 1] = (byte)(g + (gray - g) * intensity); // G
            result[i + 2] = (byte)(b + (gray - b) * intensity); // B
            result[i + 3] = a;                                   // A (unchanged)
        }
        
        return result;
    }
}
