using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebView.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    internal class ConsoleRenderer : Renderer
    {
        private Dispatcher _dispatcher;
        private IRenderPort _renderPort;

        public ConsoleRenderer(
            IServiceProvider serviceProvider,
            IRenderPort renderPort,
            ILoggerFactory loggerFactory,
            IComponentActivator componentActivator,
            Dispatcher dispatcher) : base(serviceProvider, loggerFactory, componentActivator)
        {
            _dispatcher = dispatcher;
            _renderPort = renderPort;
        }

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception)
        {
            _renderPort.OnException(exception);
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            return _renderPort.ApplyBatchAsync(renderBatch);
        }

        public Task AddComponent<TComponent>(string selector, in ParameterView initialParameters = default)
        {
            var component = InstantiateComponent(typeof(TComponent));
            var componentId = AssignRootComponentId(component);

            _renderPort.AttachRootComponent(componentId, selector);

            return RenderRootComponentAsync(componentId, initialParameters);
        }
    }
}
