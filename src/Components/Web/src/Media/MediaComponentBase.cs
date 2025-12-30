// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Media;

/// <summary>
/// Base component that handles turning a media stream into an object URL plus caching and lifetime management.
/// Subclasses implement their own rendering and provide the target attribute (e.g., <c>src</c> or <c>href</c>) used
/// </summary>
public abstract partial class MediaComponentBase : IComponent, IHandleAfterRender, IAsyncDisposable
{
    private RenderHandle _renderHandle;

    /// <summary>
    /// The current object URL (blob URL) assigned to the underlying element, or <c>null</c> if not yet loaded
    /// or if a previous load failed/was cancelled.
    /// </summary>
    internal string? _currentObjectUrl;

    /// <summary>
    /// Indicates whether the last load attempt ended in an error state for the active cache key.
    /// </summary>
    internal bool _hasError;

    private bool _isDisposed;
    private bool _initialized;
    private bool _hasPendingRender;

    /// <summary>
    /// The cache key associated with the currently active/most recent load operation. Used to ignore
    /// out-of-order JS interop responses belonging to stale operations.
    /// </summary>
    internal string? _activeCacheKey;

    /// <summary>
    /// The <see cref="MediaSource"/> instance currently being processed (or <c>null</c> if none).
    /// </summary>
    internal MediaSource? _currentSource;
    private CancellationTokenSource? _loadCts;

    /// <summary>
    /// Gets a value indicating whether the component is currently loading the media content.
    /// True when a source has been provided, no object URL is available yet, and there is no error.
    /// </summary>
    internal bool IsLoading => _currentSource != null && string.IsNullOrEmpty(_currentObjectUrl) && !_hasError;

    /// <summary>
    /// Gets a value indicating whether the renderer is interactive so client-side JS interop can be performed.
    /// </summary>
    internal bool IsInteractive => _renderHandle.IsInitialized && _renderHandle.RendererInfo.IsInteractive;

    /// <summary>
    /// Gets the reference to the rendered HTML element for this media component.
    /// </summary>
    internal ElementReference? Element { get; set; }

    /// <summary>
    /// Gets or sets the JS runtime used for interop with the browser to materialize media object URLs.
    /// </summary>
    [Inject] internal IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets or sets the logger factory used to create the <see cref="Logger"/> instance.
    /// </summary>
    [Inject] internal ILoggerFactory LoggerFactory { get; set; } = default!;

    /// <summary>
    /// Logger for media operations.
    /// </summary>
    private ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());
    private ILogger? _logger;

    internal abstract string TargetAttributeName { get; }

    /// <summary>
    /// Determines whether the component should automatically invoke a media load after the first render
    /// and whenever the <see cref="Source"/> changes. Override and return <c>false</c> for components
    /// (such as download buttons) that defer loading until an explicit user action.
    /// </summary>
    internal virtual bool ShouldAutoLoad => true;

    /// <summary>
    /// Gets or sets the media source.
    /// </summary>
    [Parameter, EditorRequired] public required MediaSource Source { get; set; }

    /// <summary>
    /// Unmatched attributes applied to the rendered element.
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
            throw new InvalidOperationException($"{nameof(MediaComponentBase)}.{nameof(Source)} is required.");
        }

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
        if (!IsInteractive || source is null || !ShouldAutoLoad)
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
            await LoadMediaAsync(source, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Triggers a render of the component by invoking the <see cref="BuildRenderTree"/> method.
    /// Ensures that only one render operation is pending at a time to prevent redundant renders.
    /// </summary>
    internal void Render()
    {
        Debug.Assert(_renderHandle.IsInitialized);

        if (!_hasPendingRender)
        {
            _hasPendingRender = true;
            _renderHandle.Render(BuildRenderTree);
            _hasPendingRender = false;
        }
    }

    private protected virtual void BuildRenderTree(RenderTreeBuilder builder) { }

    private sealed class MediaLoadResult
    {
        public bool Success { get; set; }
        public bool FromCache { get; set; }
        public string? ObjectUrl { get; set; }
        public string? Error { get; set; }
    }

    private async Task LoadMediaAsync(MediaSource? source, CancellationToken cancellationToken)
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

            var result = await JSRuntime.InvokeAsync<MediaLoadResult>(
                "Blazor._internal.BinaryMedia.setContentAsync",
                cancellationToken,
                Element,
                streamRef,
                source.MimeType,
                source.CacheKey,
                source.Length,
                TargetAttributeName);

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
                    Log.LoadFailed(Logger, source.CacheKey, new InvalidOperationException(result.Error ?? "Unknown error"));
                }

                Render();
            }
        }
        catch (OperationCanceledException)
        {
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
            CancelPreviousLoad();
        }
        return new ValueTask();
    }

    /// <summary>
    /// Cancels any in-flight media load operation, if one is active, by signalling its <see cref="CancellationTokenSource"/>.
    /// </summary>
    internal void CancelPreviousLoad()
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

    /// <summary>
    /// Creates a new <see cref="CancellationTokenSource"/> for an upcoming load operation and returns its token.
    /// </summary>
    internal CancellationToken ResetCancellationToken()
    {
        _loadCts = new CancellationTokenSource();
        return _loadCts.Token;
    }

    private static bool HasSameKey(MediaSource? a, MediaSource? b)
    {
        return a is not null && b is not null && string.Equals(a.CacheKey, b.CacheKey, StringComparison.Ordinal);
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Begin load for key '{CacheKey}'", EventName = "BeginLoad")]
        public static partial void BeginLoad(ILogger logger, string cacheKey);

        [LoggerMessage(2, LogLevel.Debug, "Loaded media from cache for key '{CacheKey}'", EventName = "CacheHit")]
        public static partial void CacheHit(ILogger logger, string cacheKey);

        [LoggerMessage(3, LogLevel.Debug, "Streaming media for key '{CacheKey}'", EventName = "StreamStart")]
        public static partial void StreamStart(ILogger logger, string cacheKey);

        [LoggerMessage(4, LogLevel.Debug, "Media load succeeded for key '{CacheKey}'", EventName = "LoadSuccess")]
        public static partial void LoadSuccess(ILogger logger, string cacheKey);

        [LoggerMessage(5, LogLevel.Debug, "Media load failed for key '{CacheKey}'", EventName = "LoadFailed")]
        public static partial void LoadFailed(ILogger logger, string cacheKey, Exception exception);
    }
}
