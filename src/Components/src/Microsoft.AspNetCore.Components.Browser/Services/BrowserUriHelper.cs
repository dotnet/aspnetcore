// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Browser.Services.BrowserUriHelperInterop;

namespace Microsoft.AspNetCore.Components.Browser.Services
{
    /// <summary>
    /// Default browser implementation of <see cref="IUriHelper"/>.
    /// </summary>
    public class BrowserUriHelper : UriHelperBase
    {
        /// <summary>
        /// Gets the instance of <see cref="BrowserUriHelper"/>.
        /// </summary>
        public static readonly BrowserUriHelper Instance = new BrowserUriHelper();

        // For simplicity we force public consumption of the BrowserUriHelper through
        // a singleton. Only a single instance can be updated by the browser through
        // interop. We can construct instances for testing.
        internal BrowserUriHelper()
        {
        }

        /// <summary>
        /// Called to initialize BaseURI and current URI before those values the first time.
        /// Override this method to dynamically calculate the those values.
        /// </summary>
        protected override void InitializeState()
        {
            ((IJSInProcessRuntime)JSRuntime.Current).Invoke<object>(
                Interop.EnableNavigationInterception,
                typeof(BrowserUriHelper).Assembly.GetName().Name,
                nameof(NotifyLocationChanged));

            // As described in other comment block above, BrowserUriHelper is only for
            // client -side (Mono) use, so it's OK to rely on synchrony here.
            var baseUri = ((IJSInProcessRuntime)JSRuntime.Current).Invoke<string>(Interop.GetBaseUri);
            var uri = ((IJSInProcessRuntime)JSRuntime.Current).Invoke<string>(Interop.GetLocationHref);
            SetAbsoluteBaseUri(baseUri);
            SetAbsoluteUri(uri);

            Console.WriteLine("Initialized");
        }

        /// <inheritdoc />
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            ((IJSInProcessRuntime)JSRuntime.Current).Invoke<object>(Interop.NavigateTo, uri, forceLoad);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string newAbsoluteUri)
        {
            Instance.SetAbsoluteUri(newAbsoluteUri);
            Instance.TriggerOnLocationChanged();
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
