// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Components.WebView.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView;

/// <summary>
/// Represents the services that are scoped to a single page load. Grouping them like this
/// means we don't have to check that each of them are available individually.
///
/// This has roughly the same role as a circuit in Blazor Server. One key difference is that,
/// for web views, the IPC channel is outside the page context, whereas in Blazor Server,
/// the IPC channel is within the circuit.
/// </summary>
internal sealed class PageContext : IAsyncDisposable
{
    private readonly AsyncServiceScope _serviceScope;

    public WebViewNavigationManager NavigationManager { get; }
    public WebViewJSRuntime JSRuntime { get; }
    public WebViewRenderer Renderer { get; }
    public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;

    public PageContext(
        Dispatcher dispatcher,
        AsyncServiceScope serviceScope,
        IpcSender ipcSender,
        JSComponentConfigurationStore jsComponentsConfiguration,
        string baseUrl,
        string startUrl)
    {
        _serviceScope = serviceScope;

        NavigationManager = (WebViewNavigationManager)ServiceProvider.GetRequiredService<NavigationManager>();
        NavigationManager.AttachToWebView(ipcSender, baseUrl, startUrl);

        JSRuntime = (WebViewJSRuntime)ServiceProvider.GetRequiredService<IJSRuntime>();
        JSRuntime.AttachToWebView(ipcSender);

        var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
        var jsComponents = new JSComponentInterop(jsComponentsConfiguration);
        Renderer = new WebViewRenderer(ServiceProvider, dispatcher, ipcSender, loggerFactory, JSRuntime, jsComponents);

        var webViewScrollToLocationHash = (WebViewScrollToLocationHash)ServiceProvider.GetRequiredService<IScrollToLocationHash>();
        webViewScrollToLocationHash.AttachJSRuntime(JSRuntime);
    }

    public async ValueTask DisposeAsync()
    {
        // Prevent any further JS interop calls during component disposal, similar to
        // how server-side Blazor marks the circuit as permanently disconnected.
        // This ensures JSObjectReference.DisposeAsync() doesn't throw when the page navigates away.
        JSRuntime.MarkAsDisconnected();

        await Renderer.DisposeAsync();
        await _serviceScope.DisposeAsync();
    }
}
