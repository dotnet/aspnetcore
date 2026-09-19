// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Default implementation of <see cref="IClaimUidExtractor"/>.
/// </summary>
internal sealed class DefaultClaimUidExtractor : IClaimUidExtractor
{
    public bool TryExtractClaimUidBytes(ClaimsPrincipal claimsPrincipal, Span<byte> destination)
    {
        Debug.Assert(claimsPrincipal != null);
        return SecurityHelper.TryGetUserIdentifier(claimsPrincipal, destination);
    }
}
