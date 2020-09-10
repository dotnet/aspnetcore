// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    /// <summary>
    /// Default client-side implementation of <see cref="NavigationManager"/>.
    /// </summary>
    internal class WebAssemblyNavigationManager : NavigationManager
    {
        /// <summary>
        /// Gets the instance of <see cref="WebAssemblyNavigationManager"/>.
        /// </summary>
        public static WebAssemblyNavigationManager Instance { get; set; } = default!;

        public WebAssemblyNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        public void SetLocation(string uri, bool isInterceptedLink)
        {
            Uri = uri;
            NotifyLocationChanged(isInterceptedLink);
        }

        /// <inheritdoc />
        protected override void NavigateToCore(string uri, bool forceLoad, bool replace)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.NavigateTo, uri, forceLoad, replace);
        }
    }
}
