// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class UnsupportedJavaScriptRuntime : IJSRuntime
{
    private const string Message = "JavaScript interop calls cannot be issued during server-side static rendering, because the page has not yet loaded in the browser. Statically-rendered components must wrap any JavaScript interop calls in conditional logic to ensure those interop calls are not attempted during static rendering.";

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => throw new InvalidOperationException(Message);

    ValueTask<TValue> IJSRuntime.InvokeAsync<TValue>(string identifier, object?[]? args)
        => throw new InvalidOperationException(Message);
}
