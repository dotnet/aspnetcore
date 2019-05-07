// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Browser
{
    /// <summary>
    /// Provides mechanisms for dispatching events to components in a <see cref="Renderer"/>.
    /// </summary>
    public static class RendererRegistryEventDispatcher
    {
        private static bool isDispatchingEvent;
        private static Queue<IncomingEventInfo> deferredIncomingEvents
            = new Queue<IncomingEventInfo>();

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(DispatchEvent))]
        public static Task DispatchEvent(
            BrowserEventDescriptor eventDescriptor, string eventArgsJson)
        {
            // Be sure we only run one event handler at once. Although they couldn't run
            // simultaneously anyway (there's only one thread), they could run nested on
            // the stack if somehow one event handler triggers another event synchronously.
            // We need event handlers not to overlap because (a) that's consistent with
            // server-side Blazor which uses a sync context, and (b) the rendering logic
            // relies completely on the idea that within a given scope it's only building
            // or processing one batch at a time.
            //
            // The only currently known case where this makes a difference is in the E2E
            // tests in ReorderingFocusComponent, where we hit what seems like a Chrome bug
            // where mutating the DOM cause an element's "change" to fire while its "input"
            // handler is still running (i.e., nested on the stack) -- this doesn't happen
            // in Firefox. Possibly a future version of Chrome may fix this, but even then,
            // it's conceivable that DOM mutation events could trigger this too.

            if (isDispatchingEvent)
            {
                var info = new IncomingEventInfo(eventDescriptor, eventArgsJson);
                deferredIncomingEvents.Enqueue(info);
                return info.TaskCompletionSource.Task;
            }
            else
            {
                isDispatchingEvent = true;
                try
                {
                    var eventArgs = ParseEventArgsJson(eventDescriptor.EventArgsType, eventArgsJson);
                    var renderer = RendererRegistry.Current.Find(eventDescriptor.BrowserRendererId);
                    return renderer.DispatchEventAsync(eventDescriptor.EventHandlerId, eventArgs);
                }
                finally
                {
                    isDispatchingEvent = false;
                    if (deferredIncomingEvents.Count > 0)
                    {
                        ProcessNextDeferredEvent();
                    }
                }
            }
        }

        private static void ProcessNextDeferredEvent()
        {
            var info = deferredIncomingEvents.Dequeue();
            var task = DispatchEvent(info.EventDescriptor, info.EventArgsJson);
            task.ContinueWith(_ =>
            {
                if (task.Exception != null)
                {
                    info.TaskCompletionSource.SetException(task.Exception);
                }
                else
                {
                    info.TaskCompletionSource.SetResult(null);
                }
            });
        }

        private static UIEventArgs ParseEventArgsJson(string eventArgsType, string eventArgsJson)
        {
            switch (eventArgsType)
            {
                case "change":
                    return Json.Deserialize<UIChangeEventArgs>(eventArgsJson);
                case "clipboard":
                    return Json.Deserialize<UIClipboardEventArgs>(eventArgsJson);
                case "drag":
                    return Json.Deserialize<UIDragEventArgs>(eventArgsJson);
                case "error":
                    return Json.Deserialize<UIErrorEventArgs>(eventArgsJson);
                case "focus":
                    return Json.Deserialize<UIFocusEventArgs>(eventArgsJson);
                case "keyboard":
                    return Json.Deserialize<UIKeyboardEventArgs>(eventArgsJson);
                case "mouse":
                    return Json.Deserialize<UIMouseEventArgs>(eventArgsJson);
                case "pointer":
                    return Json.Deserialize<UIPointerEventArgs>(eventArgsJson);
                case "progress":
                    return Json.Deserialize<UIProgressEventArgs>(eventArgsJson);
                case "touch":
                    return Json.Deserialize<UITouchEventArgs>(eventArgsJson);
                case "unknown":
                    return Json.Deserialize<UIEventArgs>(eventArgsJson);
                case "wheel":
                    return Json.Deserialize<UIWheelEventArgs>(eventArgsJson);
                default:
                     throw new ArgumentException($"Unsupported value '{eventArgsType}'.", nameof(eventArgsType));
            }
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        public class BrowserEventDescriptor
        {
            /// <summary>
            /// For framework use only.
            /// </summary>
            public int BrowserRendererId { get; set; }

            /// <summary>
            /// For framework use only.
            /// </summary>
            public int EventHandlerId { get; set; }

            /// <summary>
            /// For framework use only.
            /// </summary>
            public string EventArgsType { get; set; }
        }

        readonly struct IncomingEventInfo
        {
            public readonly BrowserEventDescriptor EventDescriptor;
            public readonly string EventArgsJson;
            public readonly TaskCompletionSource<object> TaskCompletionSource;

            public IncomingEventInfo(BrowserEventDescriptor eventDescriptor, string eventArgsJson)
            {
                EventDescriptor = eventDescriptor;
                EventArgsJson = eventArgsJson;
                TaskCompletionSource = new TaskCompletionSource<object>();
            }
        }
    }
}
