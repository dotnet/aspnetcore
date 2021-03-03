using System;
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
    // TODO: Proper serialization, error handling, etc.

    // Handles comunication between the component abstractions (Renderer, NavigationManager, JSInterop, etc.)
    // and the underlying transport channel
    internal class IpcSender
    {
        private readonly Dispatcher _dispatcher;
        private readonly Action<string> _messageDispatcher;

        public IpcSender(Dispatcher dispatcher, Action<string> messageDispatcher)
        {
            _dispatcher = dispatcher;
            _messageDispatcher = messageDispatcher;
        }

        public void ApplyRenderBatch(long batchId, RenderBatch renderBatch)
        {
            var arrayBuilder = new ArrayBuilder<byte>(2048);
            using var memoryStream = new ArrayBuilderMemoryStream(arrayBuilder);
            using (var renderBatchWriter = new RenderBatchWriter(memoryStream, false))
            {
                renderBatchWriter.Write(in renderBatch);
            }
            var message = IpcCommon.Serialize(IpcCommon.OutgoingMessageType.RenderBatch, batchId, Convert.ToBase64String(arrayBuilder.Buffer, 0, arrayBuilder.Count));
            DispatchMessageWithErrorHandling(message);
        }

        // This is called by the navigation manager and needs to be forwarded to the WebView
        // It might trigger the WebView to change the location of the URL and cause a LocationUpdated event.
        public void Navigate(string uri, bool forceLoad)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.Navigate, uri, forceLoad));
        }

        // TODO: Make these APIs async if we want the renderer to be able to deal with errors.
        // Called from Renderer to attach a new component ID to a given selector.
        public void AttachToDocument(int componentId, string selector)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.AttachToDocument, componentId, selector));
        }

        // Called from the WebView to detach a root component from the document.
        public void DetachFromDocument(int componentId)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.DetachFromDocument, componentId));
        }

        // Interop calls emitted by the JSRuntime
        public void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.DetachFromDocument, taskId, identifier, argsJson, resultType, targetInstanceId));
        }

        // TODO: We need to think about this, the invocation result contains the triplet [callId, successOrError, resultOrError]
        // serialized as JSON with the options provided by the JSRuntime. The host can't operate on the "unserialized"
        // data since it needs to deal with DotNetObjectReferences and JSObjectReference which the host doesn't have any visibility
        // over how to serialize or deserialize.
        // The strongest limitation we can find on a platform is that we might only be able to communicate with the host via "strings" (post-message)
        // and in that situation we can define a separator within the string like (callId,success,resultOrError) that the
        // side running in the browser can parse for processing.
        public void EndInvokeDotNet(string callId, bool success, string invocationResultOrError)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.EndInvokeDotNet, callId, success, invocationResultOrError));
        }

        public void NotifyUnhandledException(Exception exception)
        {
            var message = IpcCommon.Serialize(IpcCommon.OutgoingMessageType.NotifyUnhandledException, exception.Message, exception.StackTrace);
            _dispatcher.InvokeAsync(() => _messageDispatcher(message));
        }

        private void DispatchMessageWithErrorHandling(string message)
        {
            NotifyErrors(_dispatcher.InvokeAsync(() => _messageDispatcher(message)));
        }

        private void NotifyErrors(Task task)
        {
            _ = AwaitAndNotify();

            async Task AwaitAndNotify()
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    NotifyUnhandledException(ex);
                }
            }
        }
    }
}
