// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides an abstraction for querying and managing URI navigation.
/// </summary>
public abstract class NavigationManager
{
    /// <summary>
    /// An event that fires when the navigation location has changed.
    /// </summary>
    public event EventHandler<LocationChangedEventArgs> LocationChanged
    {
        add
        {
            AssertInitialized();
            _locationChanged += value;
        }
        remove
        {
            AssertInitialized();
            _locationChanged -= value;
        }
    }

    private EventHandler<LocationChangedEventArgs>? _locationChanged;

    private readonly List<Func<LocationChangingContext, ValueTask>> _locationChangingHandlers = new();

    private CancellationTokenSource? _locationChangingCts;

    // For the baseUri it's worth storing as a System.Uri so we can do operations
    // on that type. System.Uri gives us access to the original string anyway.
    private Uri? _baseUri;

    // The URI. Always represented an absolute URI.
    private string? _uri;
    private bool _isInitialized;

    /// <summary>
    /// Gets or sets the current base URI. The <see cref="BaseUri" /> is always represented as an absolute URI in string form with trailing slash.
    /// Typically this corresponds to the 'href' attribute on the document's &lt;base&gt; element.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="BaseUri" /> will not trigger the <see cref="LocationChanged" /> event.
    /// </remarks>
    public string BaseUri
    {
        get
        {
            AssertInitialized();
            return _baseUri!.OriginalString;
        }
        protected set
        {
            if (value != null)
            {
                value = NormalizeBaseUri(value);
            }

            _baseUri = new Uri(value!, UriKind.Absolute);
        }
    }

    /// <summary>
    /// Gets or sets the current URI. The <see cref="Uri" /> is always represented as an absolute URI in string form.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="Uri" /> will not trigger the <see cref="LocationChanged" /> event.
    /// </remarks>
    public string Uri
    {
        get
        {
            AssertInitialized();
            return _uri!;
        }
        protected set
        {
            Validate(_baseUri, value);
            _uri = value;
        }
    }

    /// <summary>
    /// Gets or sets the state associated with the current navigation.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="HistoryEntryState" /> will not trigger the <see cref="LocationChanged" /> event.
    /// </remarks>
    public string? HistoryEntryState { get; protected set; }

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
    public void NavigateTo([StringSyntax(StringSyntaxAttribute.Uri)] string uri, bool forceLoad) // This overload is for binary back-compat with < 6.0
        => NavigateTo(uri, forceLoad, replace: false);

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
    /// <param name="replace">If true, replaces the current entry in the history stack. If false, appends the new entry to the history stack.</param>
    public void NavigateTo([StringSyntax(StringSyntaxAttribute.Uri)] string uri, bool forceLoad = false, bool replace = false)
    {
        AssertInitialized();

        if (replace)
        {
            NavigateToCore(uri, new NavigationOptions
            {
                ForceLoad = forceLoad,
                ReplaceHistoryEntry = replace,
            });
        }
        else
        {
            // For back-compatibility, we must call the (string, bool) overload of NavigateToCore from here,
            // because that's the only overload guaranteed to be implemented in subclasses.
            NavigateToCore(uri, forceLoad);
        }
    }

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="options">Provides additional <see cref="NavigationOptions"/>.</param>
    public void NavigateTo([StringSyntax(StringSyntaxAttribute.Uri)] string uri, NavigationOptions options)
    {
        AssertInitialized();
        NavigateToCore(uri, options);
    }

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
    // The reason this overload exists and is virtual is for back-compat with < 6.0. Existing NavigationManager subclasses may
    // already override this, so the framework needs to keep using it for the cases when only pre-6.0 options are used.
    // However, for anyone implementing a new NavigationManager post-6.0, we don't want them to have to override this
    // overload any more, so there's now a default implementation that calls the updated overload.
    protected virtual void NavigateToCore([StringSyntax(StringSyntaxAttribute.Uri)] string uri, bool forceLoad)
        => NavigateToCore(uri, new NavigationOptions { ForceLoad = forceLoad });

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="options">Provides additional <see cref="NavigationOptions"/>.</param>
    protected virtual void NavigateToCore([StringSyntax(StringSyntaxAttribute.Uri)] string uri, NavigationOptions options) =>
        throw new NotImplementedException($"The type {GetType().FullName} does not support supplying {nameof(NavigationOptions)}. To add support, that type should override {nameof(NavigateToCore)}(string uri, {nameof(NavigationOptions)} options).");

    /// <summary>
    /// Refreshes the current page via request to the server.
    /// </summary>
    /// <remarks>
    /// If <paramref name="forceReload"/> is <c>true</c>, a full page reload will always be performed.
    /// Otherwise, the response HTML may be merged with the document's existing HTML to preserve client-side state,
    /// falling back on a full page reload if necessary.
    /// </remarks>
    public virtual void Refresh(bool forceReload = false)
        => NavigateTo(Uri, forceLoad: true, replace: true);

    /// <summary>
    /// Called to initialize BaseURI and current URI before these values are used for the first time.
    /// Override <see cref="EnsureInitialized" /> and call this method to dynamically calculate these values.
    /// </summary>
    protected void Initialize(string baseUri, string uri)
    {
        // Make sure it's possible/safe to call this method from constructors of derived classes.
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(baseUri);

        if (_isInitialized)
        {
            throw new InvalidOperationException($"'{GetType().Name}' already initialized.");
        }

        _isInitialized = true;

        // Setting BaseUri before Uri so they get validated.
        BaseUri = baseUri;
        Uri = uri;
    }

    /// <summary>
    /// Allows derived classes to lazily self-initialize. Implementations that support lazy-initialization should override
    /// this method and call <see cref="Initialize(string, string)" />.
    /// </summary>
    protected virtual void EnsureInitialized()
    {
    }

    /// <summary>
    /// Converts a relative URI into an absolute one (by resolving it
    /// relative to the current absolute URI).
    /// </summary>
    /// <param name="relativeUri">The relative URI.</param>
    /// <returns>The absolute URI.</returns>
    public Uri ToAbsoluteUri(string? relativeUri)
    {
        AssertInitialized();
        return new Uri(_baseUri!, relativeUri);
    }

    /// <summary>
    /// Given a base URI (e.g., one previously returned by <see cref="BaseUri"/>),
    /// converts an absolute URI into one relative to the base URI prefix.
    /// </summary>
    /// <param name="uri">An absolute URI that is within the space of the base URI.</param>
    /// <returns>A relative URI path.</returns>
    public string ToBaseRelativePath(string uri)
    {
        if (uri.StartsWith(_baseUri!.OriginalString, StringComparison.Ordinal))
        {
            // The absolute URI must be of the form "{baseUri}something" (where
            // baseUri ends with a slash), and from that we return "something"
            return uri.Substring(_baseUri.OriginalString.Length);
        }

        var pathEndIndex = uri.AsSpan().IndexOfAny('#', '?');
        var uriPathOnly = pathEndIndex < 0 ? uri : uri.AsSpan(0, pathEndIndex);
        if (_baseUri.OriginalString.EndsWith('/') && uriPathOnly.Equals(_baseUri.OriginalString.AsSpan(0, _baseUri.OriginalString.Length - 1), StringComparison.Ordinal))
        {
            // Special case: for the base URI "/something/", if you're at
            // "/something" then treat it as if you were at "/something/" (i.e.,
            // with the trailing slash). It's a bit ambiguous because we don't know
            // whether the server would return the same page whether or not the
            // slash is present, but ASP.NET Core at least does by default when
            // using PathBase.
            return uri.Substring(_baseUri.OriginalString.Length - 1);
        }

        var message = $"The URI '{uri}' is not contained by the base URI '{_baseUri}'.";
        throw new ArgumentException(message);
    }

    internal ReadOnlySpan<char> ToBaseRelativePath(ReadOnlySpan<char> uri)
    {
        if (MemoryExtensions.StartsWith(uri, _baseUri!.OriginalString.AsSpan(), StringComparison.Ordinal))
        {
            // The absolute URI must be of the form "{baseUri}something" (where
            // baseUri ends with a slash), and from that we return "something"
            return uri[_baseUri.OriginalString.Length..];
        }

        var pathEndIndex = uri.IndexOfAny('#', '?');
        var uriPathOnly = pathEndIndex < 0 ? uri : uri[..pathEndIndex];
        if (_baseUri.OriginalString.EndsWith('/') && MemoryExtensions.Equals(uriPathOnly, _baseUri.OriginalString.AsSpan(0, _baseUri.OriginalString.Length - 1), StringComparison.Ordinal))
        {
            // Special case: for the base URI "/something/", if you're at
            // "/something" then treat it as if you were at "/something/" (i.e.,
            // with the trailing slash). It's a bit ambiguous because we don't know
            // whether the server would return the same page whether or not the
            // slash is present, but ASP.NET Core at least does by default when
            // using PathBase.
            return uri[(_baseUri.OriginalString.Length - 1)..];
        }

        var message = $"The URI '{uri}' is not contained by the base URI '{_baseUri}'.";
        throw new ArgumentException(message);
    }

    internal static string NormalizeBaseUri(string baseUri)
    {
        var lastSlashIndex = baseUri.LastIndexOf('/');
        if (lastSlashIndex >= 0)
        {
            baseUri = baseUri.Substring(0, lastSlashIndex + 1);
        }

        return baseUri;
    }

    /// <summary>
    /// Triggers the <see cref="LocationChanged"/> event with the current URI value.
    /// </summary>
    protected void NotifyLocationChanged(bool isInterceptedLink)
    {
        try
        {
            _locationChanged?.Invoke(
                this,
                new LocationChangedEventArgs(_uri!, isInterceptedLink)
                {
                    HistoryEntryState = HistoryEntryState
                });
        }
        catch (Exception ex)
        {
            throw new LocationChangeException("An exception occurred while dispatching a location changed event.", ex);
        }
    }

    /// <summary>
    /// Notifies the registered handlers of the current location change.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI.</param>
    /// <param name="state">The state associated with the target history entry.</param>
    /// <param name="isNavigationIntercepted">Whether this navigation was intercepted from a link.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the completion of the operation. If the result is <see langword="true"/>, the navigation should continue.</returns>
    protected async ValueTask<bool> NotifyLocationChangingAsync(string uri, string? state, bool isNavigationIntercepted)
    {
        _locationChangingCts?.Cancel();
        _locationChangingCts = null;

        var handlerCount = _locationChangingHandlers.Count;

        if (handlerCount == 0)
        {
            return true;
        }

        var cts = new CancellationTokenSource();

        _locationChangingCts = cts;

        var cancellationToken = cts.Token;
        var context = new LocationChangingContext
        {
            TargetLocation = uri,
            HistoryEntryState = state,
            IsNavigationIntercepted = isNavigationIntercepted,
            CancellationToken = cancellationToken,
        };

        try
        {
            if (handlerCount == 1)
            {
                var handlerTask = InvokeLocationChangingHandlerAsync(_locationChangingHandlers[0], context);

                if (handlerTask.IsFaulted)
                {
                    await handlerTask;
                    return false; // Unreachable because the previous line will throw.
                }

                if (context.DidPreventNavigation)
                {
                    return false;
                }

                if (!handlerTask.IsCompletedSuccessfully)
                {
                    await handlerTask.AsTask().WaitAsync(cancellationToken);
                }
            }
            else
            {
                var locationChangingHandlersCopy = ArrayPool<Func<LocationChangingContext, ValueTask>>.Shared.Rent(handlerCount);

                try
                {
                    _locationChangingHandlers.CopyTo(locationChangingHandlersCopy);

                    var locationChangingTasks = new HashSet<Task>();

                    for (var i = 0; i < handlerCount; i++)
                    {
                        var handlerTask = InvokeLocationChangingHandlerAsync(locationChangingHandlersCopy[i], context);

                        if (handlerTask.IsFaulted)
                        {
                            await handlerTask;
                            return false; // Unreachable because the previous line will throw.
                        }

                        if (context.DidPreventNavigation)
                        {
                            return false;
                        }

                        locationChangingTasks.Add(handlerTask.AsTask());
                    }

                    while (locationChangingTasks.Count != 0)
                    {
                        var completedHandlerTask = await Task.WhenAny(locationChangingTasks).WaitAsync(cancellationToken);

                        if (completedHandlerTask.IsFaulted)
                        {
                            await completedHandlerTask;
                            return false; // Unreachable because the previous line will throw.
                        }

                        if (context.DidPreventNavigation)
                        {
                            return false;
                        }

                        locationChangingTasks.Remove(completedHandlerTask);
                    }
                }
                finally
                {
                    ArrayPool<Func<LocationChangingContext, ValueTask>>.Shared.Return(locationChangingHandlersCopy);
                }
            }

            return !context.DidPreventNavigation;
        }
        catch (TaskCanceledException ex)
        {
            if (ex.CancellationToken == cancellationToken)
            {
                // This navigation was in progress when a successive navigation occurred.
                // We treat this as a canceled navigation.
                return false;
            }

            throw;
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();

            if (_locationChangingCts == cts)
            {
                _locationChangingCts = null;
            }
        }
    }

    private async ValueTask InvokeLocationChangingHandlerAsync(Func<LocationChangingContext, ValueTask> handler, LocationChangingContext context)
    {
        try
        {
            await handler(context);
        }
        catch (OperationCanceledException)
        {
            // Ignore exceptions caused by cancellations.
        }
        catch (Exception ex)
        {
            HandleLocationChangingHandlerException(ex, context);
        }
    }

    /// <summary>
    /// Handles exceptions thrown in location changing handlers.
    /// </summary>
    /// <param name="ex">The exception to handle.</param>
    /// <param name="context">The context passed to the handler.</param>
    protected virtual void HandleLocationChangingHandlerException(Exception ex, LocationChangingContext context)
        => throw new InvalidOperationException($"To support navigation locks, {GetType().Name} must override {nameof(HandleLocationChangingHandlerException)}");

    /// <summary>
    /// Sets whether navigation is currently locked. If it is, then implementations should not update <see cref="Uri"/> and call
    /// <see cref="NotifyLocationChanged(bool)"/> until they have first confirmed the navigation by calling
    /// <see cref="NotifyLocationChangingAsync(string, string?, bool)"/>.
    /// </summary>
    /// <param name="value">Whether navigation is currently locked.</param>
    protected virtual void SetNavigationLockState(bool value)
        => throw new NotSupportedException($"To support navigation locks, {GetType().Name} must override {nameof(SetNavigationLockState)}");

    /// <summary>
    /// Registers a handler to process incoming navigation events.
    /// </summary>
    /// <param name="locationChangingHandler">The handler to process incoming navigation events.</param>
    /// <returns>An <see cref="IDisposable"/> that can be disposed to unregister the location changing handler.</returns>
    public IDisposable RegisterLocationChangingHandler(Func<LocationChangingContext, ValueTask> locationChangingHandler)
    {
        AssertInitialized();

        var isFirstHandler = _locationChangingHandlers.Count == 0;

        _locationChangingHandlers.Add(locationChangingHandler);

        if (isFirstHandler)
        {
            SetNavigationLockState(true);
        }

        return new LocationChangingRegistration(locationChangingHandler, this);
    }

    private void RemoveLocationChangingHandler(Func<LocationChangingContext, ValueTask> locationChangingHandler)
    {
        AssertInitialized();

        if (_locationChangingHandlers.Remove(locationChangingHandler) && _locationChangingHandlers.Count == 0)
        {
            SetNavigationLockState(false);
        }
    }

    private void AssertInitialized()
    {
        if (!_isInitialized)
        {
            EnsureInitialized();
        }

        if (!_isInitialized)
        {
            throw new InvalidOperationException($"'{GetType().Name}' has not been initialized.");
        }
    }

    private static bool TryGetLengthOfBaseUriPrefix(Uri baseUri, string uri, out int length)
    {
        if (uri.StartsWith(baseUri.OriginalString, StringComparison.Ordinal))
        {
            // The absolute URI must be of the form "{baseUri}something" (where
            // baseUri ends with a slash), and from that we return "something"
            length = baseUri.OriginalString.Length;
            return true;
        }

        var pathEndIndex = uri.AsSpan().IndexOfAny('#', '?');
        var uriPathOnly = pathEndIndex < 0 ? uri : uri.AsSpan(0, pathEndIndex);
        if (baseUri.OriginalString.EndsWith('/') && uriPathOnly.Equals(baseUri.OriginalString.AsSpan(0, baseUri.OriginalString.Length - 1), StringComparison.Ordinal))
        {
            // Special case: for the base URI "/something/", if you're at
            // "/something" then treat it as if you were at "/something/" (i.e.,
            // with the trailing slash). It's a bit ambiguous because we don't know
            // whether the server would return the same page whether or not the
            // slash is present, but ASP.NET Core at least does by default when
            // using PathBase.
            length = baseUri.OriginalString.Length - 1;
            return true;
        }

        length = 0;
        return false;
    }

    private static void Validate(Uri? baseUri, string uri)
    {
        if (baseUri == null || uri == null)
        {
            return;
        }

        if (!TryGetLengthOfBaseUriPrefix(baseUri, uri, out _))
        {
            var message = $"The URI '{uri}' is not contained by the base URI '{baseUri}'.";
            throw new ArgumentException(message);
        }
    }

    private sealed class LocationChangingRegistration : IDisposable
    {
        private readonly Func<LocationChangingContext, ValueTask> _handler;
        private readonly NavigationManager _navigationManager;

        public LocationChangingRegistration(Func<LocationChangingContext, ValueTask> handler, NavigationManager navigationManager)
        {
            _handler = handler;
            _navigationManager = navigationManager;
        }

        public void Dispose()
        {
            _navigationManager.RemoveLocationChangingHandler(_handler);
        }
    }
}
