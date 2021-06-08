// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                mainWindow.Photino.OpenAlertWindow("Fatal exception", error.ExceptionObject.ToString());
            };

            mainWindow.AddRootComponent<MyComponent>("#app");

            mainWindow.Run();
        }

        class MyComponent : ComponentBase
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(0, "This is from Blazor");
            }
        }
    }
}
