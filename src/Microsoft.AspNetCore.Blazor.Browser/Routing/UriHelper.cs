// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Routing
{
    // TODO: Make this not static, and wrap it in an interface that can be injected through DI.
    // We can make EnableNavigationInteception private, and call it automatically when the any
    // concrete instance is instantiated.

    /// <summary>
    /// Helpers for working with URIs and navigation state.
    /// </summary>
    public static class UriHelper
    {
        static readonly string _functionPrefix = typeof(UriHelper).FullName;

        /// <summary>
        /// An event that fires when the navigation location has changed.
        /// </summary>
        public static event EventHandler<string> OnLocationChanged;

        /// <summary>
        /// Prevents default navigation on all links whose href is inside the base URI space,
        /// causing clicks on those links to trigger <see cref="OnLocationChanged"/> instead.
        /// </summary>
        public static void EnableNavigationInteception()
            => RegisteredFunction.InvokeUnmarshalled<object>(
                $"{_functionPrefix}.enableNavigationInteception");

        /// <summary>
        /// Gets the URI prefix that can be prepended before URI paths to produce an absolute URI.
        /// Typically this corresponds to the 'href' attribute on the document's &lt;base&gt; element.
        /// </summary>
        /// <returns>The URI prefix.</returns>
        public static string GetBaseUriPrefix()
        {
            var baseUri = RegisteredFunction.InvokeUnmarshalled<string>(
                $"{_functionPrefix}.getBaseURI");
            return ToBaseUriPrefix(baseUri);
        }

        /// <summary>
        /// Gets the browser's current absolute URI.
        /// </summary>
        /// <returns>The browser's current absolute URI.</returns>
        public static string GetAbsoluteUri()
        {
            return RegisteredFunction.InvokeUnmarshalled<string>(
                $"{_functionPrefix}.getLocationHref");
        }

        /// <summary>
        /// Given a base URI prefix (e.g., one previously returned by <see cref="GetBaseUriPrefix"/>),
        /// converts an absolute URI into one relative to the base URI prefix.
        /// </summary>
        /// <param name="baseUriPrefix">The base URI prefix (e.g., previously returned by <see cref="GetBaseUriPrefix"/>).</param>
        /// <param name="absoluteUri">An absolute URI that is within the space of the base URI prefix.</param>
        /// <returns>A relative URI path.</returns>
        public static string ToBaseRelativePath(string baseUriPrefix, string absoluteUri)
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
                // and from that we return "/something" (also stripping any querystring
                // and/or hash value)
                return absoluteUri.Substring(baseUriPrefix.Length);
            }

            throw new ArgumentException($"The URI '{absoluteUri}' is not contained by the base URI '{baseUriPrefix}'.");
        }

        private static void NotifyLocationChanged(string newAbsoluteUri)
            => OnLocationChanged?.Invoke(null, newAbsoluteUri);

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
