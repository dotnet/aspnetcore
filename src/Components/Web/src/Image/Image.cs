// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Image;

/* This is equivalent to a .razor file containing:
 *
 * <img class="blazor-image @GetCssClass()"
 *      data-state="@(_isLoading ? "loading" : _hasError ? "error" : null)"
 *      @ref="Element" @attributes="AdditionalAttributes" />
 *
 */
/// <summary>
/// A component that efficiently renders images from non-HTTP sources like byte arrays.
/// </summary>
public class Image : IComponent, IHandleAfterRender, IAsyncDisposable
{
    private RenderHandle _renderHandle;
    private bool _isLoading = true;
    private bool _hasError;
    private bool _isDisposed;
    private bool _initialized;
    private bool _hasPendingRender;
    private bool _firstRender = true;
    private string? _activeCacheKey;

    private bool IsInteractive => _renderHandle.IsInitialized &&
                                _renderHandle.RendererInfo.IsInteractive;

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
            return;
        }

        // Handle parameter changes
        if (!_isDisposed && Source != null && !string.Equals(previousSource?.CacheKey, Source.CacheKey, StringComparison.Ordinal))
        {
            await LoadImage(Source);
        }
    }

    /// <inheritdoc />
    public async Task OnAfterRenderAsync()
    {
        if (!IsInteractive)
        {
            return;
        }

        if (_firstRender && Source != null && !_isDisposed)
        {
            _firstRender = false;
            await LoadImage(Source);
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

    private async Task LoadImage(ImageSource? source)
    {
        if (source == null || !IsInteractive)
        {
            return;
        }

        _activeCacheKey = source.CacheKey;

        try
        {
            SetLoadingState();

            if (CacheStrategy == CacheStrategy.Memory)
            {
                Console.WriteLine($"Loading image from memory cache: {source.CacheKey}");

                bool foundInCache = await JSRuntime.InvokeAsync<bool>(
                    "Blazor._internal.BinaryImageComponent.trySetFromCache",
                    Element, source.CacheKey);

                if (foundInCache)
                {
                    if (_activeCacheKey == source.CacheKey)
                    {
                        Console.WriteLine($"Image loaded from memory cache: {source.CacheKey}");
                        SetSuccessState();
                    }
                    return;
                }
            }

            await StreamImage(source);

            if (_activeCacheKey == source.CacheKey)
            {
                SetSuccessState();
            }
        }
        catch (Exception)
        {
            if (_activeCacheKey == source.CacheKey)
            {
                SetErrorState();
            }
        }
    }

    private async Task StreamImage(ImageSource source)
    {
        if (!IsInteractive)
        {
            return;
        }

        if (source.Stream.CanSeek && source.Stream.Position != 0)
        {
            throw new InvalidOperationException("ImageSource stream must be at position 0 when starting a load.");
        }

        var loadKey = source.CacheKey;
        try
        {
            using var streamRef = new DotNetStreamReference(source.Stream, leaveOpen: true);

            await JSRuntime.InvokeVoidAsync(
                "Blazor._internal.BinaryImageComponent.loadImageFromStream",
                Element,
                streamRef,
                source.MimeType,
                source.CacheKey,
                CacheStrategy.ToString().ToLowerInvariant(),
                source.Length);
        }
        catch (Exception ex)
        {
            if (_activeCacheKey == loadKey)
            {
                throw new InvalidOperationException($"Failed to stream image data via stream reference: {ex.Message}", ex);
            }
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

            if (Source != null && IsInteractive)
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
