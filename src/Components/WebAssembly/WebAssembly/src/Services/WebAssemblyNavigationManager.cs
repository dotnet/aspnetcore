// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
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

        public Task SetLocation(string uri, bool isInterceptedLink)
        {
            Uri = uri;
            return NotifyLocationChanged(isInterceptedLink);
        }

        /// <inheritdoc />
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            DefaultWebAssemblyJSRuntime.Instance.Invoke<object>(Interop.NavigateTo, uri, forceLoad);
        }

        public override async Task BeforeLocationChangeAsync()
        {
            if (OnNavigate == null) {
                return;
            }

            var assembliesToLoad = OnNavigate(Uri);

            if (assembliesToLoad.Count == 0)
            {
                return;
            }

            var count = (int)await DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<string[], object, object, Task<object>>(
                "window.Blazor._internal.getDynamicAssemblies",
                assembliesToLoad.ToArray(),
                null,
                null);

            if (count == 0)
            {
                return;
            }

            var assemblies = DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<object, object, object, object[]>(
                "window.Blazor._internal.readDynamicAssemblies",
                null,
                null,
                null);

            for (var i = 0; i < assemblies.Length; i++)
            {
                Assembly.Load((byte[])assemblies[i]);
            }
        }
    }
}
