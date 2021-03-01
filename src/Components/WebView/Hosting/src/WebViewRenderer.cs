using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebView
{
    internal class WebViewRenderer : Renderer
    {
        private readonly Dictionary<string, int> _componentIdBySelector = new();
        private readonly Dispatcher _dispatcher;
        private readonly IpcSender _ipcSender;

        public WebViewRenderer(
            IServiceProvider serviceProvider,
            Dispatcher dispatcher,
            IpcSender ipcSender,
            ILoggerFactory loggerFactory) :
            base(serviceProvider, loggerFactory)
        {
            _dispatcher = dispatcher;
            _ipcSender = ipcSender;
        }

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception)
        {
            _ipcSender.NotifyUnhandledException(exception);
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            return _ipcSender.ApplyRenderBatch(renderBatch);
        }

        public async Task AddRootComponentAsync(Type componentType, string selector, ParameterView parameters)
        {
            if (_componentIdBySelector.ContainsKey(selector))
            {
                throw new InvalidOperationException("A component is already associated with the given selector.");
            }

            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);

            _componentIdBySelector.Add(selector, componentId);
            _ipcSender.AttachToDocument(componentId, selector);

            await RenderRootComponentAsync(componentId, parameters);
        }

        public async Task RemoveRootComponentAsync(string selector)
        {
            if (!_componentIdBySelector.TryGetValue(selector, out var componentId))
            {
                throw new InvalidOperationException("Could not find a component Id associated with the given selector.");
            }

            // TODO: The renderer needs an API to do trigger the disposal of the component tree.
            await Task.CompletedTask;

            _ipcSender.DetachFromDocument(componentId);
        }
    }
}
