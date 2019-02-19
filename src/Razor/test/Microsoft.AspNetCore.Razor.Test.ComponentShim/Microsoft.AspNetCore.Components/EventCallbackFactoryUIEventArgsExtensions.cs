// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    public static class EventCallbackFactoryUIEventArgsExtensions
    {
        public static EventCallback<UIEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIEventArgs> callback) => default;

        public static EventCallback<UIEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIEventArgs, Task> callback) => default;

        public static EventCallback<UIChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIChangeEventArgs> callback) => default;

        public static EventCallback<UIChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIChangeEventArgs, Task> callback) => default;

        public static EventCallback<UIClipboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIClipboardEventArgs> callback) => default;

        public static EventCallback<UIClipboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIClipboardEventArgs, Task> callback) => default;

        public static EventCallback<UIDragEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIDragEventArgs> callback) => default;

        public static EventCallback<UIDragEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIDragEventArgs, Task> callback) => default;

        public static EventCallback<UIErrorEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIErrorEventArgs> callback) => default;

        public static EventCallback<UIErrorEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIErrorEventArgs, Task> callback) => default;

        public static EventCallback<UIFocusEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIFocusEventArgs> callback) => default;

        public static EventCallback<UIFocusEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIFocusEventArgs, Task> callback) => default;

        public static EventCallback<UIKeyboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIKeyboardEventArgs> callback) => default;

        public static EventCallback<UIKeyboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIKeyboardEventArgs, Task> callback) => default;

        public static EventCallback<UIMouseEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIMouseEventArgs> callback) => default;

        public static EventCallback<UIMouseEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIMouseEventArgs, Task> callback) => default;

        public static EventCallback<UIPointerEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIPointerEventArgs> callback) => default;

        public static EventCallback<UIPointerEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIPointerEventArgs, Task> callback) => default;

        public static EventCallback<UIProgressEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIProgressEventArgs> callback) => default;

        public static EventCallback<UIProgressEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIProgressEventArgs, Task> callback) => default;

        public static EventCallback<UITouchEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UITouchEventArgs> callback) => default;

        public static EventCallback<UITouchEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UITouchEventArgs, Task> callback) => default;

        public static EventCallback<UIWheelEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIWheelEventArgs> callback) => default;

        public static EventCallback<UIWheelEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIWheelEventArgs, Task> callback) => default;
    }
}
