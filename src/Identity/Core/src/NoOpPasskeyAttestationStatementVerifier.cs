// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

internal sealed class NoOpPasskeyAttestationStatementVerifier : IPasskeyAttestationStatementVerifier
{
    public Task<bool> VerifyAsync(ReadOnlyMemory<byte> attestationObject, ReadOnlyMemory<byte> clientDataHash)
        => Task.FromResult(true);
}
