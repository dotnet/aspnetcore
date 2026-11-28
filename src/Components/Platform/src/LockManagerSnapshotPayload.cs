// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Components.Platform;

// The lock manager snapshot as it crosses from JavaScript. The lists arrive as arrays and are wrapped without a copy.
internal sealed class LockManagerSnapshotPayload
{
    public LockInfo[]? Held { get; init; }

    public LockInfo[]? Pending { get; init; }

    public LockManagerSnapshot ToLockManagerSnapshot() => new()
    {
        Held = Held is null ? null : ImmutableCollectionsMarshal.AsImmutableArray(Held),
        Pending = Pending is null ? null : ImmutableCollectionsMarshal.AsImmutableArray(Pending),
    };
}
