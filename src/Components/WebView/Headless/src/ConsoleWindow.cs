using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.WebView.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    public class ConsoleWindow : IRenderPort
    {
        private Dictionary<long, string> _rootComponents = new();
        private IServiceProvider _scope;
        private IContentProvider _contentProvider;
        private ConsoleRenderer _renderer;
        private IJSRuntime _jsRuntime;

        public void Attach(IServiceProvider scope)
        {
            _scope = scope;
            _contentProvider = _scope.GetRequiredService<IContentProvider>();
            _renderer = ActivatorUtilities.CreateInstance<ConsoleRenderer>(_scope, this);
            _jsRuntime = new ConsoleJsRuntime();
        }

        public void AddComponent<TComponent>(string selector)
        {
            _renderer.Dispatcher.InvokeAsync(() =>
            {
                _renderer.AddComponent<TComponent>(selector);
            });
        }

        public Task ApplyBatchAsync(RenderBatch renderBatch)
        {
            throw new NotImplementedException();
        }

        public void OnException(Exception exception)
        {
            Console.WriteLine(exception.ToString());
        }

        public void AttachRootComponent(int componentId, string selector)
        {
            _rootComponents.Add(componentId, selector);
        }

        public Task DispatchEventAsync(WebEventDescriptor descriptor, string eventArguments)
        {
            throw new InvalidOperationException();
        }
    }

    public interface MessageChannels
    {
        public void Attach();

        // Rendering
        // (Outbound)
        public Task ApplyRenderBatch(RenderBatch renderBatch);

        // (Inbound)
        public Task RenderBatchCompleted(long batchId, string renderErrors);

        // Event handling
        // (Inbound)
        public Task DispatchEventAsync(WebEventDescriptor descriptor, string eventArguments);

        // Error handling
        // (Outbound)
        public Task OnError(Exception ex);

        // .NET interop (this is typically handled via JS interop, should it be its own thing?)
        // (Inbound)
        public void BeginInvokeDotNetFromJS(string assembly, string methodIdentifier, int dotnetObjectId, string argsJson);
        // (Outbound)
        public void EndInvokeDotNetFromJS(int callbackId, bool success, string resultOrError);

        // JS interop
        // (Outbound)
        public void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId);
        // (Inbound)
        public void EndInvokeJSFromDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult);
    }
}
