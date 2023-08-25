// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView.Photino;

/// <summary>
/// A window containing a Blazor web view.
/// </summary>
public class BlazorWindow
{
    private readonly PhotinoWindow _window;
    private readonly PhotinoWebViewManager _manager;
    private readonly string _pathBase;

    /// <summary>
    /// Constructs an instance of <see cref="BlazorWindow"/>.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="hostPage">The path to the host page.</param>
    /// <param name="services">The service provider.</param>
    /// <param name="configureWindow">A callback that configures the window.</param>
    /// <param name="pathBase">The pathbase for the application. URLs will be resolved relative to this.</param>
    public BlazorWindow(
        string title,
        string hostPage,
        IServiceProvider services,
        Action<PhotinoWindow>? configureWindow = null,
        string? pathBase = null)
    {
        _window = new PhotinoWindow
        {
            Title = title,
            Width = 1600,
            Height = 1200,
            Left = 300,
            Top = 300,
        };
        _window.RegisterCustomSchemeHandler(PhotinoWebViewManager.BlazorAppScheme, HandleWebRequest);

        configureWindow?.Invoke(_window);

        // We assume the host page is always in the root of the content directory, because it's
        // unclear there's any other use case. We can add more options later if so.
        var contentRootDir = Path.GetDirectoryName(Path.GetFullPath(hostPage))!;
        var hostPageRelativePath = Path.GetRelativePath(contentRootDir, hostPage);
        var fileProvider = new PhysicalFileProvider(contentRootDir);

        var dispatcher = new PhotinoDispatcher(_window);
        var jsComponents = new JSComponentConfigurationStore();

        _pathBase = (pathBase ?? string.Empty);
        if (!_pathBase.EndsWith('/'))
        {
            _pathBase += "/";
        }
        var appBaseUri = new Uri(new Uri(PhotinoWebViewManager.AppBaseOrigin), _pathBase);

        _manager = new PhotinoWebViewManager(_window, services, dispatcher, appBaseUri, fileProvider, jsComponents, hostPageRelativePath);
        RootComponents = new BlazorWindowRootComponents(_manager, jsComponents);
    }

    /// <summary>
    /// Gets the underlying <see cref="PhotinoWindow"/>.
    /// </summary>
    public PhotinoWindow Photino => _window;

    /// <summary>
    /// Gets configuration for the root components in the window.
    /// </summary>
    public BlazorWindowRootComponents RootComponents { get; }

    private string? _latestControlDivValue;

    /// <summary>
    /// Shows the window and waits for it to be closed.
    /// </summary>
    public void Run(bool isTestMode = false)
    {
        const string NewControlDivValueMessage = "wvt:NewControlDivValue";
        var isWebViewReady = false;

        if (isTestMode)
        {
            Console.WriteLine($"RegisterWebMessageReceivedHandler...");
            _window.RegisterWebMessageReceivedHandler((s, msg) =>
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
        }

        if (isTestMode)
        {
            Console.WriteLine($"Navigating to: {_pathBase}");
        }
        _manager.Navigate(_pathBase);

        var testPassed = false;

        if (isTestMode)
        {
            _window.WindowCreated += (s, e) =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // 1. Wait for WebView ready
                        Console.WriteLine($"Waiting for WebView ready...");
                        var isWebViewReadyRetriesLeft = 20;
                        while (!isWebViewReady)
                        {
                            Console.WriteLine($"WebView not ready yet, waiting 1sec...");
                            await Task.Delay(1000);
                            isWebViewReadyRetriesLeft--;
                            if (isWebViewReadyRetriesLeft == 0)
                            {
                                Console.WriteLine($"WebView never became ready, failing the test...");
                                return;
                            }
                        }
                        Console.WriteLine($"WebView is ready!");

                        // 2. Check TestPage starting state
                        if (!await WaitForControlDiv(controlValueToWaitFor: "0"))
                        {
                            return;
                        }

                        // 3. Click a button
                        _window.SendWebMessage($"wvt:ClickButton:incrementButton");

                        // 4. Check TestPage is updated after button click
                        if (!await WaitForControlDiv(controlValueToWaitFor: "1"))
                        {
                            return;
                        }

                        // 5. If we get here, it all worked!
                        Console.WriteLine($"All tests passed!");
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
                        _window.Close();
                    }
                });
            };
        }

        // This line actually starts Photino and makes the window appear
        Console.WriteLine($"Starting Photino window...");
        _window.WaitForClose();

        if (isTestMode)
        {
            Console.WriteLine($"Test passed? {testPassed}");
        }
    }

    const int MaxWaitTimes = 30;
    const int WaitTimeInMS = 250;

    public async Task<bool> WaitForControlDiv(string controlValueToWaitFor)
    {

        for (var i = 0; i < MaxWaitTimes; i++)
        {
            // Tell WebView to report the current controlDiv value (this is inside the loop because
            // it's possible for this to execute before the WebView has finished processing previous
            // C#-generated events, such as WebView button clicks).
            Console.WriteLine($"Asking WebView for current controlDiv value...");
            _window.SendWebMessage($"wvt:GetControlDivValue");

            // And wait for the value to appear
            if (_latestControlDivValue == controlValueToWaitFor)
            {
                Console.WriteLine($"WebView reported the expected controlDiv value of {controlValueToWaitFor}!");
                return true;
            }
            Console.WriteLine($"Waiting for controlDiv to have value '{controlValueToWaitFor}', but it's still '{_latestControlDivValue}', so waiting {WaitTimeInMS}ms.");
            await Task.Delay(WaitTimeInMS);
        }

        Console.WriteLine($"Waited {MaxWaitTimes * WaitTimeInMS}ms but couldn't get controlDiv to have value '{controlValueToWaitFor}' (last value is '{_latestControlDivValue}').");
        return false;
    }

    private Stream HandleWebRequest(object sender, string scheme, string url, out string contentType)
        => _manager.HandleWebRequest(url, out contentType!)!;
}
