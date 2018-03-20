// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using Microsoft.AspNetCore.Blazor.Services;
using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Services
{
    /// <summary>
    /// Default browser implementation of <see cref="IUriHelper"/>.
    /// </summary>
    public class BrowserUriHelper : IUriHelper
    {
        // Since there's only one browser (and hence only one navigation state), the internal state
        // is all static. In typical usage the DI system will register BrowserUriHelper as a singleton
        // so it makes no difference, but if you manually instantiate more than one BrowserUriHelper
        // that's fine too - they will just share their internal state.
        // This class will never be used during server-side prerendering, so we don't have thread-
        // safety concerns due to the static state.
        static readonly string _functionPrefix = typeof(BrowserUriHelper).FullName;
        static bool _hasEnabledNavigationInterception;
        static string _cachedAbsoluteUri;
        static EventHandler<string> _onLocationChanged;
        static string _baseUriStringNoTrailingSlash; // No trailing slash so we can just prepend it to suffixes
        static Uri _baseUriWithTrailingSlash; // With trailing slash so it can be used in new Uri(base, relative)

        /// <inheritdoc />
        public event EventHandler<string> OnLocationChanged
        {
            add
            {
                EnsureNavigationInteceptionEnabled();
                _onLocationChanged += value;
            }
            remove
            {
                // We could consider deactivating the JS-side enableNavigationInteception
                // if there are no remaining listeners, but we don't need that currently.
                // If we ever do that, will also need to change the logic inside GetAbsoluteUri
                // so it knows not to continue using the cached URI.
                _onLocationChanged -= value;
            }
        }

        /// <inheritdoc />
        public string GetBaseUriPrefix()
        {
            EnsureBaseUriPopulated();
            return _baseUriStringNoTrailingSlash;
        }

        /// <inheritdoc />
        public string GetAbsoluteUri()
        {
            if (_cachedAbsoluteUri == null)
            {
                var newUri = RegisteredFunction.InvokeUnmarshalled<string>(
                    $"{_functionPrefix}.getLocationHref");

                if (_hasEnabledNavigationInterception)
                {
                    // Once we turn on navigation interception, we no longer have to query
                    // the browser for its URI each time (because we'd know if it had changed)
                    _cachedAbsoluteUri = newUri;
                }

                return newUri;
            }
            else
            {
                return _cachedAbsoluteUri;
            }
        }

        /// <inheritdoc />
        public Uri ToAbsoluteUri(string relativeUri)
        {
            EnsureBaseUriPopulated();
            return new Uri(_baseUriWithTrailingSlash, relativeUri);
        }

        /// <inheritdoc />
        public string ToBaseRelativePath(string baseUriPrefix, string absoluteUri)
        {
            if (absoluteUri.Equals(baseUriPrefix, StringComparison.Ordinal))
            {
                // Special case: if you're exactly at the base URI, treat it as if you
                // were at "{baseUriPrefix}/" (i.e., with a following slash). It's a bit
                // ambiguous because we don't know whether the server would return the
                // same page whether or not the slash is present, but ASP.NET Core at
                // least does by default when using PathBase.
                return "/";
            }
            else if (absoluteUri.StartsWith(baseUriPrefix, StringComparison.Ordinal)
                && absoluteUri.Length > baseUriPrefix.Length
                && absoluteUri[baseUriPrefix.Length] == '/')
            {
                // The absolute URI must be of the form "{baseUriPrefix}/something",
                // and from that we return "/something"
                return absoluteUri.Substring(baseUriPrefix.Length);
            }

            throw new ArgumentException($"The URI '{absoluteUri}' is not contained by the base URI '{baseUriPrefix}'.");
        }

        /// <inheritdoc />
        public void NavigateTo(string uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            RegisteredFunction.InvokeUnmarshalled<object>($"{_functionPrefix}.navigateTo", uri);
        }

        private static void EnsureBaseUriPopulated()
        {
            // The <base href> is fixed for the lifetime of the page, so just cache it
            if (_baseUriStringNoTrailingSlash == null)
            {
                var baseUri = RegisteredFunction.InvokeUnmarshalled<string>(
                    $"{_functionPrefix}.getBaseURI");
                _baseUriStringNoTrailingSlash = ToBaseUriPrefix(baseUri);
                _baseUriWithTrailingSlash = new Uri(_baseUriStringNoTrailingSlash + "/");
            }
        }

        private static void NotifyLocationChanged(string newAbsoluteUri)
        {
            _cachedAbsoluteUri = newAbsoluteUri;
            _onLocationChanged?.Invoke(null, newAbsoluteUri);
        }

        private static void EnsureNavigationInteceptionEnabled()
        {
            // Don't need thread safety because:
            // (1) there's only one UI thread
            // (2) doesn't matter if we call enableNavigationInteception more than once anyway
            if (!_hasEnabledNavigationInterception)
            {
                _hasEnabledNavigationInterception = true;
                RegisteredFunction.InvokeUnmarshalled<object>(
                    $"{_functionPrefix}.enableNavigationInteception");
            }
        }

        /// <summary>
        /// Given the document's document.baseURI value, returns the URI prefix
        /// that can be prepended to URI paths to produce an absolute URI.
        /// This is computed by removing the final slash and any following characters.
        /// Internal for tests.
        /// </summary>
        /// <param name="baseUri">The page's document.baseURI value.</param>
        /// <returns>The URI prefix</returns>
        internal static string ToBaseUriPrefix(string baseUri)
        {
            if (baseUri != null)
            {
                var lastSlashIndex = baseUri.LastIndexOf('/');
                if (lastSlashIndex >= 0)
                {
                    return baseUri.Substring(0, lastSlashIndex);
                }
            }

            return string.Empty;
        }
    }
}
