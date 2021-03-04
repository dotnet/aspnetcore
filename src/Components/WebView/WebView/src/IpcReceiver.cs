// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
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
        private readonly Func<string, string, Task> _onAttachMessage;

        public IpcReceiver(Func<string,string,Task> onAttachMessage)
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
                    // TODO: Should we just ignore these messages? Is there any way their delivery
                    // might be delayed until after a page has detached?
                    throw new InvalidOperationException("Cannot receive IPC messages when no page is attached");
                }

                switch (messageType)
                {
                    case IpcCommon.IncomingMessageType.BeginInvokeDotNet:
                        BeginInvokeDotNet(pageContext, args[0].GetString(), args[1].GetString(), args[2].GetString(), args[3].GetInt64(), args[4].GetString());
                        break;
                    case IpcCommon.IncomingMessageType.EndInvokeJS:
                        EndInvokeJS(pageContext, args[0].GetInt64(), args[1].GetBoolean(), args[2].GetString());
                        break;
                    case IpcCommon.IncomingMessageType.DispatchBrowserEvent:
                        await DispatchBrowserEventAsync(pageContext, args[0].GetRawText(), args[1].GetRawText());
                        break;
                    case IpcCommon.IncomingMessageType.OnRenderCompleted:
                        OnRenderCompleted(pageContext, args[0].GetInt64(), args[1].GetString());
                        break;
                    case IpcCommon.IncomingMessageType.OnLocationChanged:
                        OnLocationChanged(pageContext, args[0].GetString(), args[1].GetBoolean());
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown message type '{messageType}'.");
                }
            }
        }

        private void BeginInvokeDotNet(PageContext pageContext, string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            DotNetDispatcher.BeginInvokeDotNet(
                pageContext.JSRuntime,
                new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId),
                argsJson);
        }

        private void EndInvokeJS(PageContext pageContext, long asyncHandle, bool succeeded, string argumentsOrError)
        {
            if (succeeded)
            {
                DotNetDispatcher.EndInvokeJS(pageContext.JSRuntime, argumentsOrError);
            }
            else
            {
                throw new InvalidOperationException(argumentsOrError);
            }
        }

        private Task DispatchBrowserEventAsync(PageContext pageContext, string eventDescriptor, string eventArgs)
        {
            var renderer = pageContext.Renderer;
            var webEventData = WebEventData.Parse(renderer, eventDescriptor, eventArgs);
            return renderer.DispatchEventAsync(
                webEventData.EventHandlerId,
                webEventData.EventFieldInfo,
                webEventData.EventArgs);
        }

        private void OnRenderCompleted(PageContext pageContext, long batchId, string errorMessageOrNull)
        {
            if (errorMessageOrNull != null)
            {
                throw new InvalidOperationException(errorMessageOrNull);
            }

            pageContext.Renderer.NotifyRenderCompleted(batchId);
        }

        private void OnLocationChanged(PageContext pageContext, string uri, bool intercepted)
        {
            pageContext.NavigationManager.LocationUpdated(uri, intercepted);
        }
    }
}
