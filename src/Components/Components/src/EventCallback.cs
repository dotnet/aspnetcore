// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A bound event handler delegate.
    /// </summary>
    public readonly struct EventCallback : IEventCallback
    {
        /// <summary>
        /// Gets a reference to the <see cref="EventCallbackFactory"/>.
        /// </summary>
        public static readonly EventCallbackFactory Factory = new EventCallbackFactory();

        /// <summary>
        /// Gets an empty <see cref="EventCallback"/>.
        /// </summary>
        public static readonly EventCallback Empty = new EventCallback(null, (Action)(() => { }));

        internal readonly MulticastDelegate? Delegate;
        internal readonly IHandleEvent? Receiver;

        /// <summary>
        /// Creates the new <see cref="EventCallback"/>.
        /// </summary>
        /// <param name="receiver">The event receiver.</param>
        /// <param name="delegate">The delegate to bind.</param>
        public EventCallback(IHandleEvent? receiver, MulticastDelegate? @delegate)
        {
            Receiver = receiver;
            Delegate = @delegate;
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
        public Task InvokeAsync(object? arg)
        {
            if (Receiver == null)
            {
                return EventCallbackWorkItem.InvokeAsync<object?>(Delegate, arg);
            }

            return Receiver.HandleEventAsync(new EventCallbackWorkItem(Delegate), arg);
        }

        /// <summary>
        /// Invokes the delegate associated with this binding and dispatches an event notification to the
        /// appropriate component.
        /// </summary>
        /// <returns>A <see cref="Task"/> which completes asynchronously once event processing has completed.</returns>
        public Task InvokeAsync() => InvokeAsync(null!);

        object? IEventCallback.UnpackForRenderTree()
        {
            return RequiresExplicitReceiver ? (object)this : Delegate;
        }

        internal static bool IsEquivalentForDiffing(ref EventCallback left, ref EventCallback right)
        {           
            if (left.Equals(right))
            {
                return true;
            }

            if (left.Delegate == null || right.Delegate == null)
            {
                return false;
            }

            // Normally an EventCallback is equal when its members are equal, i.e. Receiver and Delegate are equal.
            // Now there is a special case, where the delegates can point to different closure instances but are equal anyway.
            // We have to test for that. However, when the receivers differ (i.e. they point to a different component)
            // and they are (or at least one is) explicit (i.e. they are not the same as the delegates targets,
            // because then they could be closures again) we know that they are different.
            if (left.Receiver != right.Receiver &&
                (left.Receiver != left.Delegate!.Target || right.Receiver != right.Delegate!.Target))
            {
                return false;
            }

            return AttributeComparerForDiffing.IsEquivalentForDiffing(left.Delegate!, right.Delegate!);
        }
    }
}
