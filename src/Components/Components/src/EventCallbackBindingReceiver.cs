// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components;

internal sealed class EventCallbackBindingReceiver(IHandleEvent? receiver)
{
    private static readonly ConditionalWeakTable<IHandleEvent, EventCallbackBindingReceiver> _receivers = new();
    private static readonly EventCallbackBindingReceiver _withoutReceiver = new(null);

    public IHandleEvent? Receiver { get; } = receiver;

    public static EventCallbackBindingReceiver Get(IHandleEvent? receiver)
        => receiver is null ? _withoutReceiver : _receivers.GetValue(receiver, static receiver => new(receiver));
}
