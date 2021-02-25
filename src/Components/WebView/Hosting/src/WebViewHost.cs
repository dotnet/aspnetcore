using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
namespace Microsoft.AspNetCore.Components.WebView
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
}
