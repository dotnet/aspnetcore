// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Http;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.JSInterop;
using System;

namespace BasicTestApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Needed because the test server runs on a different port than the client app,
            // and we want to test sending/receiving cookies undering this config
            BrowserHttpMessageHandler.DefaultCredentials = FetchCredentialsOption.Include;

            // Signal to tests that we're ready
            GC.KeepAlive(ActivateMonoJSRuntime.EnsureActivated());
            JSRuntime.Current.InvokeAsync<object>("testReady");
        }

        [JSInvokable(nameof(MountTestComponent))]
        public static void MountTestComponent(string componentTypeName)
        {
            var componentType = Type.GetType(componentTypeName);
            new BrowserRenderer().AddComponent(componentType, "app");
        }
    }
}
