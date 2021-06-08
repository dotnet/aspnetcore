// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.WebView.Photino;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace PhotinoTestApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();

            var mainWindow = new BlazorWindow(
                title: "Hello, world!",
                hostPage: "wwwroot/index.html",
                services: serviceCollection.BuildServiceProvider());

            mainWindow.Run();
        }
    }
}
