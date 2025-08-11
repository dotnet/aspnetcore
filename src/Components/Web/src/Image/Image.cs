// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Image;

/// <summary>
/// A component that efficiently renders images from non-HTTP sources like byte arrays.
/// </summary>
public class Image : IComponent, IHandleAfterRender, IAsyncDisposable
{
    private RenderHandle _renderHandle;
    private bool _isLoading = true;
    private bool _hasError;
    private bool _isDisposed;
    private int _loadVersion;
    private bool _initialized;
    private bool _hasPendingRender;
    private bool _firstRender = true;

    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
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
    public void Attach(RenderHandle renderHandle)
    {
        if (_renderHandle.IsInitialized)
        {
            throw new InvalidOperationException("Component is already attached to a render handle.");
        }
        _renderHandle = renderHandle;
    }

    /// <inheritdoc />
    public async Task SetParametersAsync(ParameterView parameters)
    {
        var previousSource = Source;

        // Set component parameters
        parameters.SetParameterProperties(this);

        // Initialize on first parameters set
        if (!_initialized)
        {
            Render();
            _initialized = true;
        }

        // Handle parameter changes
        if (!_firstRender && previousSource?.CacheKey != Source?.CacheKey && Source != null && !_isDisposed)
        {
            var version = ++_loadVersion;

            await LoadImageIfSourceProvided(version, Source);
        }
    }

    /// <inheritdoc />
    public async Task OnAfterRenderAsync()
    {
        if (_firstRender && Source != null && !_isDisposed)
        {
            _firstRender = false;
            var version = ++_loadVersion;
            await LoadImageIfSourceProvided(version, Source);
        }
    }

    /// <summary>
    /// Queues a render of the component.
    /// </summary>
    protected void Render()
    {
        if (!_hasPendingRender && _renderHandle.IsInitialized)
        {
            _hasPendingRender = true;
            _renderHandle.Render(BuildRenderTree);
            _hasPendingRender = false;
        }
    }

    /// <summary>
    /// Builds the render tree for the component.
    /// </summary>
    protected virtual void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "img");

        if (_isLoading)
        {
            builder.AddAttribute(1, "data-state", "loading");
        }
        else if (_hasError)
        {
            builder.AddAttribute(1, "data-state", "error");
        }

        var cssClass = GetCssClass();
        builder.AddAttribute(2, "class", $"blazor-image {cssClass}".Trim());

        builder.AddMultipleAttributes(3, AdditionalAttributes);
        builder.AddElementReferenceCapture(4, elementReference => Element = elementReference);

        builder.CloseElement();
    }

    private async Task LoadImageIfSourceProvided(int version, ImageSource? source)
    {
        if (source == null)
        {
            return;
        }

        try
        {
            SetLoadingState();

            if (CacheStrategy == CacheStrategy.Memory)
            {
                Console.WriteLine($"Loading image from memory cache: {source.CacheKey}");

                bool foundInCache = await JSRuntime.InvokeAsync<bool>(
                    "Blazor._internal.BinaryImageComponent.trySetFromCache",
                    Element, source.CacheKey);

                Console.WriteLine($"Image found in cache: {foundInCache}");

                if (foundInCache)
                {
                    if (version == _loadVersion)
                    {
                        SetSuccessState();
                    }
                    return;
                }
            }

            await StreamImageInChunks(source, version);

            if (version == _loadVersion)
            {
                SetSuccessState();
            }
        }
        catch (Exception)
        {
            if (version == _loadVersion)
            {
                SetErrorState();
            }
        }
    }

    private async Task StreamImageInChunks(ImageSource source, int version)
    {
        if (version != _loadVersion)
        {
            return;
        }

        try
        {
            string transferId = $"transfer-{Guid.NewGuid():N}";

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.initChunkedTransfer",
                Element, transferId, source.MimeType, source.CacheKey,
                CacheStrategy.ToString().ToLowerInvariant(), source.Length);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
            try
            {
                var stream = source.Stream;

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ChunkSize))) > 0)
                {
                    if (version != _loadVersion)
                    {
                        return;
                    }

                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.addChunk",
                        transferId, buffer.AsMemory(0, bytesRead).ToArray());
                }

                if (version == _loadVersion)
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.finalizeChunkedTransfer",
                        transferId);
                }
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
        Render();
    }

    private void SetSuccessState()
    {
        _isLoading = false;
        _hasError = false;
        Render();
    }

    private void SetErrorState()
    {
        _isLoading = false;
        _hasError = true;
        Render();
    }

    private string GetCssClass() => AdditionalAttributes?.TryGetValue("class", out var cssClass) == true
        ? cssClass?.ToString() ?? string.Empty : string.Empty;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (Source != null && _renderHandle.RendererInfo.IsInteractive == true)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync(
                        "Blazor._internal.BinaryImageComponent.revokeImageUrl",
                        Element);
                }
                catch (JSDisconnectedException)
                {
                    // Client disconnected
                }
            }
        }
    }
}
