// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
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

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
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

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
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

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
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
        public EventCallback<T> Create<T>(object receiver, Action callback)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
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

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
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

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
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

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return CreateCore<T>(receiver, callback);
        }

        private EventCallback CreateCore(object receiver, MulticastDelegate callback)
        {
            if (!object.ReferenceEquals(receiver, callback.Target) && receiver is IHandleEvent handler)
            {
                return new EventCallback(handler, callback);
            }

            return new EventCallback(callback.Target as IHandleEvent, callback);
        }

        private EventCallback<T> CreateCore<T>(object receiver, MulticastDelegate callback)
        {
            if (!object.ReferenceEquals(receiver, callback.Target) && receiver is IHandleEvent handler)
            {
                return new EventCallback<T>(handler, callback);
            }

            return new EventCallback<T>(callback.Target as IHandleEvent, callback);
        }
    }
}
