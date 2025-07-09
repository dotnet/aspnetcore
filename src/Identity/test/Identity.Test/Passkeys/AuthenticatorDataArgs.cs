// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

internal readonly struct AuthenticatorDataArgs
{
    public required AuthenticatorDataFlags Flags { get; init; }
    public required ReadOnlyMemory<byte> RpIdHash { get; init; }
    public required uint SignCount { get; init; }
    public ReadOnlyMemory<byte>? AttestedCredentialData { get; init; }
    public ReadOnlyMemory<byte>? Extensions { get; init; }
}
