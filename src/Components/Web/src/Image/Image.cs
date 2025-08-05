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
    [Parameter] public ImageSource? Source { get; set; }

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
    /// Gets or sets the attributes for the image.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the size of the chunks used when sending image data.
    /// </summary>
    [Parameter] public int ChunkSize { get; set; } = 64 * 1024;

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
        builder.AddAttribute(1, "id", $"{_id}-container");
        builder.AddAttribute(2, "class", "blazor-image-container");

        string containerStyle = GetContainerStyle();
        if (!string.IsNullOrEmpty(containerStyle))
        {
            builder.AddAttribute(3, "style", containerStyle);
        }

        if (_isLoading && LoadingContent != null)
        {
            builder.AddContent(4, LoadingContent);
        }
        else if (_hasError && ErrorContent != null)
        {
            builder.AddContent(5, ErrorContent);
        }

        builder.OpenElement(6, "img");
        builder.AddAttribute(7, "id", _id);

        var cssClass = _isLoading || _hasError ? "d-none" : GetCssClass();
        if (!string.IsNullOrEmpty(cssClass))
        {
            builder.AddAttribute(8, "class", cssClass);
        }

        if (!string.IsNullOrEmpty(Alt))
        {
            builder.AddAttribute(9, "alt", Alt);
        }

        builder.AddMultipleAttributes(10, AdditionalAttributes);
        builder.AddElementReferenceCapture(11, inputReference => Element = inputReference);

        builder.CloseElement();
        builder.CloseElement();
    }

    /// <inheritdoc/>
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

            // Check if image is already cached before transferring data
            if (!string.IsNullOrEmpty(Source.CacheKey))
            {
                bool foundInCache = await JSRuntime.InvokeAsync<bool>(
                    "Blazor._internal.BinaryImageComponent.trySetFromCache",
                    _id, Source.CacheKey);

                if (foundInCache)
                {
                    SetSuccessState();
                    return;
                }
            }

            // Stream the image data in chunks
            await StreamImageInChunks(Source);
            SetSuccessState();
        }
        catch (Exception)
        {
            SetErrorState();
        }
    }

    private async Task StreamImageInChunks(ImageSource source)
    {
        try
        {
            string transferId = $"{_id}-{Guid.NewGuid():N}";

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.initChunkedTransfer",
                _id, transferId, source.MimeType, source.CacheKey,
                CacheStrategy.ToString().ToLowerInvariant(), source.Length);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
            try
            {
                using Stream stream = source.Stream;
                int chunkIndex = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ChunkSize))) > 0)
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.addChunk",
                        transferId, chunkIndex, buffer.AsMemory(0, bytesRead).ToArray());

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

    private void SetSuccessState()
    {
        _isLoading = false;
        _hasError = false;
        StateHasChanged();
    }

    private void SetErrorState()
    {
        _isLoading = false;
        _hasError = true;
        StateHasChanged();
    }

    private static RenderFragment CreateDefaultLoadingContent() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-loading");
        builder.AddAttribute(2, "style", "display: flex; justify-content: center; align-items: center; padding: 20px; flex-direction: column;");

        builder.OpenElement(3, "svg");
        builder.AddAttribute(4, "class", "blazor-image-progress");
        builder.AddAttribute(5, "viewBox", "0 0 100 100");
        builder.AddAttribute(6, "style", "width: 40px; height: 40px; margin-bottom: 8px;");

        builder.OpenElement(7, "circle");
        builder.AddAttribute(8, "cx", "50");
        builder.AddAttribute(9, "cy", "50");
        builder.AddAttribute(10, "r", "40");
        builder.AddAttribute(11, "style", "fill: none; stroke: #f3f3f3; stroke-width: 8;");
        builder.CloseElement();

        builder.OpenElement(12, "circle");
        builder.AddAttribute(13, "cx", "50");
        builder.AddAttribute(14, "cy", "50");
        builder.AddAttribute(15, "r", "40");
        builder.AddAttribute(16, "style", "fill: none; stroke: #3498db; stroke-width: 8; stroke-linecap: round; transform: rotate(-90deg); transform-origin: 50px 50px; transition: stroke-dasharray 0.3s ease-in-out;");
        builder.CloseElement();

        builder.CloseElement();

        builder.OpenElement(20, "style");
        builder.AddContent(21, @"
            .blazor-image-progress circle:last-child {
                stroke-dasharray: calc(3.141 * var(--blazor-image-progress, 0) * 100% * 0.8), 500%;
            }
        ");
        builder.CloseElement();

        builder.CloseElement();
    };

    // private static RenderFragment CreateDefaultLoadingContent() => builder =>
    // {
    //     builder.OpenElement(0, "div");
    //     builder.AddAttribute(1, "class", "blazor-image-loading");
    //     builder.CloseElement();
    // };

    private static RenderFragment CreateDefaultErrorContent() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "blazor-image-error");
        builder.AddAttribute(2, "style", "display: flex; justify-content: center; align-items: center; padding: 20px; flex-direction: column; text-align: center;");

        builder.OpenElement(3, "span");
        builder.AddAttribute(4, "class", "error-icon");
        builder.AddAttribute(5, "style", "font-size: 24px; margin-bottom: 8px;");
        builder.AddContent(6, "⚠️");
        builder.CloseElement();

        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "class", "error-message");
        builder.AddAttribute(9, "style", "color: #dc3545; font-size: 14px;");
        builder.AddContent(10, "Failed to load image");
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

            if (Source != null && RendererInfo.IsInteractive == true)
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
