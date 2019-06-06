// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides extension methods for <see cref="EventCallbackFactory"/> and <see cref="UIEventArgs"/> types. For internal
    /// framework use.
    /// </summary>
    public static class EventCallbackFactoryUIEventArgsExtensions
    {
        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIChangeEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIChangeEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIClipboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIClipboardEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIClipboardEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIClipboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIClipboardEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIClipboardEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIDragEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIDragEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIDragEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIDragEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIDragEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIDragEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIErrorEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIErrorEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIErrorEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIErrorEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIErrorEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIErrorEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIFocusEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIFocusEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIFocusEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIFocusEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIFocusEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIFocusEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIKeyboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIKeyboardEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIKeyboardEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIKeyboardEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIKeyboardEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIKeyboardEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIMouseEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIMouseEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIMouseEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIMouseEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIMouseEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIMouseEventArgs>(receiver, callback);
        }
        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIPointerEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIPointerEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIPointerEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIPointerEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIPointerEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIPointerEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIProgressEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIProgressEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIProgressEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIProgressEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIProgressEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIProgressEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UITouchEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UITouchEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UITouchEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UITouchEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UITouchEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UITouchEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIWheelEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<UIWheelEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIWheelEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<UIWheelEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<UIWheelEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<UIWheelEventArgs>(receiver, callback);
        }
    }
}
