// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

// Objects that the functions created by JSCallbackFactory call. The browser can call such a function
// before a handler is set or after it is removed, so a missing handler is not an error.

internal sealed class JSCallback<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T1>
{
    public Func<T1, Task>? Handler { get; set; }

    [JSInvokable]
    public Task Invoke(T1 arg1) => Handler?.Invoke(arg1) ?? Task.CompletedTask;
}

internal sealed class JSCallback<
    [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T1,
    [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T2>
{
    public Func<T1, T2, Task>? Handler { get; set; }

    [JSInvokable]
    public Task Invoke(T1 arg1, T2 arg2) => Handler?.Invoke(arg1, arg2) ?? Task.CompletedTask;
}

internal sealed class JSCallback<
    [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T1,
    [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T2,
    [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T3>
{
    public Func<T1, T2, T3, Task>? Handler { get; set; }

    [JSInvokable]
    public Task Invoke(T1 arg1, T2 arg2, T3 arg3) => Handler?.Invoke(arg1, arg2, arg3) ?? Task.CompletedTask;
}
