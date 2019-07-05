// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserUriHelperInterop;

namespace Microsoft.AspNetCore.Blazor.Services
{
    /// <summary>
    /// Default client-side implementation of <see cref="IUriHelper"/>.
    /// </summary>
    public class WebAssemblyUriHelper : UriHelperBase
    {
        /// <summary>
        /// Gets the instance of <see cref="WebAssemblyUriHelper"/>.
        /// </summary>
        public static readonly WebAssemblyUriHelper Instance = new WebAssemblyUriHelper();

        // For simplicity we force public consumption of the BrowserUriHelper through
        // a singleton. Only a single instance can be updated by the browser through
        // interop. We can construct instances for testing.
        internal WebAssemblyUriHelper()
        {
        }

        protected override void EnsureInitialized()
        {
            WebAssemblyJSRuntime.Instance.Invoke<object>(
                Interop.ListenForNavigationEvents,
                typeof(WebAssemblyUriHelper).Assembly.GetName().Name,
                nameof(NotifyLocationChanged));

            // As described in the comment block above, BrowserUriHelper is only for
            // client-side (Mono) use, so it's OK to rely on synchronicity here.
            var baseUri = WebAssemblyJSRuntime.Instance.Invoke<string>(Interop.GetBaseUri);
            var uri = WebAssemblyJSRuntime.Instance.Invoke<string>(Interop.GetLocationHref);
            InitializeState(uri, baseUri);
        }

        /// <inheritdoc />
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            WebAssemblyJSRuntime.Instance.Invoke<object>(Interop.NavigateTo, uri, forceLoad);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string newAbsoluteUri, bool isInterceptedLink)
        {
            Instance.SetAbsoluteUri(newAbsoluteUri);
            Instance.TriggerOnLocationChanged(isInterceptedLink);
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
