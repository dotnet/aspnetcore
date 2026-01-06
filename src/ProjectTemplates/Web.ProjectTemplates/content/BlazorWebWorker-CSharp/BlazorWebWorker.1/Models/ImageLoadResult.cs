// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorWebWorker._1.Models;

/// <summary>
/// Result of loading an image onto a canvas.
/// </summary>
public class ImageLoadResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
