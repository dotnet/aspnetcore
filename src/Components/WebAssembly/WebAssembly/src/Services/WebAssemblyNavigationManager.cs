// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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

        public WebAssemblyLazyLoadDefinition LazyLoadDefinition { get; set;  }

        public WebAssemblyNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        public async Task SetLocation(string uri, bool isInterceptedLink)
        {
            Uri = uri;
            await BeforeLocationChangeAsync();
            NotifyLocationChanged(isInterceptedLink);
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

        public async Task BeforeLocationChangeAsync()
        {
            if (LazyLoadDefinition == null) {
                return;
            }

            var path = Uri.Replace(BaseUri, "");
            var assembliesToLoad = LazyLoadDefinition.GetLazyAssembliesForRoute(path);

            if (assembliesToLoad == null)
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

            foreach (byte[] assembly in assemblies)
            {
                Assembly.Load(assembly);
            }
        }
    }
}
