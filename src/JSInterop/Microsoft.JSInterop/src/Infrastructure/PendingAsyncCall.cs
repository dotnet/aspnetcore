// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Infrastructure;

internal sealed class PendingAsyncCall<[DynamicallyAccessedMembers(JsonSerialized)] TValue> : IPendingAsyncCall
{
    private readonly TaskCompletionSource<TValue> _completion = new();

    public Task<TValue> Task => _completion.Task;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The result type is annotated on InvokeAsync and flows to TValue.")]
    public void Complete(JSRuntime runtime, ref Utf8JsonReader reader)
    {
        var typeInfo = runtime.JsonSerializerOptions.GetTypeInfo(typeof(TValue));
        var value = (TValue?)JsonSerializer.Deserialize(ref reader, typeInfo);

        runtime.ByteArraysToBeRevived.Clear();

        _completion.SetResult(value!);
    }

    public void Fail(Exception exception) => _completion.SetException(exception);

    public void Cancel(CancellationToken cancellationToken) => _completion.TrySetCanceled(cancellationToken);
}
