// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides an abstraction for querying and mananging URI navigation.
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

        private EventHandler<LocationChangedEventArgs> _locationChanged;

        // For the baseUri it's worth storing both the string form and Uri form and
        // keeping them in sync. These are always represented as absolute URIs with
        // a trailing slash.
        private Uri _baseUri;
        private string _baseUriString;

        // The URI. Always represented an absolute URI.
        private string _uri;

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
                return _baseUriString;
            }
            protected set
            {
                if (value != null)
                {
                    value = NormalizeBaseUri(value);
                }

                _baseUriString = value ?? "/";
                _baseUri = new Uri(_baseUriString);
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
                return _uri;
            }
            protected set
            {
                _uri = value;
            }
        }

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="BaseUri"/>).</param>
        /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
        public void NavigateTo(string uri, bool forceLoad = false)
        {
            AssertInitialized();
            NavigateToCore(uri, forceLoad);
        }

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="BaseUri"/>).</param>
        /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
        protected abstract void NavigateToCore(string uri, bool forceLoad);

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
                throw new InvalidOperationException($"'{typeof(NavigationManager).Name}' already initialized.");
            }
            _isInitialized = true;

            Uri = uri;
            BaseUri = baseUri;
        }

        /// <summary>
        /// Allows derived classes to lazyly self-initialize. Implementations that support lazy-initialization should override
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
            return new Uri(_baseUri, relativeUri);
        }

        /// <summary>
        /// Given a base URI (e.g., one previously returned by <see cref="BaseUri"/>),
        /// converts an absolute URI into one relative to the base URI prefix.
        /// </summary>
        /// <param name="uri">An absolute URI that is within the space of the base URI.</param>
        /// <returns>A relative URI path.</returns>
        public string ToBaseRelativePath(string uri)
        {
            if (uri.StartsWith(_baseUriString, StringComparison.Ordinal))
            {
                // The absolute URI must be of the form "{baseUri}something" (where
                // baseUri ends with a slash), and from that we return "something"
                return uri.Substring(_baseUriString.Length);
            }

            var hashIndex = uri.IndexOf('#');
            var uriWithoutHash = hashIndex < 0 ? uri : uri.Substring(0, hashIndex);
            if ($"{uriWithoutHash}/".Equals(_baseUriString, StringComparison.Ordinal))
            {
                // Special case: for the base URI "/something/", if you're at
                // "/something" then treat it as if you were at "/something/" (i.e.,
                // with the trailing slash). It's a bit ambiguous because we don't know
                // whether the server would return the same page whether or not the
                // slash is present, but ASP.NET Core at least does by default when
                // using PathBase.
                return uri.Substring(_baseUriString.Length - 1);
            }

            var message = $"The URI '{uri}' is not contained by the base URI '{_baseUriString}'.";
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
            _locationChanged?.Invoke(this, new LocationChangedEventArgs(_uri, isInterceptedLink));
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
    }
}
