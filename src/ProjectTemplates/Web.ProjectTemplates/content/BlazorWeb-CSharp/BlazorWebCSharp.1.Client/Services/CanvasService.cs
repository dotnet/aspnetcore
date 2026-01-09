// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BlazorWebCSharp._1.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorWebCSharp._1.Client.Services;

/// <summary>
/// Implementation of canvas operations via JavaScript interop.
/// </summary>
public class CanvasService : ICanvasService
{
    private readonly IJSRuntime _jsRuntime;

    public CanvasService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<byte[]?> FetchImageAsBytesAsync(string url)
    {
        return await _jsRuntime.InvokeAsync<byte[]?>("fetchImageAsBytes", url);
    }

    /// <inheritdoc />
    public async Task<ImageLoadResult> LoadImageToCanvasAsync(ElementReference canvas, byte[] imageData, string contentType)
    {
        var base64Data = Convert.ToBase64String(imageData);
        return await _jsRuntime.InvokeAsync<ImageLoadResult>("loadImageToCanvas", canvas, base64Data, contentType);
    }

    /// <inheritdoc />
    public async Task<byte[]> GetStoredPixelsAsync()
    {
        return await _jsRuntime.InvokeAsync<byte[]>("getStoredPixels");
    }

    /// <inheritdoc />
    public async Task DrawPixelsToCanvasAsync(ElementReference canvas, byte[] pixels, int width, int height)
    {
        await _jsRuntime.InvokeVoidAsync("drawPixelsToCanvas", canvas, pixels, width, height);
    }
}
