// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

// Represents the state to persist between creating the passkey creation options
// and performing passkey attestation.
internal sealed class PasskeyAttestationState
{
    public required ReadOnlyMemory<byte> Challenge { get; init; }

    public required PasskeyUserEntity UserEntity { get; init; }
}
