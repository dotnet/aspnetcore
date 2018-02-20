// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Routing
{
    public static class UriHelper
    {
        static readonly string _functionPrefix = typeof(UriHelper).FullName;

        public static event EventHandler<string> OnLocationChanged;

        public static void EnableNavigationInteception()
            => RegisteredFunction.InvokeUnmarshalled<object>(
                $"{_functionPrefix}.enableNavigationInteception");

        public static string GetBaseUriPrefix()
        {
            var baseUri = RegisteredFunction.InvokeUnmarshalled<string>(
                $"{_functionPrefix}.getBaseURI");
            return ToBaseURIPrefix(baseUri);
        }

        public static string GetAbsoluteUri()
        {
            return RegisteredFunction.InvokeUnmarshalled<string>(
                $"{_functionPrefix}.getLocationHref");
        }

        public static string ToBaseRelativePath(string baseUriPrefix, string absoluteUri)
        {
            // The absolute URI must be of the form "{baseUriPrefix}/something",
            // and from that we return "/something" (also stripping any querystring
            // and/or hash value)
            if (absoluteUri.StartsWith(baseUriPrefix, StringComparison.Ordinal)
                && absoluteUri.Length > baseUriPrefix.Length
                && absoluteUri[baseUriPrefix.Length] == '/')
            {
                // TODO: Remove querystring and/or hash
                return absoluteUri.Substring(baseUriPrefix.Length);
            }

            throw new ArgumentException($"The URI '{absoluteUri}' is not contained by the base URI '{baseUriPrefix}'.");
        }

        private static void NotifyLocationChanged(string newAbsoluteUri)
            => OnLocationChanged?.Invoke(null, newAbsoluteUri);

        /// <summary>
        /// Given the href value from the document's <base> element, returns the URI
        /// prefix that can be prepended to URI paths to produce an absolute URI.
        /// This is computed by removing the final slash and any following characters.
        /// </summary>
        /// <param name="baseUri">The href value from a document's <base> element.</param>
        /// <returns>The URI prefix</returns>
        private static string ToBaseURIPrefix(string baseUri)
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
