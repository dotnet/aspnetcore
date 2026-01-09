// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorWebCSharp._1.Client.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorWebCSharp._1.Client.Services;

/// <summary>
/// Service for canvas-related operations via JavaScript interop.
/// </summary>
public interface ICanvasService
{
    /// <summary>
    /// Fetches an image from a URL and returns it as a byte array.
    /// </summary>
    Task<byte[]?> FetchImageAsBytesAsync(string url);

    /// <summary>
    /// Loads an image onto a canvas and stores pixel data in JS memory.
    /// </summary>
    Task<ImageLoadResult> LoadImageToCanvasAsync(ElementReference canvas, byte[] imageData, string contentType);

    /// <summary>
    /// Gets the stored pixel data from JS memory.
    /// </summary>
    Task<byte[]> GetStoredPixelsAsync();

    /// <summary>
    /// Draws processed pixel data to a canvas.
    /// </summary>
    Task DrawPixelsToCanvasAsync(ElementReference canvas, byte[] pixels, int width, int height);
}
