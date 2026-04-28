// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// This interface can extract unique identifers for a <see cref="ClaimsPrincipal"/>.
/// </summary>
internal interface IClaimUidExtractor
{
    /// <summary>
    /// Extracts claims identifier, and writes into <paramref name="destination"/> buffer.
    /// </summary>
    bool TryExtractClaimUidBytes(ClaimsPrincipal claimsPrincipal, Span<byte> destination);
}
