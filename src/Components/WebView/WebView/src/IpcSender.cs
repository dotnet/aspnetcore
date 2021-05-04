// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView
{
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

        public void Navigate(string uri, bool forceLoad)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.Navigate, uri, forceLoad));
        }

        public void AttachToDocument(int componentId, string selector)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.AttachToDocument, componentId, selector));
        }

        public void DetachFromDocument(int componentId)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.DetachFromDocument, componentId));
        }

        public void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.BeginInvokeJS, taskId, identifier, argsJson, resultType, targetInstanceId));
        }

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
