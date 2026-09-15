// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Extensions.Internal;

/// <summary>
/// Helper code for security-related operations.
/// </summary>
internal static class SecurityHelper
{
    internal const int UserIdentifierSize = SHA256.HashSizeInBytes;

    /// <summary>
    /// Add all ClaimsIdentities from an additional ClaimPrincipal to the ClaimsPrincipal
    /// Merges a new claims principal, placing all new identities first, and eliminating
    /// any empty unauthenticated identities from context.User
    /// </summary>
    /// <param name="existingPrincipal">The <see cref="ClaimsPrincipal"/> containing existing <see cref="ClaimsIdentity"/>.</param>
    /// <param name="additionalPrincipal">The <see cref="ClaimsPrincipal"/> containing <see cref="ClaimsIdentity"/> to be added.</param>
    public static ClaimsPrincipal MergeUserPrincipal(ClaimsPrincipal? existingPrincipal, ClaimsPrincipal? additionalPrincipal)
    {
        // For the first principal, just use the new principal rather than copying it
        if (existingPrincipal == null && additionalPrincipal != null)
        {
            return additionalPrincipal;
        }

        var newPrincipal = new ClaimsPrincipal();

        // New principal identities go first
        if (additionalPrincipal != null)
        {
            newPrincipal.AddIdentities(additionalPrincipal.Identities);
        }

        // Then add any existing non empty or authenticated identities
        if (existingPrincipal != null)
        {
            newPrincipal.AddIdentities(existingPrincipal.Identities.Where(i => i.IsAuthenticated || i.Claims.Any()));
        }
        return newPrincipal;
    }

    /// <summary>
    /// Determines whether the <paramref name="user"/> is authenticated.
    /// Uses the same aggregate semantics as authorization's
    /// <c>DenyAnonymousAuthorizationRequirement</c>: the principal is authenticated when it has an
    /// identity and at least one of its identities is authenticated (not only the primary identity).
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> to inspect.</param>
    /// <returns><see langword="true"/> if the principal is authenticated; otherwise <see langword="false"/>.</returns>
    public static bool IsAuthenticated(ClaimsPrincipal? user)
        => user?.Identity is not null && user.Identities.Any(static identity => identity.IsAuthenticated);

    /// <summary>
    /// Computes a stable identifier from claims on authenticated identities.
    /// </summary>
    /// <param name="principal">The principal to identify.</param>
    /// <param name="destination">The destination for the identifier.</param>
    /// <returns><see langword="true"/> when an authenticated identity contains at least one claim.</returns>
    public static bool TryGetUserIdentifier(ClaimsPrincipal? principal, Span<byte> destination)
    {
        if (principal is null)
        {
            return false;
        }

        var uniqueIdentifierParameters = GetUniqueIdentifierParameters(principal.Identities);
        if (uniqueIdentifierParameters is null)
        {
            return false;
        }

        ComputeSha256(uniqueIdentifierParameters, destination);
        return true;
    }

    private static List<string>? GetUniqueIdentifierParameters(IEnumerable<ClaimsIdentity> claimsIdentities)
    {
        var identitiesList = claimsIdentities as List<ClaimsIdentity>;
        if (identitiesList is null)
        {
            identitiesList = [.. claimsIdentities];
        }

        for (var i = 0; i < identitiesList.Count; i++)
        {
            var identity = identitiesList[i];
            if (!identity.IsAuthenticated)
            {
                continue;
            }

            var subClaim = identity.FindFirst(
                claim => string.Equals("sub", claim.Type, StringComparison.Ordinal));
            if (subClaim is not null && !string.IsNullOrEmpty(subClaim.Value))
            {
                return
                [
                    subClaim.Type,
                    subClaim.Value,
                    subClaim.Issuer
                ];
            }

            var nameIdentifierClaim = identity.FindFirst(
                claim => string.Equals(ClaimTypes.NameIdentifier, claim.Type, StringComparison.Ordinal));
            if (nameIdentifierClaim is not null && !string.IsNullOrEmpty(nameIdentifierClaim.Value))
            {
                return
                [
                    nameIdentifierClaim.Type,
                    nameIdentifierClaim.Value,
                    nameIdentifierClaim.Issuer
                ];
            }

            var upnClaim = identity.FindFirst(
                claim => string.Equals(ClaimTypes.Upn, claim.Type, StringComparison.Ordinal));
            if (upnClaim is not null && !string.IsNullOrEmpty(upnClaim.Value))
            {
                return
                [
                    upnClaim.Type,
                    upnClaim.Value,
                    upnClaim.Issuer
                ];
            }
        }

        var allClaims = new List<Claim>();
        for (var i = 0; i < identitiesList.Count; i++)
        {
            if (identitiesList[i].IsAuthenticated)
            {
                allClaims.AddRange(identitiesList[i].Claims);
            }
        }

        if (allClaims.Count == 0)
        {
            return null;
        }

        allClaims.Sort((a, b) => string.Compare(a.Type, b.Type, StringComparison.Ordinal));

        var identifierParameters = new List<string>(allClaims.Count * 3);
        for (var i = 0; i < allClaims.Count; i++)
        {
            var claim = allClaims[i];
            identifierParameters.Add(claim.Type);
            identifierParameters.Add(claim.Value);
            identifierParameters.Add(claim.Issuer);
        }

        return identifierParameters;
    }

    private static void ComputeSha256(List<string> parameters, Span<byte> destination)
    {
        Debug.Assert(destination.Length >= UserIdentifierSize);

        var totalSize = 0;
        for (var i = 0; i < parameters.Count; i++)
        {
            var byteCount = Encoding.UTF8.GetByteCount(parameters[i]);
            totalSize += Measure7BitEncodedUIntLength(byteCount) + byteCount;
        }

        byte[]? rentedBuffer = null;
        var buffer = totalSize <= 256
            ? stackalloc byte[256]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(totalSize));

        try
        {
            var span = buffer[..totalSize];
            var offset = 0;
            for (var i = 0; i < parameters.Count; i++)
            {
                offset += Write7BitEncodedString(span[offset..], parameters[i]);
            }

            SHA256.HashData(span[..offset], destination);
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private static int Measure7BitEncodedUIntLength(int value)
        => ((31 - System.Numerics.BitOperations.LeadingZeroCount((uint)value | 1)) / 7) + 1;

    private static int Write7BitEncodedString(Span<byte> target, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            target[0] = 0;
            return 1;
        }

        var stringByteCount = Encoding.UTF8.GetByteCount(value);
        var lengthPrefixSize = Write7BitEncodedInt(target, (uint)stringByteCount);
        Encoding.UTF8.GetBytes(value.AsSpan(), target[lengthPrefixSize..]);
        return lengthPrefixSize + stringByteCount;
    }

    private static int Write7BitEncodedInt(Span<byte> target, uint value)
    {
        var index = 0;
        while (value > 0x7Fu)
        {
            target[index++] = (byte)(value | ~0x7Fu);
            value >>= 7;
        }

        target[index++] = (byte)value;
        return index;
    }
}
