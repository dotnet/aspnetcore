// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Components.WebView.Photino;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView;

class Program
{
    // Yes, this is a Program.Main() inside of a test project! This project is a regular xUnit.net test project, but
    // some of the WebViewManagerTests also launch this project as a regular executable to launch UI tests. To achieve
    // this, the CSPROJ has the <StartupObject> property set to indicate that _this_ is the Program.Main() to use
    // when launching as an executable.
    [STAThread]
    static void Main(string[] args)
    {
        Console.WriteLine($"Running in test mode!");

        Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"Current assembly: {typeof(Program).Assembly.Location}");
        var thisProgramDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);

        Console.WriteLine($"Old PATH: {Environment.GetEnvironmentVariable("PATH")}");
        Environment.SetEnvironmentVariable("PATH", Path.Combine(thisProgramDir, "runtimes", "win-x64", "native") + ";" + Environment.GetEnvironmentVariable("PATH"));
        Console.WriteLine($"New PATH: {Environment.GetEnvironmentVariable("PATH")}");

        var thisAppFiles = Directory.GetFiles(thisProgramDir, "*", SearchOption.AllDirectories).ToArray();
        Console.WriteLine($"Found {thisAppFiles.Length} files in this app:");
        foreach (var file in thisAppFiles)
        {
            Console.WriteLine($"\t{file}");
        }

        var hostPage = "wwwroot/webviewtesthost.html";

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlazorWebView();
        serviceCollection.AddSingleton<HttpClient>();

        Console.WriteLine($"Creating BlazorWindow...");
        BlazorWindow mainWindow = null;
        try
        {
            mainWindow = new BlazorWindow(
                title: "Hello, world!",
                hostPage: hostPage,
                services: serviceCollection.BuildServiceProvider(),
                pathBase: "/subdir"); // The content in BasicTestApp assumes this
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception {ex.GetType().FullName} while creating window: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine($"Hooking exception handler...");
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            Console.Write(
                "Fatal exception" + Environment.NewLine +
                error.ExceptionObject.ToString() + Environment.NewLine);
        };

        Console.WriteLine($"Setting up root components...");
        mainWindow.RootComponents.Add<Pages.TestPage>("root");

        Console.WriteLine($"Running window...");

        try
        {
            mainWindow.Run(isTestMode: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while running window: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
