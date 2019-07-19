// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides extension methods for <see cref="EventCallbackFactory"/> and <see cref="UIEventArgs"/> types.
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
    }
}
