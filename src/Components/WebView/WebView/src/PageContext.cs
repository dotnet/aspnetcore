// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
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
    internal class PageContext : IAsyncDisposable
    {
        private readonly AsyncServiceScope _serviceScope;
        private readonly DotNetObjectReference<PageContext> _selfReference;

        public WebViewNavigationManager NavigationManager { get; }
        public WebViewJSRuntime JSRuntime { get; }
        public WebViewRenderer Renderer { get; }

        public PageContext(
            Dispatcher dispatcher,
            AsyncServiceScope serviceScope,
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
            Renderer = new WebViewRenderer(services, dispatcher, ipcSender, loggerFactory, JSRuntime.ElementReferenceContext);

            // We need to dispatch events via JS interop so that all the special data types (byte arrays,
            // DotNetObjectReference, etc.) can be passed as eventargs data. So, register an object that
            // the JS side can use to call back with event data.
            _selfReference = DotNetObjectReference.Create(this);
            _ = JSRuntime.InvokeVoidAsync("Blazor._internal.attachEventDispatcher", _selfReference);
        }

        public ValueTask DisposeAsync()
        {
            Renderer.Dispose();
            _selfReference.Dispose();
            return _serviceScope.DisposeAsync();
        }

        [JSInvokable]
        public Task DispatchEventAsync(JsonElement eventDescriptor, JsonElement eventArgs)
        {
            var jsonOptions = JSRuntime.ReadJsonSerializerOptions();
            var webEventData = WebEventData.Parse(Renderer, jsonOptions, eventDescriptor, eventArgs);
            return Renderer.DispatchEventAsync(
                webEventData.EventHandlerId,
                webEventData.EventFieldInfo,
                webEventData.EventArgs);
        }
    }
}
