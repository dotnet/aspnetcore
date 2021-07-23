// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebView.Photino;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace PhotinoTestApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddSingleton<HttpClient>();

            var mainWindow = new BlazorWindow(
                title: "Hello, world!",
                hostPage: "wwwroot/webviewhost.html",
                services: serviceCollection.BuildServiceProvider());

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                mainWindow.Photino.OpenAlertWindow("Fatal exception", error.ExceptionObject.ToString());
            };

            mainWindow.AddRootComponent<BasicTestApp.Index>("root");

            mainWindow.Run();
        }
    }
}
