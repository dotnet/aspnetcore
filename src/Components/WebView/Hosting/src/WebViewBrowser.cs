using System;
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
    public class WebViewBrowser
    {
        private readonly Dispatcher _dispatcher;
        private readonly JSRuntime _jsRuntime;
        private readonly WebViewRenderer _renderer;
        private readonly WebViewNavigationManager _navigationManager;
        private readonly WebViewHost _webViewHost;

        public WebViewBrowser(
            Dispatcher dispatcher,
            JSRuntime jsRuntime,
            WebViewRenderer renderer,
            NavigationManager navigationManager,
            WebViewHost webViewHost)
        {
            _dispatcher = dispatcher;
            _jsRuntime = jsRuntime;
            _renderer = renderer;
            _navigationManager = (WebViewNavigationManager)navigationManager;
            _webViewHost = webViewHost;
        }

        public Task BeginInvokeDotNet(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            try
            {
                return _dispatcher.InvokeAsync(() => DotNetDispatcher.BeginInvokeDotNet(
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

        public Task EndInvokeJS(long asyncHandle, bool succeeded, string arguments)
        {
            if (succeeded)
            {
                return _dispatcher.InvokeAsync(() => DotNetDispatcher.EndInvokeJS(_jsRuntime, arguments));
            }
            else
            {
                _webViewHost.NotifyUnhandledException(new InvalidOperationException(arguments));
                return Task.CompletedTask;
            }
        }

        public async Task DispatchBrowserEvent(string eventDescriptor, string eventArgs)
        {
            var webEventData = WebEventData.Parse(_renderer, eventDescriptor, eventArgs);

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

        public async ValueTask OnRenderCompleted(string errorMessageOrNull)
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

        public async ValueTask OnLocationChanged(string uri, bool intercepted)
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
