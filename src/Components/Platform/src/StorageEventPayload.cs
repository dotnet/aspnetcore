// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

// The storage event as it crosses from JavaScript. StorageArea holds the name of the global, not a reference.
internal sealed class StorageEventPayload
{
    public string? Key { get; init; }

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? StorageArea { get; init; }

    public StorageEvent ToStorageEvent(Window window) => new()
    {
        Key = Key,
        OldValue = OldValue,
        NewValue = NewValue,
        Url = Url,
        StorageArea = StorageArea switch
        {
            "localStorage" => window.LocalStorage,
            "sessionStorage" => window.SessionStorage,
            _ => null,
        },
    };
}
