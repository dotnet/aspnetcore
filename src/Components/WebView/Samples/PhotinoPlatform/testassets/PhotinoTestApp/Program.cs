// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.Photino;
using Microsoft.Extensions.DependencyInjection;

namespace PhotinoTestApp;

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
            services: serviceCollection.BuildServiceProvider(),
            pathBase: "/subdir"); // The content in BasicTestApp assumes this

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            Console.Write(
                "Fatal exception" + Environment.NewLine +
                error.ExceptionObject.ToString() + Environment.NewLine);
        };

        mainWindow.RootComponents.Add<BasicTestApp.Index>("root");
        mainWindow.RootComponents.RegisterForJavaScript<BasicTestApp.DynamicallyAddedRootComponent>("my-dynamic-root-component");
        mainWindow.RootComponents.RegisterForJavaScript<BasicTestApp.JavaScriptRootComponentParameterTypes>(
            "component-with-many-parameters",
            javaScriptInitializer: "myJsRootComponentInitializers.testInitializer");

        mainWindow.Run();
    }
}
