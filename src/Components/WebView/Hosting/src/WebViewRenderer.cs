using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebView
{
    public class WebViewRenderer : Renderer
    {
        private Dictionary<string, int> _componentIdBySelector = new();
        private Dispatcher _dispatcher;
        private WebViewHost _host;

        public WebViewRenderer(
            IServiceProvider serviceProvider,
            Dispatcher dispatcher,
            WebViewHost host,
            ILoggerFactory loggerFactory) :
            base(serviceProvider, loggerFactory)
        {
            _dispatcher = dispatcher;
            _host = host;
        }

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception)
        {
            _host.NotifyUnhandledException(exception);
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            return _host.ApplyRenderBatch(renderBatch);
        }

        public async Task RenderRootComponentAsync(Type componentType, string selector)
        {
            if (_componentIdBySelector.ContainsKey(selector))
            {
                throw new InvalidOperationException("A component is already associated with the given selector.");
            }

            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            _componentIdBySelector.Add(selector, componentId);
            _host.AttachToDocument(componentId, selector);

            await RenderRootComponentAsync(componentId);
        }

        public async Task RemoveRootComponentAsync(string selector)
        {
            if (!_componentIdBySelector.TryGetValue(selector, out var componentId))
            {
                throw new InvalidOperationException("Could not find a component Id associated with the given selector.");
            }

            // TODO: The renderer needs an API to do trigger the disposal of the component tree.
            await Task.CompletedTask;

            _host.DetachFromDocument(componentId);
        }
    }
}
