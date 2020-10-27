// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
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
        public static WebAssemblyNavigationManager Instance { get; set; }

        public WebAssemblyNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        public void SetLocation(string uri, bool isInterceptedLink)
        {
            Uri = uri;
            NotifyLocationChanged(isInterceptedLink);
        }

        public ValueTask<bool> HandleLocationChanging(string uri, bool isInterceptedLink, bool forceLoad)
        {
            return NotifyLocationChanging(uri , isInterceptedLink, forceLoad);
        }

        /// <inheritdoc />
        protected override async void NavigateToCore(string uri, bool forceLoad)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (!await NotifyLocationChanging(uri, false, forceLoad))
            {
                DefaultWebAssemblyJSRuntime.Instance.Invoke<object>(Interop.NavigateTo, uri, forceLoad);
            }
        }

        /// <inheritdoc />
        protected override bool SetHasLocationChangingEventHandlers(bool value)
        {
            DefaultWebAssemblyJSRuntime.Instance.Invoke<object>(Interop.SetHasLocationChangingListeners, value);
            return true;
        }
    }
}
