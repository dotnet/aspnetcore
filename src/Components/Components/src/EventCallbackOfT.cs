// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A bound event handler delegate.
    /// </summary>
    public readonly struct EventCallback<TValue> : IEventCallback
    {
        /// <summary>
        /// Gets an empty <see cref="EventCallback{TValue}"/>.
        /// </summary>
        public static readonly EventCallback<TValue> Empty = new EventCallback<TValue>(null, (Action)(() => { }));

        internal readonly MulticastDelegate? Delegate;
        internal readonly IHandleEvent? Receiver;
        internal readonly bool PreventRender;

        /// <summary>
        /// Creates the new <see cref="EventCallback{TValue}"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="delegate">The delegate to bind.</param>
        public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate)
        {
            Receiver = receiver;
            Delegate = @delegate;
            PreventRender = false;
        }

        /// <summary>
        /// Creates the new <see cref="EventCallback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="delegate">The delegate to bind.</param>
        /// <param name="preventRender"></param>
        public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate, bool preventRender)
        {
            Receiver = receiver;
            Delegate = @delegate;
            PreventRender = preventRender;
        }

        /// <summary>
        /// Gets a value that indicates whether the delegate associated with this event dispatcher is non-null.
        /// </summary>
        public bool HasDelegate => Delegate != null;

        // This is a hint to the runtime that Receiver is a different object than what
        // Delegate.Target points to. This allows us to avoid boxing the command object
        // when building the render tree. See logic where this is used.
        internal bool RequiresExplicitReceiver => Receiver != null && !object.ReferenceEquals(Receiver, Delegate?.Target);

        /// <summary>
        /// Invokes the delegate associated with this binding and dispatches an event notification to the
        /// appropriate component.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns>A <see cref="Task"/> which completes asynchronously once event processing has completed.</returns>
        public Task InvokeAsync(TValue? arg)
        {
            if (Receiver == null)
            {
                return EventCallbackWorkItem.InvokeAsync<TValue?>(Delegate, arg);
            }

            return Receiver.HandleEventAsync(new EventCallbackWorkItem(Delegate), arg, PreventRender);
        }

        /// <summary>
        /// Invokes the delegate associated with this binding and dispatches an event notification to the
        /// appropriate component.
        /// </summary>
        /// <returns>A <see cref="Task"/> which completes asynchronously once event processing has completed.</returns>
        public Task InvokeAsync() => InvokeAsync(default!);

        internal EventCallback AsUntyped()
        {
            return new EventCallback(Receiver ?? Delegate?.Target as IHandleEvent, Delegate, PreventRender);
        }

        object? IEventCallback.UnpackForRenderTree()
        {
            return RequiresExplicitReceiver ? (object)AsUntyped() : Delegate;
        }
    }
}
