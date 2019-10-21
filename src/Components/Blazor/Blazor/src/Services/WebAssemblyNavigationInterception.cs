// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Blazor.Services
{
    internal sealed class WebAssemblyNavigationInterception : INavigationInterception
    {
        public static readonly WebAssemblyNavigationInterception Instance = new WebAssemblyNavigationInterception();

        public Task EnableNavigationInterceptionAsync()
        {
            WebAssemblyJSRuntime.Instance.Invoke<object>(Interop.EnableNavigationInterception);
            return Task.CompletedTask;
        }
    }
}
