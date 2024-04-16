// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.JSInterop;

internal interface IJSInteropTask : IDisposable
{
    public Type ResultType { get; }

    public JsonSerializerOptions? DeserializeOptions { get; set; }

    void SetResult(object? result);

    void SetException(Exception exception);
}
