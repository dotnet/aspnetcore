// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
namespace Microsoft.AspNetCore.Components.WebView;

// These are all the messages .NET Host needs to know how to receive from JS

// This class is a "Proxy" or "front-controller" for the incoming messages from the Browser via the transport channel.
// It receives messages on OnMessageReceived, interprets the payload and dispatches them to the appropriate method
internal sealed class IpcReceiver
{
    private readonly Func<string, string, Task> _onAttachMessage;

    public IpcReceiver(Func<string, string, Task> onAttachMessage)
    {
        _onAttachMessage = onAttachMessage;
    }

    public async Task OnMessageReceivedAsync(PageContext pageContext, string message)
    {
        // Ignore other messages as they may be unrelated to Blazor WebView
        if (IpcCommon.TryDeserializeIncoming(message, out var messageType, out var args))
        {
            if (messageType == IpcCommon.IncomingMessageType.AttachPage)
            {
                await _onAttachMessage(args[0].GetString(), args[1].GetString());
                return;
            }

            // For any other message, you have to have a page attached already
            if (pageContext == null)
            {
                throw new InvalidOperationException("Cannot receive IPC messages when no page is attached");
            }

            // If the page's JS runtime has been marked disconnected (e.g., the page is being
            // reloaded or the WebView is shutting down), drop messages that would invoke .NET
            // user code or fire user-registered callbacks on a disposed scope. Forwarding them
            // would route stale JS object IDs into a disposed renderer/runtime or trigger
            // handlers registered against scoped services that have already been disposed
            // (see dotnet/aspnetcore#66255, dotnet/maui#34855).
            //
            // EndInvokeJS, OnRenderCompleted, and OnLocationChanged are intentionally NOT
            // dropped here:
            //   * EndInvokeJS completes pending InvokeAsync<T> task completion sources on
            //     the runtime. Dropping it would leave caller-awaited tasks hanging forever.
            //   * OnRenderCompleted just completes a pending render batch TCS on the
            //     renderer; the renderer tolerates being notified after disposal.
            //   * OnLocationChanged updates internal NavigationManager state and fires the
            //     LocationChanged event, both of which are benign on a disposed page.
            if (pageContext.JSRuntime.IsDisposed && messageType is
                IpcCommon.IncomingMessageType.BeginInvokeDotNet or
                IpcCommon.IncomingMessageType.ReceiveByteArrayFromJS or
                IpcCommon.IncomingMessageType.OnLocationChanging)
            {
                return;
            }

            switch (messageType)
            {
                case IpcCommon.IncomingMessageType.BeginInvokeDotNet:
                    BeginInvokeDotNet(pageContext, args[0].GetString(), args[1].GetString(), args[2].GetString(), args[3].GetInt64(), args[4].GetString());
                    break;
                case IpcCommon.IncomingMessageType.EndInvokeJS:
                    EndInvokeJS(pageContext, args[2].GetString());
                    break;
                case IpcCommon.IncomingMessageType.ReceiveByteArrayFromJS:
                    ReceiveByteArrayFromJS(pageContext, args[0].GetInt32(), args[1].GetBytesFromBase64());
                    break;
                case IpcCommon.IncomingMessageType.OnRenderCompleted:
                    OnRenderCompleted(pageContext, args[0].GetInt64(), args[1].GetString());
                    break;
                case IpcCommon.IncomingMessageType.OnLocationChanged:
                    OnLocationChanged(pageContext, args[0].GetString(), args[1].GetString(), args[2].GetBoolean());
                    break;
                case IpcCommon.IncomingMessageType.OnLocationChanging:
                    OnLocationChanging(pageContext, args[0].GetInt32(), args[1].GetString(), args[2].GetString(), args[3].GetBoolean());
                    break;
                default:
                    throw new InvalidOperationException($"Unknown message type '{messageType}'.");
            }
        }
    }

    private static void BeginInvokeDotNet(PageContext pageContext, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
    {
        DotNetDispatcher.BeginInvokeDotNet(
            pageContext.JSRuntime,
            new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId),
            argsJson);
    }

    private static void EndInvokeJS(PageContext pageContext, string argumentsOrError)
    {
        DotNetDispatcher.EndInvokeJS(pageContext.JSRuntime, argumentsOrError);
    }

    private static void ReceiveByteArrayFromJS(PageContext pageContext, int id, byte[] data)
    {
        DotNetDispatcher.ReceiveByteArray(pageContext.JSRuntime, id, data);
    }

    private static void OnRenderCompleted(PageContext pageContext, long batchId, string errorMessageOrNull)
    {
        if (errorMessageOrNull != null)
        {
            throw new InvalidOperationException(errorMessageOrNull);
        }

        pageContext.Renderer.NotifyRenderCompleted(batchId);
    }

    private static void OnLocationChanged(PageContext pageContext, string uri, string? state, bool intercepted)
    {
        pageContext.NavigationManager.LocationUpdated(uri, state, intercepted);
    }

    private static void OnLocationChanging(PageContext pageContext, int callId, string uri, string? state, bool intercepted)
    {
        pageContext.NavigationManager.HandleLocationChanging(callId, uri, state, intercepted);
    }
}
