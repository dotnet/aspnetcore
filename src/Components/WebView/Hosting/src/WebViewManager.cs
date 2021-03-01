using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView
{
    /// <summary>
    /// Manages activities within a web view that hosts Blazor components. Platform authors
    /// should subclass this to wire up the abstract and protected methods to the APIs of
    /// the platform's web view.
    /// </summary>
    public abstract class WebViewManager : IDisposable
    {
        // These services are not DI services, because their lifetime isn't limited to a single
        // per-page-load scope. Instead, their lifetime matches the webview itself.
        private readonly IServiceProvider _provider;
        private readonly Dispatcher _dispatcher;
        private readonly IpcSender _ipcSender;
        private readonly IpcReceiver _ipcReceiver;

        // Each time a web page connects, we establish a new per-page context
        private PageContext _currentPageContext;

        public WebViewManager(IServiceProvider provider, Dispatcher dispatcher)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _ipcSender = new IpcSender(_dispatcher, SendMessage);
            _ipcReceiver = new IpcReceiver(this);
        }

        /// <summary>
        /// Notifies listeners that a page is attached to the webview. Handlers for this event
        /// may add root components by calling <see cref="AddRootComponent(Type, string, ParameterView)"/>.
        /// </summary>
        public event EventHandler OnPageAttached;

        /// <summary>
        /// Instructs the web view to navigate to the specified URL, bypassing any
        /// client-side routing.
        /// </summary>
        /// <param name="url">The URL, which may be absolute or relative to the application root.</param>
        public abstract void Navigate(string url);

        /// <summary>
        /// Sends a message to JavaScript code running in the attached web view. This must
        /// be forwarded to the Blazor JavaScript code.
        /// </summary>
        /// <param name="message">The message.</param>
        protected abstract void SendMessage(string message);

        /// <summary>
        /// Adds a root component to the attached page.
        /// </summary>
        /// <param name="componentType">The type of the root component. This must implement <see cref="IComponent"/>.</param>
        /// <param name="selector">The CSS selector describing where in the page the component should be placed.</param>
        /// <param name="parameters">Parameters for the component.</param>
        public void AddRootComponent(Type componentType, string selector, ParameterView parameters)
        {
            _ = _dispatcher.InvokeAsync(async () =>
            {
                // TODO: Make sure any exceptions get surfaced

                if (_currentPageContext == null)
                {
                    throw new InvalidOperationException("Cannot add components when no page is attached");
                }

                await _currentPageContext.Renderer.AddRootComponentAsync(componentType, selector, parameters);
            });
        }

        /// <summary>
        /// Removes a previously-attached root component from the current page.
        /// </summary>
        /// <param name="selector">The CSS selector describing where in the page the component was placed. This must exactly match the selector provided on an earlier call to <see cref="AddRootComponent(Type, string, ParameterView)"/>.</param>
        public void RemoveRootComponent(string selector)
        {
            _ = _dispatcher.InvokeAsync(async () =>
            {
                // TODO: Make sure any exceptions get surfaced

                if (_currentPageContext == null)
                {
                    throw new InvalidOperationException("Cannot remove components when no page is attached");
                }

                await _currentPageContext.Renderer.RemoveRootComponentAsync(selector);
            });
        }

        /// <summary>
        /// Notifies the <see cref="WebViewManager"/> about a message from JavaScript running within the web view.
        /// </summary>
        /// <param name="message">The message.</param>
        protected void MessageReceived(string message)
        {
            _ = _dispatcher.InvokeAsync(async () =>
            {
                // TODO: Verify this produces the correct exception-surfacing behaviors.
                // For example, JS interop exceptions should flow back into JS, whereas
                // renderer exceptions should be fatal.
                try
                {
                    await _ipcReceiver.OnMessageReceivedAsync(_currentPageContext, message);
                }
                catch (Exception ex)
                {
                    _ipcSender.NotifyUnhandledException(ex);
                    throw;
                }
            });
        }

        internal void AttachToPage(string baseUrl, string startUrl)
        {
            // If there was some previous attached page, dispose all its resources. TODO: Are we happy
            // with this pattern? The alternative would be requiring the platform author to notify us
            // when the webview is navigating away so we could dispose more eagerly then.
            _currentPageContext?.Dispose();

            var serviceScope = _provider.CreateScope();
            _currentPageContext = new PageContext(_dispatcher, serviceScope, _ipcSender, baseUrl, startUrl);
            OnPageAttached?.Invoke(this, null);
        }

        /// <summary>
        /// Disposes the <see cref="WebViewManager"/>.
        /// </summary>
        public void Dispose()
        {
            _currentPageContext?.Dispose();
        }
    }
}
