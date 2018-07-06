// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.JSInterop;
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
        const string _functionPrefix = "Blazor._internal.uriHelper.";
        static bool _hasEnabledNavigationInterception;
        static string _cachedAbsoluteUri;
        static EventHandler<string> _onLocationChanged;

        // These two are always kept in sync. We store both representations to
        // avoid having to convert between them on demand.
        static Uri _baseUriWithTrailingSlash;
        static string _baseUriStringWithTrailingSlash;

        /// <inheritdoc />
        public event EventHandler<string> OnLocationChanged
        {
            add
            {
                EnsureNavigationInterceptionEnabled();
                _onLocationChanged += value;
            }
            remove
            {
                // We could consider deactivating the JS-side enableNavigationInterception
                // if there are no remaining listeners, but we don't need that currently.
                // If we ever do that, will also need to change the logic inside GetAbsoluteUri
                // so it knows not to continue using the cached URI.
                _onLocationChanged -= value;
            }
        }

        /// <inheritdoc />
        public string GetBaseUri()
        {
            EnsureBaseUriPopulated();
            return _baseUriStringWithTrailingSlash;
        }

        /// <inheritdoc />
        public string GetAbsoluteUri()
        {
            if (_cachedAbsoluteUri == null)
            {
                // BrowserUriHelper is only intended for client-side (Mono) use, so it's OK
                // to rely on synchrony here. When we come to implement IUriHelper for
                // out-of-process cases, we can't use all the statics either, so this whole
                // service needs to be rebuilt. It will most likely require you to supply
                // the current URL and base href as constructor parameters so it has that
                // info synchronously.
                var newUri = ((IJSInProcessRuntime)JSRuntime.Current)
                    .Invoke<string>(_functionPrefix + "getLocationHref");

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
        public string ToBaseRelativePath(string baseUri, string absoluteUri)
        {
            if (absoluteUri.StartsWith(baseUri, StringComparison.Ordinal))
            {
                // The absolute URI must be of the form "{baseUri}something" (where
                // baseUri ends with a slash), and from that we return "something"
                return absoluteUri.Substring(baseUri.Length);
            } else if ($"{absoluteUri}/".Equals(baseUri, StringComparison.Ordinal))
            {
                // Special case: for the base URI "/something/", if you're at
                // "/something" then treat it as if you were at "/something/" (i.e.,
                // with the trailing slash). It's a bit ambiguous because we don't know
                // whether the server would return the same page whether or not the
                // slash is present, but ASP.NET Core at least does by default when
                // using PathBase.
                return string.Empty;
            }

            throw new ArgumentException($"The URI '{absoluteUri}' is not contained by the base URI '{baseUri}'.");
        }

        /// <inheritdoc />
        public void NavigateTo(string uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            JSRuntime.Current.InvokeAsync<object>(_functionPrefix + "navigateTo", uri);
        }

        private static void EnsureBaseUriPopulated()
        {
            // The <base href> is fixed for the lifetime of the page, so just cache it
            if (_baseUriStringWithTrailingSlash == null)
            {
                // As described in other comment block above, BrowserUriHelper is only for
                // client -side (Mono) use, so it's OK to rely on synchrony here.
                var baseUriAbsolute = ((IJSInProcessRuntime)JSRuntime.Current)
                    .Invoke<string>(_functionPrefix + "getBaseURI");

                _baseUriStringWithTrailingSlash = ToBaseUri(baseUriAbsolute);
                _baseUriWithTrailingSlash = new Uri(_baseUriStringWithTrailingSlash);
            }
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string newAbsoluteUri)
        {
            _cachedAbsoluteUri = newAbsoluteUri;
            _onLocationChanged?.Invoke(null, newAbsoluteUri);
        }

        private static void EnsureNavigationInterceptionEnabled()
        {
            // Don't need thread safety because:
            // (1) there's only one UI thread
            // (2) doesn't matter if we call enableNavigationInterception more than once anyway
            if (!_hasEnabledNavigationInterception)
            {
                _hasEnabledNavigationInterception = true;
                JSRuntime.Current.InvokeAsync<object>(_functionPrefix + "enableNavigationInterception");
            }
        }

        /// <summary>
        /// Given the document's document.baseURI value, returns the URI
        /// that can be prepended to relative URI paths to produce an absolute URI.
        /// This is computed by removing anything after the final slash.
        /// Internal for tests.
        /// </summary>
        /// <param name="absoluteBaseUri">The page's document.baseURI value.</param>
        /// <returns>The URI prefix</returns>
        internal static string ToBaseUri(string absoluteBaseUri)
        {
            if (absoluteBaseUri != null)
            {
                var lastSlashIndex = absoluteBaseUri.LastIndexOf('/');
                if (lastSlashIndex >= 0)
                {
                    return absoluteBaseUri.Substring(0, lastSlashIndex + 1);
                }
            }

            return "/";
        }
    }
}
