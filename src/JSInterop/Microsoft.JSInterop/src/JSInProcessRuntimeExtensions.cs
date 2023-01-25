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
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The method returns void, so nothing is deserialized.")]
    public static void InvokeVoid(this IJSInProcessRuntime jsRuntime, string identifier, params object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);

        jsRuntime.Invoke<IJSVoidResult>(identifier, args);
    }
}
