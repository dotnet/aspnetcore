using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
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
        private readonly Dispatcher _dispatcher;
        private readonly JSRuntime _jsRuntime;
        private readonly WebViewRenderer _renderer;
        private readonly WebViewNavigationManager _navigationManager;
        private readonly IpcSender _webViewHost;

        public IpcReceiver(
            Dispatcher dispatcher,
            IJSRuntime jsRuntime,
            WebViewRenderer renderer,
            NavigationManager navigationManager,
            IpcSender webViewHost)
        {
            _dispatcher = dispatcher;
            _jsRuntime = (JSRuntime)jsRuntime;
            _renderer = renderer;
            _navigationManager = (WebViewNavigationManager)navigationManager;
            _webViewHost = webViewHost;
        }

        // TODO: Proper error handling, reporting etc.
        public void OnMessageReceived(string message)
        {
            var (type, args) = Deserialize(message);
            switch (type)
            {
                case "Initialize":
                    break;
                case "BeginInvokeDotNet":
                    _ = BeginInvokeDotNet((string)args[0], (string)args[1], (string)args[2], (long)args[3], (string)args[4]);
                    break;
                case "EndInvokeJS":
                    EndInvokeJS((long)args[0], (bool)args[1], (string)args[2]);
                    break;
                case "DispatchBrowserEvent":
                    DispatchBrowserEvent((string)args[0], (string)args[1]);
                    break;
                case "OnRenderCompleted":
                    break;
                case "OnLocationChanged":
                    break;
                default:
                    throw new InvalidOperationException("Unknown message.");
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

        private async Task BeginInvokeDotNet(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            try
            {
                await _dispatcher.InvokeAsync(() => DotNetDispatcher.BeginInvokeDotNet(
                    _jsRuntime,
                    new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId),
                    argsJson));
            }
            catch (Exception ex)
            {
                _webViewHost.NotifyUnhandledException(ex);
                throw;
            }
        }

        private void EndInvokeJS(long asyncHandle, bool succeeded, string argumentsOrError)
        {
            if (succeeded)
            {
                // EndInvokeJS doesn't throw
                _ = _dispatcher.InvokeAsync(() => DotNetDispatcher.EndInvokeJS(_jsRuntime, argumentsOrError));
            }
            else
            {
                _webViewHost.NotifyUnhandledException(new InvalidOperationException(argumentsOrError));
            }
        }

        private void DispatchBrowserEvent(string eventDescriptor, string eventArgs)
        {
            WebEventData webEventData = null;
            try
            {
                webEventData = WebEventData.Parse(_renderer, eventDescriptor, eventArgs);
            }
            catch (Exception ex)
            {
                _webViewHost.NotifyUnhandledException(ex);
                throw;
            }
            _ = DispatchWithErrorHandling();

            async Task DispatchWithErrorHandling()
            {
                try
                {
                    await _dispatcher.InvokeAsync(() =>
                    {
                        return _renderer.DispatchEventAsync(
                            webEventData.EventHandlerId,
                            webEventData.EventFieldInfo,
                            webEventData.EventArgs);
                    });
                }
                catch (Exception ex)
                {
                    _webViewHost.NotifyUnhandledException(ex);
                }
            }
        }

        private async ValueTask OnRenderCompleted(string errorMessageOrNull)
        {
            try
            {
                await _dispatcher.InvokeAsync(() => _webViewHost.RenderCompleted(errorMessageOrNull));
            }
            catch (Exception e)
            {
                _webViewHost.NotifyUnhandledException(e);
            }
        }

        private async ValueTask OnLocationChanged(string uri, bool intercepted)
        {
            try
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    _navigationManager.LocationUpdated(uri, intercepted);
                });
            }

            // It's up to the NavigationManager implementation to validate the URI.
            //
            // Note that it's also possible that setting the URI could cause a failure in code that listens
            // to NavigationManager.LocationChanged.
            //
            // In either case, a well-behaved client will not send invalid URIs, and we don't really
            // want to continue processing with the circuit if setting the URI failed inside application
            // code. The safest thing to do is consider it a critical failure since URI is global state,
            // and a failure means that an update to global state was partially applied.
            catch (LocationChangeException nex)
            {
                _webViewHost.NotifyUnhandledException(nex);
            }
            catch (Exception ex)
            {
                _webViewHost.NotifyUnhandledException(ex);
            }
        }
    }
}
