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
    /// Extracts claims identifier and writes to <paramref name="destination"/>.
    /// </summary>
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/>.</param>
    /// <param name="destination">destination for claimUid bytes</param>
    /// <param name="bytesWritten">length of bytes written to <paramref name="destination"/></param>
    void ExtractClaimUid(ClaimsPrincipal claimsPrincipal, Span<byte> destination, out int bytesWritten);

    /// <summary>
    /// Extracts claims identifier.
    /// </summary>
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/>.</param>
    /// <returns>The claims identifier.</returns>
    string? ExtractClaimUid(ClaimsPrincipal claimsPrincipal);
}
