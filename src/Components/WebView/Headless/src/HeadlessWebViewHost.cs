using System;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.JSInterop;

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
    public class HeadlessWebViewHost : WebViewHost
    {
        public event Action<RenderBatch> OnRenderBatch;

        // This is triggered when the browser updates the location as a result of the
        // user clicking a link or similar.
        // public event Action<string, bool> OnUpdateLocation; // Re-enable once used

        // This is triggered when a component triggers a navigation programatically
        public event Action<string, bool> OnNavigate;

        // Triggered to attach a component to the document.
        public event Action<int, string> OnAttachComponent;

        // Triggered to detach a component from the document.
        public event Action<int> OnDetachComponent;

        // Triggered when an object on the document starts an interop call.
        public event Action<string, bool, string> OnEndInvokeDotNet;

        // Triggered when a component or service starts a JS interop call.
        public event Action<long, string, string, JSCallResultType, long> OnBeginInvokeJS;

        public event Action<Exception> OnUnhandledException;

        public override void ApplyRenderBatchCore(RenderBatch renderBatch)
        {
            OnRenderBatch(renderBatch);
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
            OnBeginInvokeJS(taskId, identifier, argsJson, resultType, targetInstanceId);
        }

        public override void EndInvokeDotNet(string callId, bool success, string invocationResultOrError)
        {
            OnEndInvokeDotNet(callId, success, invocationResultOrError);
        }

        public override void NotifyUnhandledException(Exception exception)
        {
            OnUnhandledException(exception);
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
