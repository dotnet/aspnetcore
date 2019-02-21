// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A factory for creating <see cref="EventCallback"/> and <see cref="EventCallback{T}"/>
    /// instances.
    /// </summary>
    public sealed class EventCallbackFactory
    {
        /// <summary>
        /// Returns the provided <paramref name="callback"/>. For internal framework use only.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventCallback Create(object receiver, EventCallback callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return callback;
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback Create(object receiver, Action callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback Create(object receiver, Action<object> callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback Create(object receiver, Func<Task> callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback Create(object receiver, Func<object, Task> callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore(receiver, callback);
        }

        /// <summary>
        /// Returns the provided <paramref name="callback"/>. For internal framework use only.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Create<T>(object receiver, string callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return callback;
        }

        /// <summary>
        /// Returns the provided <paramref name="callback"/>. For internal framework use only.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventCallback<T> Create<T>(object receiver, EventCallback callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return new EventCallback<T>(callback.Receiver, callback.Delegate);
        }

        /// <summary>
        /// Returns the provided <paramref name="callback"/>. For internal framework use only.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventCallback<T> Create<T>(object receiver, EventCallback<T> callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return callback;
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback<T> Create<T>(object receiver, Action callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore<T>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback<T> Create<T>(object receiver, Action<T> callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore<T>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback<T> Create<T>(object receiver, Func<Task> callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore<T>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public EventCallback<T> Create<T>(object receiver, Func<T, Task> callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            return CreateCore<T>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>. For internal framework use only.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventCallback<T> CreateInferred<T>(object receiver, Action<T> callback, T value)
        {
            return Create(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>. For internal framework use only.
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="callback"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventCallback<T> CreateInferred<T>(object receiver, Func<T, Task> callback, T value)
        {
            return Create(receiver, callback);
        }

        private EventCallback CreateCore(object receiver, MulticastDelegate callback)
        {
            return new EventCallback(callback?.Target as IHandleEvent ?? receiver as IHandleEvent, callback);
        }

        private EventCallback<T> CreateCore<T>(object receiver, MulticastDelegate callback)
        {
            return new EventCallback<T>(callback?.Target as IHandleEvent ?? receiver as IHandleEvent, callback);
        }
    }
}
