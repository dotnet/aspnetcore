// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private IImageSource? _lastSource;

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
        // Open container div
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-container");

        string containerStyle = GetContainerStyle();
        if (!string.IsNullOrEmpty(containerStyle))
        {
            builder.AddAttribute(2, "style", containerStyle);
        }

        // Add loading content if needed
        if (_isLoading && LoadingContent != null)
        {
            builder.AddContent(3, LoadingContent);
        }
        else if (_hasError && ErrorContent != null)
        {
            builder.AddContent(4, ErrorContent);
        }

        // Add the image element
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
        builder.CloseElement(); // close img

        builder.CloseElement(); // close div
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
        var previousSource = _lastSource;
        await base.SetParametersAsync(parameters);

        // Reload if source changed
        if (Source?.CacheKey != previousSource?.CacheKey && Source != null && !_isDisposed)
        {
            //await LoadImageIfSourceProvided();
        }

        _lastSource = Source;
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

            byte[] imageData = await Source.GetBytesAsync();

            //await JSRuntime.InvokeVoidAsync(
            //    "Blazor._internal.BinaryImageComponent.createImageFromBytes",
            //    _id, imageData, Source.MimeType, Source.CacheKey,
            //    CacheStrategy.ToString().ToLowerInvariant());

            await SendImageInChunks(imageData, Source.MimeType, Source.CacheKey);

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

            await JSRuntime.InvokeVoidAsync(
                "imageComponent.initChunkedTransfer",
                _id, transferId, totalChunks, imageData.Length, mimeType, cacheKey,
                CacheStrategy.ToString().ToLowerInvariant());

            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * ChunkSize;
                int length = Math.Min(ChunkSize, imageData.Length - offset);
                byte[] chunk = new byte[length];
                Array.Copy(imageData, offset, chunk, 0, length);

                await JSRuntime.InvokeVoidAsync(
                    "imageComponent.addChunk",
                    transferId, i, chunk);

                double progress = (i + 1) / (double)totalChunks;
                await OnProgress.InvokeAsync(progress);
            }

            await JSRuntime.InvokeVoidAsync(
                "imageComponent.finalizeChunkedTransfer",
                transferId, _id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send chunked image data: {ex.Message}", ex);
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
                    // Client disconnected, ignore
                }
            }
        }
    }
}
