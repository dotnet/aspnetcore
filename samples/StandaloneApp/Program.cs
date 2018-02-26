// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;

namespace StandaloneApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var serviceProvider = new BrowserServiceProvider(configure =>
            {
                // Add any custom services here
            });

            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
}
