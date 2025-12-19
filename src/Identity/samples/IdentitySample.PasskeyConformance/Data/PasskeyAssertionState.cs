// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace IdentitySample.PasskeyConformance.Data;

internal sealed class PasskeyAssertionState
{
    public required ServerPublicKeyCredentialGetOptionsRequest Request { get; init; }
    public required string? AssertionState { get; init; }
}
