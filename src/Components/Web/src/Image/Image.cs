// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Image;

/* This is equivalent to a .razor file containing:
 *
 * <div class="blazor-image-container" style="@ContainerStyle">
 *     @if (_isLoading && LoadingContent != null)
 *     {
 *         @LoadingContent
 *     }
 *     else if (_hasError && ErrorContent != null)
 *     {
 *         @ErrorContent
 *     }
 *     <img id="@Id" class="@(_isLoading || _hasError ? "d-none" : GetCssClass())"
 *          alt="@Alt" @ref="Element" @attributes="AdditionalAttributes" />
 * </div>
 */
/// <summary>
/// A component that efficiently renders images from non-HTTP sources like byte arrays.
/// </summary>
public class Image : ComponentBase, IAsyncDisposable
{
    private readonly string _id = $"image-{Guid.NewGuid():N}";
    private bool _isLoading = true;
    private bool _hasError;
    private bool _isDisposed;

    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
    /// <para>
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </para>
    /// </summary>
    [DisallowNull] public ElementReference? Element { get; protected set; }

    /// <summary>
    /// Gets the injected <see cref="IJSRuntime"/>.
    /// </summary>
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the source for the image.
    /// </summary>
    [Parameter] public IImageSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the alt text for the image.
    /// </summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>
    /// Gets or sets the content to display while the image is loading.
    /// </summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// Gets or sets the content to display when an error occurs loading the image.
    /// </summary>
    [Parameter] public RenderFragment? ErrorContent { get; set; }

    /// <summary>
    /// Gets or sets the caching strategy for the image.
    /// </summary>
    [Parameter] public CacheStrategy CacheStrategy { get; set; } = CacheStrategy.Memory;

    /// <summary>
    /// Event callback invoked when the image has loaded successfully.
    /// </summary>
    [Parameter] public EventCallback<bool> OnImageLoaded { get; set; }

    /// <summary>
    /// Event callback invoked when an error occurs loading the image.
    /// </summary>
    [Parameter] public EventCallback<string> OnImageError { get; set; }

    /// <summary>
    /// Gets or sets the attributes for the image.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the size of the chunks used when sending image data.
    /// </summary>
    [Parameter] public int ChunkSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Event callback invoked to report the progress of image loading.
    /// </summary>
    [Parameter] public EventCallback<double> OnProgress { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Set default content if not provided
        LoadingContent ??= CreateDefaultLoadingContent();
        ErrorContent ??= CreateDefaultErrorContent();
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-container");

        string containerStyle = GetContainerStyle();
        if (!string.IsNullOrEmpty(containerStyle))
        {
            builder.AddAttribute(2, "style", containerStyle);
        }

        if (_isLoading && LoadingContent != null)
        {
            builder.AddContent(3, LoadingContent);
        }
        else if (_hasError && ErrorContent != null)
        {
            builder.AddContent(4, ErrorContent);
        }

        builder.OpenElement(5, "img");
        builder.AddAttribute(6, "id", _id);

        var cssClass = _isLoading || _hasError ? "d-none" : GetCssClass();
        if (!string.IsNullOrEmpty(cssClass))
        {
            builder.AddAttribute(7, "class", cssClass);
        }

        if (!string.IsNullOrEmpty(Alt))
        {
            builder.AddAttribute(8, "alt", Alt);
        }

        builder.AddMultipleAttributes(9, AdditionalAttributes);
        builder.AddElementReferenceCapture(10, inputReference => Element = inputReference);

        builder.CloseElement();
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_isDisposed)
        {
            await LoadImageIfSourceProvided();
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
    }

    private async Task LoadImageIfSourceProvided()
    {
        if (Source == null)
        {
            return;
        }
        try
        {
            SetLoadingState();

            if (Source is IStreamingImageSource streamingSource)
            {
                await StreamImageInChunks(streamingSource);
            }
            else if (Source is ILoadableImageSource loadableSource)
            {
                byte[] imageData = await loadableSource.GetBytesAsync();
                await SendImageInChunks(imageData, loadableSource.MimeType, loadableSource.CacheKey);
            }
            else
            {
                throw new InvalidOperationException(
                    "The provided image source must be either ILoadableImageSource or IStreamingImageSource.");
            }

            await SetSuccessState();
        }
        catch (Exception ex)
        {
            await SetErrorState(ex.Message);
        }
    }

    private async Task SendImageInChunks(byte[] imageData, string mimeType, string cacheKey)
    {
        try
        {
            int totalChunks = (int)Math.Ceiling((double)imageData.Length / ChunkSize);
            string transferId = $"{_id}-{Guid.NewGuid():N}";

            byte[] chunkBuffer = ArrayPool<byte>.Shared.Rent(ChunkSize);

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.initChunkedTransfer",
                _id, transferId, totalChunks, imageData.Length, mimeType, cacheKey,
                CacheStrategy.ToString().ToLowerInvariant());
            try
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    int offset = i * ChunkSize;
                    int length = Math.Min(ChunkSize, imageData.Length - offset);

                    Array.Copy(imageData, offset, chunkBuffer, 0, length);

                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.addChunk",
                        transferId, i, chunkBuffer.AsMemory(0, length).ToArray());

                    double progress = (i + 1) / (double)totalChunks;
                    await OnProgress.InvokeAsync(progress);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(chunkBuffer);
            }

        await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer",
                transferId, _id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send chunked image data: {ex.Message}", ex);
        }
    }

    private async Task StreamImageInChunks(IStreamingImageSource source)
    {
        try
        {
            long totalSize = await source.GetSizeAsync();
            int totalChunks = (int)Math.Ceiling((double)totalSize / ChunkSize);
            string transferId = $"{_id}-{Guid.NewGuid():N}";

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.initChunkedTransfer",
                _id, transferId, totalChunks, totalSize, source.MimeType, source.CacheKey,
                CacheStrategy.ToString().ToLowerInvariant());

            byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
            try
            {
                using Stream stream = await source.OpenReadStreamAsync();
                int chunkIndex = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ChunkSize))) > 0)
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.addChunk",
                        transferId, chunkIndex, buffer.AsMemory(0, bytesRead).ToArray());

                    double progress = (chunkIndex + 1) / (double)totalChunks;
                    await OnProgress.InvokeAsync(progress);

                    chunkIndex++;
                }

                await JSRuntime.InvokeVoidAsync(
                    "Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer",
                    transferId, _id);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to stream image data: {ex.Message}", ex);
        }
    }

    private void SetLoadingState()
    {
        _isLoading = true;
        _hasError = false;
        StateHasChanged();
    }

    private async Task SetSuccessState()
    {
        _isLoading = false;
        _hasError = false;
        StateHasChanged();
        await OnImageLoaded.InvokeAsync(true);
    }

    private async Task SetErrorState(string error)
    {
        _isLoading = false;
        _hasError = true;
        StateHasChanged();
        await OnImageError.InvokeAsync(error);
    }

    private static RenderFragment CreateDefaultLoadingContent() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-loading");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "loading-spinner");
        builder.CloseElement();

        builder.CloseElement();
    };

    private static RenderFragment CreateDefaultErrorContent() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-error");

        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "class", "error-icon");
        builder.AddContent(4, "⚠️");
        builder.CloseElement();

        builder.OpenElement(5, "span");
        builder.AddAttribute(6, "class", "error-message");
        builder.AddContent(7, "Failed to load image");
        builder.CloseElement();

        builder.CloseElement();
    };

    private string GetContainerStyle() => AdditionalAttributes?.TryGetValue("style", out var style) == true
        ? style?.ToString() ?? string.Empty : string.Empty;

    private string GetCssClass() => AdditionalAttributes?.TryGetValue("class", out var cssClass) == true
        ? cssClass?.ToString() ?? string.Empty : string.Empty;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (Source != null)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.revokeImageUrl",
                        _id);
                }
                catch (JSDisconnectedException)
                {
                    // Client disconnected
                }
            }
        }
    }
}
