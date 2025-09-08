// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Web.Image;

/* This is equivalent to a .razor file containing:
 *
 * <img data-blazor-image
 *      src="@(_currentObjectUrl)"
 *      data-state=@(IsLoading ? "loading" : _hasError ? "error" : null)
 *      @attributes="AdditionalAttributes"
 *      @ref="Element"/>
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

    private ElementReference? Element { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject] private ILogger<Image> Logger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the source for the image.
    /// </summary>
    [Parameter, EditorRequired] public required ImageSource Source { get; set; }

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

        parameters.SetParameterProperties(this);
        if (Source is null)
        {
            throw new InvalidOperationException("Image.Source is required.");
        }

        // Initialize on first parameters set
        if (!_initialized)
        {
            Render();
            _initialized = true;
            return Task.CompletedTask;
        }

        if (!HasSameKey(previousSource, Source))
        {
            Render();
        }

        return Task.CompletedTask;
    }

    async Task IHandleAfterRender.OnAfterRenderAsync()
    {
        var source = Source;
        if (!IsInteractive || source is null)
        {
            return;
        }

        if (_currentSource != null && HasSameKey(_currentSource, source))
        {
            return;
        }

        CancelPreviousLoad();
        var token = ResetCancellationToken();

        _currentSource = source;

        try
        {
            await LoadImage(source, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Render()
    {
        Debug.Assert(_renderHandle.IsInitialized);

        if (!_hasPendingRender)
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

        var showInitialLoad = Source != null && _currentSource == null && string.IsNullOrEmpty(_currentObjectUrl) && !_hasError;

        if (IsLoading || showInitialLoad)
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

    private struct ImageLoadResult
    {
        public bool Success { get; set; }
        public bool FromCache { get; set; }
        public string? ObjectUrl { get; set; }
        public string? Error { get; set; }
    }

    private async Task LoadImage(ImageSource source, CancellationToken cancellationToken)
    {
        if (!IsInteractive)
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
        }
        catch (Exception ex)
        {
            Log.LoadFailed(Logger, source.CacheKey, ex);
            if (_activeCacheKey == source.CacheKey && !cancellationToken.IsCancellationRequested)
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
            CancelPreviousLoad();
        }

        return new ValueTask();
    }

    private void CancelPreviousLoad()
    {
        try
        {
            _loadCts?.Cancel();
        }
        catch
        {
        }

        _loadCts?.Dispose();
        _loadCts = null;
    }

    private CancellationToken ResetCancellationToken()
    {
        _loadCts = new CancellationTokenSource();
        return _loadCts.Token;
    }

    private static bool HasSameKey(ImageSource? a, ImageSource? b)
    {
        return a is not null && b is not null && string.Equals(a.CacheKey, b.CacheKey, StringComparison.Ordinal);
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
