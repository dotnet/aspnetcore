using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop.Infrastructure;

// Sync vs Async APIs for this.
// This mainly depends on the underlying support for the browser. Assuming that there is no synchronous API
// communication is safer, since it's not guaranteed.
// In that scenario, some APIs need to expose the async nature of the communication. That happens when some
// component like the renderer needs to know the results of the operation. For example when updating the UI
// since more code needs to execute afterwards.
// In other cases like when we try to attach a component to the document, we don't necessarily need to do that
// since we only care about errors that might happen while attaching the component and the renderer doesn't
// necessarily need to know about those if we are terminating the component/host as a result.
// If we decide we need to expose the async nature of the communication channel, then we will need to keep track
// of all the message pairs/completions across the IPC channel.
namespace Microsoft.AspNetCore.Components.WebView
{
    // These are all the messages .NET Host needs to know how to receive from JS

    // This class is a "Proxy" or "front-controller" for the incoming messages from the Browser via the transport channel.
    // It receives messages on OnMessageReceived, interprets the payload and dispatches them to the appropriate method
    internal class IpcReceiver
    {
        private readonly WebViewManager _manager;

        public IpcReceiver(WebViewManager manager)
        {
            _manager = manager;
        }

        public async Task OnMessageReceivedAsync(PageContext pageContext, string message)
        {
            // TODO: Don't assume that all possible messages are for us. Have some kind of
            // magic prefix to distinguish from any other use of web messages unrelated to us.

            var (type, args) = Deserialize(message);

            if (string.Equals(type, "Initialize", StringComparison.Ordinal))
            {
                _manager.AttachToPage((string)args[0], (string)args[1]);
                return;
            }

            // For any other message, you have to have a page attached already
            if (pageContext == null)
            {
                // TODO: Should we just ignore these messages? Is there any way their delivery
                // might be delayed until after a page has detached?
                throw new InvalidOperationException("Cannot receive IPC messages when no page is attached");
            }

            switch (type)
            {
                case "BeginInvokeDotNet":
                    BeginInvokeDotNet(pageContext, (string)args[0], (string)args[1], (string)args[2], (long)args[3], (string)args[4]);
                    break;
                case "EndInvokeJS":
                    EndInvokeJS(pageContext, (long)args[0], (bool)args[1], (string)args[2]);
                    break;
                case "DispatchBrowserEvent":
                    await DispatchBrowserEventAsync(pageContext, (string)args[0], (string)args[1]);
                    break;
                case "OnRenderCompleted":
                    OnRenderCompleted(pageContext, (long)args[0], (string)args[1]);
                    break;
                case "OnLocationChanged":
                    OnLocationChanged(pageContext, (string)args[0], (bool)args[1]);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown message type '{type}'.");
            }
        }

        // Internal for test only
        static internal (string type, object[] args) Deserialize(string message)
        {
            var payload = message.Split(":");
            return payload[0] switch
            {
                "Initialize" => ("Initialize", new object[] { payload[1], payload[2] }),
                "BeginInvokeDotNet" => ("BeginInvokeDotNet", new object[] { payload[1], payload[2], payload[3], long.Parse(payload[4], CultureInfo.InvariantCulture), payload[5] }),
                "EndInvokeJS" => ("EndInvokeJS", new object[] { long.Parse(payload[1],CultureInfo.InvariantCulture), bool.Parse(payload[2]), payload[3] }),
                "DispatchBrowserEvent" => ("DispatchBrowserEvent", new object[] { payload[1], payload[2] }),
                "OnRenderCompleted" => ("OnRenderCompleted", new object[] { payload[1] }),
                "OnLocationChanged" => ("OnLocationChanged", new object[] { payload[1], bool.Parse(payload[2]) }),
                _ => throw new InvalidOperationException("Unknown message")
            };
        }

        private void BeginInvokeDotNet(PageContext pageContext, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            DotNetDispatcher.BeginInvokeDotNet(
                pageContext.JSRuntime,
                new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId),
                argsJson);
        }

        private void EndInvokeJS(PageContext pageContext, long asyncHandle, bool succeeded, string argumentsOrError)
        {
            if (succeeded)
            {
                DotNetDispatcher.EndInvokeJS(pageContext.JSRuntime, argumentsOrError);
            }
            else
            {
                throw new InvalidOperationException(argumentsOrError);
            }
        }

        private Task DispatchBrowserEventAsync(PageContext pageContext, string eventDescriptor, string eventArgs)
        {
            var renderer = pageContext.Renderer;
            var webEventData = WebEventData.Parse(renderer, eventDescriptor, eventArgs);
            return renderer.DispatchEventAsync(
                webEventData.EventHandlerId,
                webEventData.EventFieldInfo,
                webEventData.EventArgs);
        }

        private void OnRenderCompleted(PageContext pageContext, long batchId, string errorMessageOrNull)
        {
            if (errorMessageOrNull != null)
            {
                throw new InvalidOperationException(errorMessageOrNull);
            }

            pageContext.Renderer.NotifyRenderCompleted(batchId);
        }

        private void OnLocationChanged(PageContext pageContext, string uri, bool intercepted)
        {
            pageContext.NavigationManager.LocationUpdated(uri, intercepted);
        }
    }
}
