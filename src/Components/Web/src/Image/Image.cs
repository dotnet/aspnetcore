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
 *      data-state=@(_isLoading ? "loading" : _hasError ? "error" : null)
 *      @ref="Element" @attributes="AdditionalAttributes" />
 *
 */
/// <summary>
/// A component that efficiently renders images from non-HTTP sources like byte arrays.
/// </summary>
public partial class Image : IComponent, IHandleAfterRender, IAsyncDisposable
{
    private RenderHandle _renderHandle;
    private string? _currentObjectUrl;
    private bool _hasError;
    private bool _isDisposed;
    private bool _initialized;
    private bool _hasPendingRender;
    private string? _activeCacheKey;
    private ImageSource? _currentSource;
    private CancellationTokenSource? _loadCts;
    private bool IsLoading => _currentSource != null && string.IsNullOrEmpty(_currentObjectUrl) && !_hasError;

    private bool IsInteractive => _renderHandle.IsInitialized &&
                                _renderHandle.RendererInfo.IsInteractive;

    [DisallowNull] private ElementReference? Element { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject] private ILogger<Image> Logger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the source for the image.
    /// </summary>
    [Parameter] public ImageSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the attributes for the image.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        if (_renderHandle.IsInitialized)
        {
            throw new InvalidOperationException("Component is already attached to a render handle.");
        }
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        var previousSource = Source;

        // Set component parameters
        parameters.SetParameterProperties(this);

        // Initialize on first parameters set
        if (!_initialized)
        {
            Render();
            _initialized = true;
            return Task.CompletedTask;
        }

        if (Source != null && !string.Equals(previousSource?.CacheKey, Source.CacheKey, StringComparison.Ordinal))
        {
            Render();
        }

        return Task.CompletedTask;
    }

    async Task IHandleAfterRender.OnAfterRenderAsync()
    {
        if (!IsInteractive || Source == null)
        {
            return;
        }

        if (_currentSource != null && string.Equals(_currentSource.CacheKey, Source.CacheKey, StringComparison.Ordinal))
        {
            return;
        }

        // Cancel any in-progress load operation
        try { _loadCts?.Cancel(); } catch { }
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        _currentSource = Source;

        try
        {
            await LoadImage(Source, token);
        }
        catch (OperationCanceledException)
        {

        }
    }

    private void Render()
    {
        if (!_hasPendingRender && _renderHandle.IsInitialized)
        {
            _hasPendingRender = true;
            _renderHandle.Render(BuildRenderTree);
            _hasPendingRender = false;
        }
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "img");

        if (!string.IsNullOrEmpty(_currentObjectUrl))
        {
            builder.AddAttribute(1, "src", _currentObjectUrl);
        }

        builder.AddAttribute(2, "data-blazor-image", "");

        var showInitial = Source != null && _currentSource == null && string.IsNullOrEmpty(_currentObjectUrl) && !_hasError;

        if (IsLoading || showInitial)
        {
            builder.AddAttribute(3, "data-state", "loading");
        }
        else if (_hasError)
        {
            builder.AddAttribute(3, "data-state", "error");
        }

        builder.AddMultipleAttributes(4, AdditionalAttributes);
        builder.AddElementReferenceCapture(5, elementReference => Element = elementReference);

        builder.CloseElement();
    }

    private sealed class ImageLoadResult
    {
        public bool Success { get; set; }
        public bool FromCache { get; set; }
        public string? ObjectUrl { get; set; }
        public string? Error { get; set; }
    }

    private async Task LoadImage(ImageSource? source, CancellationToken cancellationToken)
    {
        if (source == null || !IsInteractive)
        {
            return;
        }

        _activeCacheKey = source.CacheKey;

        try
        {
            Log.BeginLoad(Logger, source.CacheKey);

            cancellationToken.ThrowIfCancellationRequested();

            using var streamRef = new DotNetStreamReference(source.Stream, leaveOpen: true);

            var result = await JSRuntime.InvokeAsync<ImageLoadResult>(
                "Blazor._internal.BinaryImageComponent.setImageAsync",
                cancellationToken,
                Element,
                streamRef,
                source.MimeType,
                source.CacheKey,
                source.Length);

            if (_activeCacheKey == source.CacheKey && !cancellationToken.IsCancellationRequested)
            {
                if (result.Success)
                {
                    _currentObjectUrl = result.ObjectUrl;
                    _hasError = false;

                    if (result.FromCache)
                    {
                        Log.CacheHit(Logger, source.CacheKey);
                    }
                    else
                    {
                        Log.StreamStart(Logger, source.CacheKey);
                    }

                    Log.LoadSuccess(Logger, source.CacheKey);
                }
                else
                {
                    _hasError = true;
                    Log.LoadFailed(Logger, source.CacheKey, new InvalidOperationException(result.Error ?? "Image load failed"));
                }

                Render();
            }
        }
        catch (OperationCanceledException)
        {
            // bubble up to caller
            throw;
        }
        catch (Exception ex)
        {
            Log.LoadFailed(Logger, source?.CacheKey ?? "(null)", ex);
            if (source != null && _activeCacheKey == source.CacheKey && !cancellationToken.IsCancellationRequested)
            {
                _currentObjectUrl = null;
                _hasError = true;
                Render();
            }
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            // Cancel any pending operations
            try { _loadCts?.Cancel(); } catch { }
            _loadCts?.Dispose();
            _loadCts = null;
        }

        return new ValueTask();
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
