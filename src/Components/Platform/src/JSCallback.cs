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
