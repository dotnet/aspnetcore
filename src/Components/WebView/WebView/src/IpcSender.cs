// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView;

// Handles communication between the component abstractions (Renderer, NavigationManager, JSInterop, etc.)
// and the underlying transport channel
internal sealed class IpcSender
{
    private readonly Dispatcher _dispatcher;
    private readonly Action<string> _messageDispatcher;
    private bool _disposed;

    public IpcSender(Dispatcher dispatcher, Action<string> messageDispatcher)
    {
        _dispatcher = dispatcher;
        _messageDispatcher = messageDispatcher;
    }

    /// <summary>
    /// Whether <see cref="Dispose"/> has been called. Once true, all outbound dispatches and
    /// <see cref="NotifyUnhandledException"/> calls become no-ops so the sender can't route
    /// messages into a torn-down platform WebView (see dotnet/maui#34855).
    /// </summary>
    internal bool IsDisposed => _disposed;

    /// <summary>
    /// Stops the sender from forwarding any further messages to the platform WebView. Intended
    /// to be called from <see cref="WebViewManager.DisposeAsync"/> before disposing the current
    /// page so that in-flight renderer batches and other outbound traffic don't reach a
    /// CoreWebView2 / WKWebView / Android WebView whose underlying control is already gone.
    /// </summary>
    internal void Dispose()
    {
        _disposed = true;
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

    public void BeginInvokeJS(in JSInvocationInfo invocationInfo)
    {
        DispatchMessageWithErrorHandling(IpcCommon.Serialize(
            IpcCommon.OutgoingMessageType.BeginInvokeJS,
            invocationInfo.AsyncHandle,
            invocationInfo.Identifier,
            invocationInfo.ArgsJson,
            invocationInfo.ResultType,
            invocationInfo.TargetInstanceId,
            invocationInfo.CallType
        ));
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
        if (_disposed)
        {
            // The WebView is gone, so we have nothing to display the exception in and
            // nothing to rethrow into. Dropping the notification is preferable to crashing
            // the host application from a background task that races with disposal.
            return;
        }

        // Send the serialized exception to the WebView for display
        var message = IpcCommon.Serialize(IpcCommon.OutgoingMessageType.NotifyUnhandledException, exception.Message, exception.StackTrace);
        _dispatcher.InvokeAsync(() => _messageDispatcher(message));

        // Also rethrow so the AppDomain's UnhandledException handler gets notified
        _dispatcher.InvokeAsync(() => ExceptionDispatchInfo.Capture(exception).Throw());
    }

    private void DispatchMessageWithErrorHandling(string message)
    {
        if (_disposed)
        {
            // The WebView is shutting down (or already disposed). Dropping outbound traffic
            // here prevents WebView2WebViewManager.SendMessage from invoking
            // CoreWebView2.PostWebMessageAsString on a disposed control (dotnet/maui#34855).
            return;
        }

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
