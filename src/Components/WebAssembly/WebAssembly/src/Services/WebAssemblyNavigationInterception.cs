// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    internal sealed class WebAssemblyNavigationInterception : INavigationInterception
    {
        public static readonly WebAssemblyNavigationInterception Instance = new WebAssemblyNavigationInterception();

        public Task EnableNavigationInterceptionAsync()
        {
            DefaultWebAssemblyJSRuntime.Instance.InvokeVoid(Interop.EnableNavigationInterception);
            return Task.CompletedTask;
        }
    }
}
