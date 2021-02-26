using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


namespace Microsoft.AspNetCore.Components.WebView
{
    // This class is a coordinator for a given WebView.
    // Handles the setup of the specific webview interop transport bus, and other browser concerns like
    // handling events for serving content, dealing with page refreshes, browser crashes, etc.
    public abstract class WebViewManager
    {
        private bool _started;
        private readonly IServiceProvider _provider;
        private readonly List<RootComponent> _registeredComponents = new();
        private IServiceScope _scope;

        private Dispatcher _dispatcher;
        private WebViewRenderer _renderer;
        private WebViewBrowserProxy _webViewBrowser;
        private WebViewClient _webViewHost;
        private Queue<Task> _componentChangeTasks = new();

        public WebViewManager(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected IServiceProvider Provider => _provider;

        protected string BaseUrl { get; set; }

        protected string StartUrl { get; set; }

        // This API is synchronous because bindings are synchronous in XAML, so we'll have to deal
        // with errors separately.
        public void AddComponent(Type componentType, string selector, ParameterView parameters)
        {
            if (!_started)
            {
                throw new InvalidOperationException("Not initialized.");
            }

            _registeredComponents.Add(new RootComponent(componentType, selector));
            _componentChangeTasks.Enqueue(RenderRootComponent(selector));

            async Task RenderRootComponent(string selector)
            {
                await _dispatcher.InvokeAsync(async () =>
                {
                    await _renderer.AddRootComponentAsync(componentType, selector, parameters);
                });
            }
        }

        public void RemoveRootComponent(string selector)
        {
            for (var i = 0; i < _registeredComponents.Count; i++)
            {
                var registration = _registeredComponents[i];
                if (registration.Selector == selector)
                {
                    _registeredComponents.RemoveAt(i);
                    _componentChangeTasks.Enqueue(RemoveRootComponent(selector));
                    async Task RemoveRootComponent(string selector)
                    {
                        await _dispatcher.InvokeAsync(async () =>
                        {
                            await _renderer.RemoveRootComponentAsync(selector);
                        });
                    }
                }
            }
        }

        public virtual void Start()
        {
            if (_started)
            {
                throw new InvalidOperationException("Already initialized.");
            }

            _scope = Provider.CreateScope();
            var services = _scope.ServiceProvider;
            var webViewNavigationManager = (WebViewNavigationManager)services.GetRequiredService<NavigationManager>();
            webViewNavigationManager.Init(BaseUrl, StartUrl);

            _dispatcher = services.GetService<Dispatcher>();
            _renderer = services.GetRequiredService<WebViewRenderer>();
            _webViewBrowser = services.GetRequiredService<WebViewBrowserProxy>();
            _webViewHost = services.GetRequiredService<WebViewClient>();
            _webViewHost.MessageDispatcher = SendMessage;

            _started = true;
        }

        protected void MessageReceived(string message) => _webViewBrowser.OnMessageReceived(message);

        protected abstract void SendMessage(string message);

        private record RootComponent(Type ComponentType, string Selector);
    }
}
