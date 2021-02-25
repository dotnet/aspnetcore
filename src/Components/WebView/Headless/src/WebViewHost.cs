using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
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
namespace Microsoft.AspNetCore.Components.WebView.Headless
{
    // These are all the messages .NET needs to know how to dispatch to JS
    public abstract class WebViewHost
    {
        private Queue<TaskCompletionSource> _pendingAcks = new Queue<TaskCompletionSource>();

        public Task ApplyRenderBatch(RenderBatch renderBatch)
        {
            var tcs = new TaskCompletionSource();
            _pendingAcks.Enqueue(tcs);
            ApplyRenderBatchCore(renderBatch);
            return tcs.Task;
        }

        public void RenderCompleted(string errorMessageOrNull)
        {
            if (errorMessageOrNull == null)
            {
                _pendingAcks.Dequeue().SetResult();
            }
        }

        public abstract void ApplyRenderBatchCore(RenderBatch renderBatch);

        // This is called externally by the WebView when the url changes from external sources
        // (For example a new URL is applied to the control)
        public abstract void UpdateLocation(string newUrl, bool intercepted);

        // This is called by the navigation manager and needs to be forwarded to the WebView
        // It might trigger the WebView to change the location of the URL and cause a LocationUpdated event.
        public abstract void Navigate(string uri, bool forceLoad);

        // Called from Renderer to attach a new component ID to a given selector.
        public abstract void AttachToDocument(int componentId, string selector);

        // Called from the WebView to detach a root component from the document.
        public abstract void DetachFromDocument(int componentId);

        // Interop calls emitted by the JSRuntime
        public abstract void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId);

        // TODO: We need to think about this, the invocation result contains the triplet [callId, successOrError, resultOrError]
        // serialized as JSON with the options provided by the JSRuntime. The host can't operate on the "unserialized"
        // data since it needs to deal with DotNetObjectReferences and JSObjectReference which the host doesn't have any visibility
        // over how to serialize or deserialize.
        // The strongest limitation we can find on a platform is that we might only be able to communicate with the host via "strings" (post-message)
        // and in that situation we can define a separator within the string like (callId,success,resultOrError) that the
        // side running in the browser can parse for processing.
        public abstract void EndInvokeDotNet(string callId, bool success, string invocationResultOrError);

        public abstract void NotifyUnhandledException(Exception exception);
    }

    // These are all the messages .NET Host needs to know how to receive from JS
    public class RemoteBrowser
    {
        private readonly Dispatcher _dispatcher;
        private readonly JSRuntime _jsRuntime;
        private readonly WebViewRenderer _renderer;
        private readonly WebViewHost _webViewHost;

        public RemoteBrowser(
            Dispatcher dispatcher,
            JSRuntime jsRuntime,
            WebViewRenderer renderer,
            WebViewHost webViewHost)
        {
            _dispatcher = dispatcher;
            _jsRuntime = jsRuntime;
            _renderer = renderer;
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
            }        }

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
                    _webViewHost.UpdateLocation(uri, intercepted);
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

    public class HeadlessWebViewHost : WebViewHost
    {
        private readonly Dispatcher _dispatcher;

        public event Action<RenderBatch> OnRenderBatch;

        // This is triggered when the browser updates the location as a result of the
        // user clicking a link or similar.
        public event Action<string, bool> OnUpdateLocation;

        // This is triggered when a component triggers a navigation programatically
        public event Action<string, bool> OnNavigate;

        // Triggered to attach a component to the document.
        public event Action<int, string> OnAttachComponent;

        // Triggered to detach a component from the document.
        public event Action<int> OnDetachComponent;

        // Triggered when an object on the document starts an interop call.
        public event Action<string, string, string, long, string> OnBeginInvokeDotNet;

        // Triggered when a component or service starts a JS interop call.
        public event Action<long, string, string, JSCallResultType, long> OnBeginInvokeJS;

        public HeadlessWebViewHost(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public override void ApplyRenderBatchCore(RenderBatch renderBatch)
        {
            OnRenderBatch(renderBatch);
        }

        public override void UpdateLocation(string newUrl, bool intercepted)
        {
            OnUpdateLocation(newUrl, intercepted);
        }

        public override void Navigate(string uri, bool forceLoad)
        {
            OnNavigate(uri, forceLoad);
        }

        public override void AttachToDocument(int componentId, string selector)
        {
            OnAttachComponent(componentId, selector);
        }

        public override void DetachFromDocument(int componentId)
        {
            OnDetachComponent(componentId);
        }

        public override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            throw new NotImplementedException();
        }

        public override void EndInvokeDotNet(string callId, bool success, string invocationResultOrError)
        {
            throw new NotImplementedException();
        }

        public override void NotifyUnhandledException(Exception exception)
        {
            throw new NotImplementedException();
        }

        //    public Task ApplyRenderBatch(RenderBatch renderBatch)
        //    {
        //        // The code in the browser also needs to be "single-threaded".
        //        return _dispatcher.InvokeAsync(() => OnRenderBatch?.Invoke(renderBatch));
        //    }

        //    public Task AttachToDocumentAsync(int componentId, string selector)
        //    {
        //        return _dispatcher.InvokeAsync(() => OnAttachComponent(componentId, selector));
        //    }

        //    public Task DetachFromDocumentAsync(int componentId)
        //    {
        //        return _dispatcher.InvokeAsync(() => OnDetachComponent(componentId));
        //    }

        //    public void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        //    {
        //        _dispatcher.InvokeAsync(() => OnBeginInvokeJS(taskId, identifier, argsJson, resultType, targetInstanceId));
        //    }

        //    public void EndInvokeDotNet(string callId, bool success, string invocationResultOrError)
        //    {
        //        _dispatcher.InvokeAsync(() => OnEndInvokeDotNet(callId, success, invocationResultOrError));
        //    }

        //    public ValueTask BeginInvokeDotNet(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        //    {
        //        return new ValueTask(_dispatcher.InvokeAsync(() => OnBeginInvokeDotNet(callId, assemblyName, methodIdentifier, dotNetObjectId, argsJson)));
        //    }

        //    public ValueTask DispatchBrowserEvent(string eventDescriptor, string eventArgs)
        //    {
        //        _dispatcher.InvokeAsync(() => OnDispatchBrowserEvent(eventDescriptor, eventArgs));
        //    }

        //    public ValueTask EndInvokeJS(long asyncHandle, bool succeeded, string arguments)
        //    {
        //        _dispatcher.InvokeAsync(() => On)
        //    }

        //    public void Navigate(string uri, bool forceLoad)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public void NotifyUnhandledException(Exception exception)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public ValueTask OnLocationChanged(string uri, bool intercepted)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public ValueTask OnRenderCompleted(long renderId, string errorMessageOrNull)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public void UpdateLocation(string newUrl, bool intercepted)
        //    {
        //        throw new NotImplementedException();
        //    }
    }
}
