// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView;

// Handles communication between the component abstractions (Renderer, NavigationManager, JSInterop, etc.)
// and the underlying transport channel
internal sealed class IpcSender
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

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(NavigationOptions))]
    public void Navigate(string uri, NavigationOptions options)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.Navigate, uri, options));
    }

    public void Refresh(bool forceReload)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.Refresh, forceReload));
    }

    public void AttachToDocument(int componentId, string selector)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.AttachToDocument, componentId, selector));
    }

    public void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.BeginInvokeJS, taskId, identifier, argsJson, resultType, targetInstanceId));
    }

    public void EndInvokeDotNet(string callId, bool success, string invocationResultOrError)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.EndInvokeDotNet, callId, success, invocationResultOrError));
    }

    public void SendByteArray(int id, byte[] data)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.SendByteArrayToJS, id, data));
    }

    public void SetHasLocationChangingListeners(bool hasListeners)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.SetHasLocationChangingListeners, hasListeners));
    }

    public void EndLocationChanging(int callId, bool shouldContinueNavigation)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(IpcCommon.OutgoingMessageType.EndLocationChanging, callId, shouldContinueNavigation));
    }

    public void NotifyUnhandledException(Exception exception)
    {
        // Send the serialized exception to the WebView for display
        var message = IpcCommon.Serialize(IpcCommon.OutgoingMessageType.NotifyUnhandledException, exception.Message, exception.StackTrace);
        _dispatcher.InvokeAsync(() => _messageDispatcher(message));

        // Also rethrow so the AppDomain's UnhandledException handler gets notified
        _dispatcher.InvokeAsync(() => ExceptionDispatchInfo.Capture(exception).Throw());
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
