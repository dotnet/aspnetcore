// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.WebView.Photino;
using Microsoft.Extensions.DependencyInjection;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebViewE2E.Test;

public class BasicBlazorHybridTest
{
    private string _latestControlDivValue;

    public void Run()
    {
        // Note: This test produces *a lot* of debug output to aid when debugging failures. The only output
        // that is necessary for the functioning of this test is the "Test passed" at the end of this method.

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Current directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Current assembly: {typeof(Program).Assembly.Location}");
        var thisProgramDir = Path.GetDirectoryName(typeof(Program).Assembly.Location);

        // Add correct runtime sub-folder to PATH to ensure native files are discovered (this is supposed to happen automatically, but somehow it doesn't...)
        var newNativePath = Path.Combine(thisProgramDir, "runtimes", RuntimeInformation.RuntimeIdentifier, "native");
        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Adding new native path: {newNativePath}");
        Environment.SetEnvironmentVariable("PATH", newNativePath + ";" + Environment.GetEnvironmentVariable("PATH"));
        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] New PATH env var: {Environment.GetEnvironmentVariable("PATH")}");

        var thisAppFiles = Directory.GetFiles(thisProgramDir, "*", SearchOption.AllDirectories).ToArray();
        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Found {thisAppFiles.Length} files in this app:");
        foreach (var file in thisAppFiles)
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] \t{file}");
        }

        var hostPage = "wwwroot/webviewtesthost.html";

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlazorWebView();
        serviceCollection.AddSingleton<HttpClient>();

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Creating BlazorWindow...");
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
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Exception {ex.GetType().FullName} while creating window: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Hooking exception handler...");
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            Console.Write(
                "Fatal exception" + Environment.NewLine +
                error.ExceptionObject.ToString() + Environment.NewLine);
        };

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Setting up root components...");
        mainWindow.RootComponents.Add<Pages.TestPage>("root");

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Running window...");

        const string NewControlDivValueMessage = "wvt:NewControlDivValue";
        var isWebViewReady = false;

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] RegisterWebMessageReceivedHandler...");
        mainWindow.PhotinoWindow.RegisterWebMessageReceivedHandler((s, msg) =>
        {
            if (!msg.StartsWith("__bwv:", StringComparison.Ordinal))
            {
                if (msg == "wvt:Started")
                {
                    isWebViewReady = true;
                }
                else if (msg.StartsWith(NewControlDivValueMessage, StringComparison.Ordinal))
                {
                    _latestControlDivValue = msg.Substring(NewControlDivValueMessage.Length + 1);
                }
            }
        });
        var testPassed = false;

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Attaching WindowCreated handler...");
        mainWindow.PhotinoWindow.WindowCreated += (s, e) =>
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] In WindowCreated event...");
            Task.Run(async () =>
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] In async test task...");

                try
                {
                    // This is the actual test logic here (wait for WebView, click button, verify updates, etc.)

                    // 1. Wait for WebView ready
                    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Waiting for WebView ready...");
                    var isWebViewReadyRetriesLeft = 20;
                    while (!isWebViewReady)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] WebView not ready yet, waiting 1sec...");
                        await Task.Delay(1000);
                        isWebViewReadyRetriesLeft--;
                        if (isWebViewReadyRetriesLeft == 0)
                        {
                            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] WebView never became ready, failing the test...");
                            return;
                        }
                    }
                    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] WebView is ready!");

                    // 2. Check TestPage starting state
                    if (!await WaitForControlDiv(mainWindow.PhotinoWindow, controlValueToWaitFor: "0"))
                    {
                        return;
                    }

                    // 3. Click a button
                    mainWindow.PhotinoWindow.SendWebMessage($"wvt:ClickButton:incrementButton");

                    // 4. Check TestPage is updated after button click
                    if (!await WaitForControlDiv(mainWindow.PhotinoWindow, controlValueToWaitFor: "1"))
                    {
                        return;
                    }

                    // 5. If we get here, it all worked!
                    Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] All tests passed!");
                    testPassed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EXCEPTION DURING TEST: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
                finally
                {
                    // No matter what happens, close the Photino Window
                    mainWindow.PhotinoWindow.Close();
                }
            });
        };

        try
        {
            mainWindow.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Exception while running window: {ex}");
        }

        // This line is what's required for the test to be considered as passed. The xUnit test in WebViewManagerE2ETests checks
        // that this reports success and that decides if the test is pass/fail.
        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Test passed? {testPassed}");
    }

    const int MaxWaitTimes = 30;
    const int WaitTimeInMS = 1000;

    public async Task<bool> WaitForControlDiv(PhotinoWindow photinoWindow, string controlValueToWaitFor)
    {

        for (var i = 0; i < MaxWaitTimes; i++)
        {
            // Tell WebView to report the current controlDiv value (this is inside the loop because
            // it's possible for this to execute before the WebView has finished processing previous
            // C#-generated events, such as WebView button clicks).
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Asking WebView for current controlDiv value...");
            photinoWindow.SendWebMessage($"wvt:GetControlDivValue");

            // And wait for the value to appear
            if (_latestControlDivValue == controlValueToWaitFor)
            {
                Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] WebView reported the expected controlDiv value of {controlValueToWaitFor}!");
                return true;
            }
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Waiting for controlDiv to have value '{controlValueToWaitFor}', but it's still '{_latestControlDivValue}', so waiting {WaitTimeInMS}ms.");
            await Task.Delay(WaitTimeInMS);
        }

        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Waited {MaxWaitTimes * WaitTimeInMS}ms but couldn't get controlDiv to have value '{controlValueToWaitFor}' (last value is '{_latestControlDivValue}').");
        return false;
    }
}
