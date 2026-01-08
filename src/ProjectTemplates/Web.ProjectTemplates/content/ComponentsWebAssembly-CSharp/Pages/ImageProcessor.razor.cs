// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using ComponentsWebAssembly_CSharp.Models;
using ComponentsWebAssembly_CSharp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ComponentsWebAssembly_CSharp.WorkerClient;

namespace ComponentsWebAssembly_CSharp.Pages;

/// <summary>
/// Image grayscale converter page.
/// Allows users to upload images and apply grayscale effect with adjustable intensity.
/// </summary>
[SupportedOSPlatform("browser")]
public partial class ImageProcessor
{
    private const string DefaultImagePath = "icon-192.png";
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    [Inject]
    private ICanvasService CanvasService { get; set; } = default!;

    private ElementReference _originalCanvasRef;
    private ElementReference _processedCanvasRef;
    private bool _hasImage;
    private double _intensity = 0.5;
    private string _errorMessage = string.Empty;
    private int _imageWidth;
    private int _imageHeight;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeWorkerAndLoadDefaultImageAsync();
        }
    }

    private async Task InitializeWorkerAndLoadDefaultImageAsync()
    {
        await Worker.InitializeAsync();
        await Worker.WaitForReadyAsync();
        await LoadDefaultImageAsync();
    }

    private async Task LoadDefaultImageAsync()
    {
        try
        {
            var imageBytes = await CanvasService.FetchImageAsBytesAsync(DefaultImagePath);
            if (imageBytes is { Length: > 0 })
            {
                await LoadImageAsync(imageBytes, "image/png");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load default image: {ex.Message}");
        }
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        try
        {
            _errorMessage = string.Empty;
            var file = e.File;

            if (file == null)
                return;

            var imageData = await ReadFileAsync(file);
            await LoadImageAsync(imageData, file.ContentType);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            _hasImage = false;
            StateHasChanged();
        }
    }

    private static async Task<byte[]> ReadFileAsync(IBrowserFile file)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: MaxFileSizeBytes);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    private async Task LoadImageAsync(byte[] imageData, string contentType)
    {
        _hasImage = true;
        StateHasChanged();
        await Task.Yield();

        var result = await CanvasService.LoadImageToCanvasAsync(_originalCanvasRef, imageData, contentType);

        if (!result.Success)
        {
            _errorMessage = result.Error ?? "Failed to load image";
            _hasImage = false;
            StateHasChanged();
            return;
        }

        _imageWidth = result.Width;
        _imageHeight = result.Height;
        await ApplyGrayscaleAsync();
    }

    private async Task OnIntensityChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int value))
        {
            _intensity = value / 100.0;
            await ApplyGrayscaleAsync();
        }
    }

    private async Task ApplyGrayscaleAsync()
    {
        if (!_hasImage)
            return;

        try
        {
            _errorMessage = string.Empty;

            var originalPixels = await CanvasService.GetStoredPixelsAsync();

            var processedPixels = await Worker.InvokeAsync(
                "ComponentsWebAssembly_CSharp.Worker.GrayscaleWorker.ApplyGrayscale",
                originalPixels,
                _intensity);

            await CanvasService.DrawPixelsToCanvasAsync(_processedCanvasRef, processedPixels, _imageWidth, _imageHeight);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }
}
