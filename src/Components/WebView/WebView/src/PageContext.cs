using System;
using Microsoft.AspNetCore.Components.WebView.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView
{
    /// <summary>
    /// Represents the services that are scoped to a single page load. Grouping them like this
    /// means we don't have to check that each of them are available individually.
    ///
    /// This has roughly the same role as a circuit in Blazor Server. One key difference is that,
    /// for web views, the IPC channel is outside the page context, whereas in Blazor Server,
    /// the IPC channel is within the circuit.
    /// </summary>
    class PageContext : IDisposable
    {
        private readonly IServiceScope _serviceScope;

        public WebViewNavigationManager NavigationManager { get; }
        public WebViewJSRuntime JSRuntime { get; }
        public WebViewRenderer Renderer { get; }

        public PageContext(
            Dispatcher dispatcher,
            IServiceScope serviceScope,
            IpcSender ipcSender,
            string baseUrl,
            string startUrl)
        {
            _serviceScope = serviceScope;
            var services = serviceScope.ServiceProvider;

            NavigationManager = (WebViewNavigationManager)services.GetRequiredService<NavigationManager>();
            NavigationManager.AttachToWebView(ipcSender, baseUrl, startUrl);

            JSRuntime = (WebViewJSRuntime)services.GetRequiredService<IJSRuntime>();
            JSRuntime.AttachToWebView(ipcSender);

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            Renderer = new WebViewRenderer(services, dispatcher, ipcSender, loggerFactory);
        }

        public void Dispose()
        {
            Renderer.Dispose();
            _serviceScope.Dispose();
        }
    }
}
