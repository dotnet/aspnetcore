// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides extension methods for <see cref="EventCallbackFactory"/> and <see cref="EventArgs"/> types.
    /// </summary>
    public static class EventCallbackFactoryEventArgsExtensions
    {
        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<EventArgs> Create(this EventCallbackFactory factory, object receiver, Action<EventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<EventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<EventArgs> Create(this EventCallbackFactory factory, object receiver, Func<EventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<EventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<ChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Action<ChangeEventArgs> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<ChangeEventArgs>(receiver, callback);
        }

        /// <summary>
        /// Creates an <see cref="EventCallback"/> for the provided <paramref name="receiver"/> and
        /// <paramref name="callback"/>.
        /// </summary>
        /// <param name="factory">The <see cref="EventCallbackFactory"/>.</param>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="callback">The event callback.</param>
        /// <returns>The <see cref="EventCallback"/>.</returns>
        public static EventCallback<ChangeEventArgs> Create(this EventCallbackFactory factory, object receiver, Func<ChangeEventArgs, Task> callback)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return factory.Create<ChangeEventArgs>(receiver, callback);
        }
    }
}
