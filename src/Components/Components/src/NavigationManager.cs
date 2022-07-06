// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides an abstraction for querying and managing URI navigation.
/// </summary>
public abstract class NavigationManager
{
    private static readonly char[] UriPathEndChar = new[] { '#', '?' };

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
    /// Setting the state is allowed to support unit testing scenarios, but it will not trigger a navigation.
    /// </remarks>
    public string? State { get; protected set; }

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
    public void NavigateTo(string uri, bool forceLoad) // This overload is for binary back-compat with < 6.0
        => NavigateTo(uri, forceLoad, replace: false);

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
    /// <param name="replace">If true, replaces the current entry in the history stack. If false, appends the new entry to the history stack.</param>
    public void NavigateTo(string uri, bool forceLoad = false, bool replace = false)
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
    public void NavigateTo(string uri, NavigationOptions options)
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
    protected virtual void NavigateToCore(string uri, bool forceLoad)
        => NavigateToCore(uri, new NavigationOptions { ForceLoad = forceLoad });

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
    /// (as returned by <see cref="BaseUri"/>).</param>
    /// <param name="options">Provides additional <see cref="NavigationOptions"/>.</param>
    protected virtual void NavigateToCore(string uri, NavigationOptions options) =>
        throw new NotImplementedException($"The type {GetType().FullName} does not support supplying {nameof(NavigationOptions)}. To add support, that type should override {nameof(NavigateToCore)}(string uri, {nameof(NavigationOptions)} options).");

    /// <summary>
    /// Called to initialize BaseURI and current URI before these values are used for the first time.
    /// Override <see cref="EnsureInitialized" /> and call this method to dynamically calculate these values.
    /// </summary>
    protected void Initialize(string baseUri, string uri)
    {
        // Make sure it's possible/safe to call this method from constructors of derived classes.
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (baseUri == null)
        {
            throw new ArgumentNullException(nameof(baseUri));
        }

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
    public Uri ToAbsoluteUri(string relativeUri)
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

        var pathEndIndex = uri.IndexOfAny(UriPathEndChar);
        var uriPathOnly = pathEndIndex < 0 ? uri : uri.Substring(0, pathEndIndex);
        if ($"{uriPathOnly}/".Equals(_baseUri.OriginalString, StringComparison.Ordinal))
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
        NotifyLocationChanged(null, isInterceptedLink);
    }

    /// <summary>
    /// Triggers the <see cref="LocationChanged"/> event with the current URI value.
    /// </summary>
    protected void NotifyLocationChanged(string? state, bool isInterceptedLink)
    {
        try
        {
            State = state;
            _locationChanged?.Invoke(this, new LocationChangedEventArgs(_uri!, state, isInterceptedLink));
        }
        catch (Exception ex)
        {
            throw new LocationChangeException("An exception occurred while dispatching a location changed event.", ex);
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

        var pathEndIndex = uri.IndexOfAny(UriPathEndChar);
        var uriPathOnly = pathEndIndex < 0 ? uri : uri.Substring(0, pathEndIndex);
        if ($"{uriPathOnly}/".Equals(baseUri.OriginalString, StringComparison.Ordinal))
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
}
