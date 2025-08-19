// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

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
public partial class Image : IComponent, IHandleAfterRender, IAsyncDisposable
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
    [DisallowNull] private ElementReference? Element { get; set; }

    /// <summary>
    /// Gets the injected <see cref="IJSRuntime"/>.
    /// </summary>
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets the injected <see cref="ILogger"/>.
    /// </summary>
    [Inject] private ILogger<Image> Logger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the source for the image.
    /// </summary>
    [Parameter] public ImageSource? Source { get; set; }

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
    private void Render()
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
    private void BuildRenderTree(RenderTreeBuilder builder)
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
            Log.BeginLoad(Logger, source.CacheKey);
            SetLoadingState();

            // Always try to load from cache first
            bool foundInCache = await JSRuntime.InvokeAsync<bool>(
                "Blazor._internal.BinaryImageComponent.trySetFromCache",
                Element, source.CacheKey);

            if (foundInCache)
            {
                Log.CacheHit(Logger, source.CacheKey);
                if (_activeCacheKey == source.CacheKey)
                {
                    SetSuccessState();
                    Log.LoadSuccess(Logger, source.CacheKey);
                }
                return;
            }

            Log.StreamStart(Logger, source.CacheKey);
            await StreamImage(source);

            if (_activeCacheKey == source.CacheKey)
            {
                SetSuccessState();
                Log.LoadSuccess(Logger, source.CacheKey);
            }
        }
        catch (Exception ex)
        {
            Log.LoadFailed(Logger, source?.CacheKey ?? "(null)", ex);
            if (source != null && _activeCacheKey == source.CacheKey)
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
                    Log.RevokedUrl(Logger);
                }
                catch (JSDisconnectedException)
                {
                    // Client disconnected
                }
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Begin load for key '{CacheKey}'", EventName = "BeginLoad")]
        public static partial void BeginLoad(ILogger logger, string cacheKey);

        [LoggerMessage(2, LogLevel.Debug, "Loaded image from cache for key '{CacheKey}'", EventName = "CacheHit")]
        public static partial void CacheHit(ILogger logger, string cacheKey);

        [LoggerMessage(3, LogLevel.Debug, "Streaming image for key '{CacheKey}'", EventName = "StreamStart")]
        public static partial void StreamStart(ILogger logger, string cacheKey);

        [LoggerMessage(4, LogLevel.Debug, "Image load succeeded for key '{CacheKey}'", EventName = "LoadSuccess")]
        public static partial void LoadSuccess(ILogger logger, string cacheKey);

        [LoggerMessage(5, LogLevel.Debug, "Image load failed for key '{CacheKey}'", EventName = "LoadFailed")]
        public static partial void LoadFailed(ILogger logger, string cacheKey, Exception exception);

        [LoggerMessage(6, LogLevel.Debug, "Revoked image URL on dispose", EventName = "RevokedUrl")]
        public static partial void RevokedUrl(ILogger logger);
    }
}
