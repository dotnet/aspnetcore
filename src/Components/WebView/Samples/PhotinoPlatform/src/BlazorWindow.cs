// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView.Photino;

/// <summary>
/// A window containing a Blazor web view.
/// </summary>
public class BlazorWindow : IDisposable
{
    private readonly PhotinoWebViewManager _manager;
    private readonly PhysicalFileProvider _fileProvider;
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
        PhotinoWindow = new PhotinoWindow
        {
            Title = title,
            Width = 1600,
            Height = 1200,
            Left = 300,
            Top = 300,
        };
        PhotinoWindow.RegisterCustomSchemeHandler(PhotinoWebViewManager.BlazorAppScheme, HandleWebRequest);

        configureWindow?.Invoke(PhotinoWindow);

        var contentRootDir = Path.GetDirectoryName(Path.GetFullPath(hostPage))!;
        var hostPageRelativePath = Path.GetRelativePath(contentRootDir, hostPage);
        _fileProvider = new PhysicalFileProvider(contentRootDir);

        var dispatcher = new PhotinoDispatcher(PhotinoWindow);
        var jsComponents = new JSComponentConfigurationStore();

        _pathBase = (pathBase ?? string.Empty);
        if (!_pathBase.EndsWith('/'))
        {
            _pathBase += "/";
        }
        var appBaseUri = new Uri(new Uri(PhotinoWebViewManager.AppBaseOrigin), _pathBase);

        _manager = new PhotinoWebViewManager(PhotinoWindow, services, dispatcher, appBaseUri, _fileProvider, jsComponents, hostPageRelativePath);
        RootComponents = new BlazorWindowRootComponents(_manager, jsComponents);
    }

    /// <summary>
    /// Gets the underlying <see cref="PhotinoNET.PhotinoWindow"/>.
    /// </summary>
    public PhotinoWindow PhotinoWindow { get; }

    /// <summary>
    /// Gets configuration for the root components in the window.
    /// </summary>
    public BlazorWindowRootComponents RootComponents { get; }

    /// <summary>
    /// Shows the window and waits for it to be closed.
    /// </summary>
    public void Run()
    {
        _manager.Navigate(_pathBase);

        Console.WriteLine($"Starting Photino window...");
        PhotinoWindow.WaitForClose();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="BlazorWindow"/>.
    /// </summary>
    public void Dispose()
    {
        _manager.Dispose();
        _fileProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private Stream HandleWebRequest(object sender, string scheme, string url, out string contentType)
        => _manager.HandleWebRequest(url, out contentType!)!;
}
