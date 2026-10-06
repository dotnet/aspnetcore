// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

/// <summary>
/// Extensions for <see cref="IJSInProcessRuntime"/>.
/// </summary>
public static class JSInProcessRuntimeExtensions
{
    /// <summary>
    /// Invokes the specified JavaScript function synchronously.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="IJSInProcessRuntime"/>.</param>
    /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public static void InvokeVoid(this IJSInProcessRuntime jsRuntime, string identifier, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);

        jsRuntime.Invoke<IJSVoidResult>(identifier, args);
    }
}
