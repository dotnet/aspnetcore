// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop;

internal abstract class TestJSRuntimeBase : IJSRuntime
{
    public abstract ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, object?[]? args);

    public abstract ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args);

    public ValueTask<IJSObjectReference> InvokeNewAsync(string identifier, object?[]? args)
        => InvokeAsync<IJSObjectReference>($"new:{identifier}", args);

    public ValueTask<IJSObjectReference> InvokeNewAsync(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<IJSObjectReference>($"new:{identifier}", cancellationToken, args);

    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier)
        => InvokeAsync<TValue>($"get:{identifier}", null);

    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken)
        => InvokeAsync<TValue>($"get:{identifier}", cancellationToken, null);

    public async ValueTask SetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value)
        => await InvokeAsync<IJSVoidResult>($"set:{identifier}", [value]);

    public async ValueTask SetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value, CancellationToken cancellationToken)
        => await InvokeAsync<IJSVoidResult>($"set:{identifier}", cancellationToken, [value]);
}
