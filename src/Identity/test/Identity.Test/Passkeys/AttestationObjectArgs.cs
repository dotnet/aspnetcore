// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Identity.Test;

internal readonly struct AttestationObjectArgs
{
    public required int? CborMapLength { get; init; }
    public required string? Format { get; init; }
    public required ReadOnlyMemory<byte>? AttestationStatement { get; init; }
    public required ReadOnlyMemory<byte>? AuthenticatorData { get; init; }
}
