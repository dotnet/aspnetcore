// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

// Represents the state to persist between creating the passkey request options
// and performing passkey assertion.
internal sealed class PasskeyAssertionState
{
    public required ReadOnlyMemory<byte> Challenge { get; init; }

    public string? UserId { get; init; }
}
