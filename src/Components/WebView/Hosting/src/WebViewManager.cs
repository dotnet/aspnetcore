using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


namespace Microsoft.AspNetCore.Components.WebView
{
    public abstract class WebViewManager
    {
        private bool _started;
        private readonly IServiceProvider _provider;
        private readonly List<RootComponent> _registeredComponents = new();
        private IServiceScope _scope;

        private Dispatcher _dispatcher;
        private WebViewRenderer _renderer;
        private WebViewBrowser _webViewBrowser;
        private WebViewHost _webViewHost;
        private Queue<Task> _componentChangeTasks = new();

        public WebViewManager(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected IServiceProvider Provider => _provider;

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
            webViewNavigationManager.Init(BaseUrl, CurrentUrl);

            _dispatcher = services.GetService<Dispatcher>();
            _renderer = services.GetRequiredService<WebViewRenderer>();
            _webViewBrowser = services.GetRequiredService<WebViewBrowser>();
            _webViewHost = services.GetRequiredService<WebViewHost>();
            _webViewHost.MessageDispatcher = SendMessage;

            _started = true;
        }

        protected void MessageReceived(string message) => _webViewBrowser.OnMessageReceived(message);

        protected abstract void SendMessage(string message);

        public void Dispose()
        {
            _scope.Dispose();
        }

        private record RootComponent(Type ComponentType, string Selector);
    }
}
