// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Extensions.Internal;

/// <summary>
/// Helper code for security-related operations.
/// </summary>
internal static class SecurityHelper
{
    private const string SubjectClaimType = "sub";
    private const int StackAllocThreshold = 256;
    private const int InitialPoolSize = 8;

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

        var principalIdentities = principal.Identities;
        if (principalIdentities is List<ClaimsIdentity> identities)
        {
            return TryGetUserIdentifier(CollectionsMarshal.AsSpan(identities), destination);
        }

        if (principalIdentities is ClaimsIdentity[] identitiesArray)
        {
            return TryGetUserIdentifier(identitiesArray, destination);
        }

        ClaimsIdentity[]? rentedIdentities = null;
        var identityCount = 0;
        try
        {
            rentedIdentities = ArrayPool<ClaimsIdentity>.Shared.Rent(InitialPoolSize);
            foreach (var identity in principalIdentities)
            {
                AddPooledItem(identity, ref rentedIdentities, ref identityCount);
            }

            return TryGetUserIdentifier(rentedIdentities.AsSpan(0, identityCount), destination);
        }
        finally
        {
            if (rentedIdentities is not null)
            {
                rentedIdentities.AsSpan(0, identityCount).Clear();
                ArrayPool<ClaimsIdentity>.Shared.Return(rentedIdentities);
            }
        }
    }

    private static bool TryGetUserIdentifier(ReadOnlySpan<ClaimsIdentity> identities, Span<byte> destination)
    {
        for (var i = 0; i < identities.Length; i++)
        {
            var identity = identities[i];
            if (!identity.IsAuthenticated)
            {
                continue;
            }

            var identifierClaim = FindUserIdentifierClaim(identity);
            if (identifierClaim is not null)
            {
                ComputeSha256(identifierClaim, destination);
                return true;
            }
        }

        Claim[]? rentedClaims = null;
        var claimCount = 0;
        try
        {
            rentedClaims = ArrayPool<Claim>.Shared.Rent(InitialPoolSize);
            for (var i = 0; i < identities.Length; i++)
            {
                if (identities[i].IsAuthenticated)
                {
                    AddClaims(identities[i].Claims, ref rentedClaims, ref claimCount);
                }
            }

            if (claimCount == 0)
            {
                return false;
            }

            var claims = rentedClaims.AsSpan(0, claimCount);
            claims.Sort(static (a, b) => string.Compare(a.Type, b.Type, StringComparison.Ordinal));
            ComputeSha256(claims, destination);
            return true;
        }
        finally
        {
            if (rentedClaims is not null)
            {
                rentedClaims.AsSpan(0, claimCount).Clear();
                ArrayPool<Claim>.Shared.Return(rentedClaims);
            }
        }
    }

    private static Claim? FindUserIdentifierClaim(ClaimsIdentity identity)
    {
        if (identity.GetType() == typeof(ClaimsIdentity))
        {
            return FindUserIdentifierClaim(identity.Claims);
        }

        var subClaim = identity.FindFirst(
            static claim => string.Equals(SubjectClaimType, claim.Type, StringComparison.Ordinal));
        if (subClaim is not null && !string.IsNullOrEmpty(subClaim.Value))
        {
            return subClaim;
        }

        var nameIdentifierClaim = identity.FindFirst(
            static claim => string.Equals(ClaimTypes.NameIdentifier, claim.Type, StringComparison.Ordinal));
        if (nameIdentifierClaim is not null && !string.IsNullOrEmpty(nameIdentifierClaim.Value))
        {
            return nameIdentifierClaim;
        }

        var upnClaim = identity.FindFirst(
            static claim => string.Equals(ClaimTypes.Upn, claim.Type, StringComparison.Ordinal));
        return upnClaim is not null && !string.IsNullOrEmpty(upnClaim.Value) ? upnClaim : null;
    }

    private static Claim? FindUserIdentifierClaim(IEnumerable<Claim> claims)
    {
        if (claims is List<Claim> claimsList)
        {
            return FindUserIdentifierClaim(CollectionsMarshal.AsSpan(claimsList));
        }

        Claim? subClaim = null;
        Claim? nameIdentifierClaim = null;
        Claim? upnClaim = null;
        foreach (var claim in claims)
        {
            if (subClaim is null && string.Equals(SubjectClaimType, claim.Type, StringComparison.Ordinal))
            {
                subClaim = claim;
                if (!string.IsNullOrEmpty(claim.Value))
                {
                    return claim;
                }
            }
            else if (nameIdentifierClaim is null && string.Equals(ClaimTypes.NameIdentifier, claim.Type, StringComparison.Ordinal))
            {
                nameIdentifierClaim = claim;
            }
            else if (upnClaim is null && string.Equals(ClaimTypes.Upn, claim.Type, StringComparison.Ordinal))
            {
                upnClaim = claim;
            }
        }

        return GetFirstNonemptyClaim(nameIdentifierClaim, upnClaim);
    }

    private static Claim? FindUserIdentifierClaim(ReadOnlySpan<Claim> claims)
    {
        Claim? subClaim = null;
        Claim? nameIdentifierClaim = null;
        Claim? upnClaim = null;
        for (var i = 0; i < claims.Length; i++)
        {
            var claim = claims[i];
            if (subClaim is null && string.Equals(SubjectClaimType, claim.Type, StringComparison.Ordinal))
            {
                subClaim = claim;
                if (!string.IsNullOrEmpty(claim.Value))
                {
                    return claim;
                }
            }
            else if (nameIdentifierClaim is null && string.Equals(ClaimTypes.NameIdentifier, claim.Type, StringComparison.Ordinal))
            {
                nameIdentifierClaim = claim;
            }
            else if (upnClaim is null && string.Equals(ClaimTypes.Upn, claim.Type, StringComparison.Ordinal))
            {
                upnClaim = claim;
            }
        }

        return GetFirstNonemptyClaim(nameIdentifierClaim, upnClaim);
    }

    private static Claim? GetFirstNonemptyClaim(Claim? nameIdentifierClaim, Claim? upnClaim)
    {
        if (nameIdentifierClaim is not null && !string.IsNullOrEmpty(nameIdentifierClaim.Value))
        {
            return nameIdentifierClaim;
        }

        return upnClaim is not null && !string.IsNullOrEmpty(upnClaim.Value) ? upnClaim : null;
    }

    private static void AddClaims(IEnumerable<Claim> claims, ref Claim[] buffer, ref int count)
    {
        if (claims is List<Claim> claimsList)
        {
            var claimsSpan = CollectionsMarshal.AsSpan(claimsList);
            for (var i = 0; i < claimsSpan.Length; i++)
            {
                AddPooledItem(claimsSpan[i], ref buffer, ref count);
            }

            return;
        }

        foreach (var claim in claims)
        {
            AddPooledItem(claim, ref buffer, ref count);
        }
    }

    private static void AddPooledItem<T>(T item, ref T[] buffer, ref int count)
    {
        if (count == buffer.Length)
        {
            var replacement = ArrayPool<T>.Shared.Rent(buffer.Length * 2);
            buffer.AsSpan(0, count).CopyTo(replacement);
            buffer.AsSpan(0, count).Clear();
            ArrayPool<T>.Shared.Return(buffer);
            buffer = replacement;
        }

        buffer[count++] = item;
    }

    private static void ComputeSha256(Claim claim, Span<byte> destination)
    {
        Debug.Assert(destination.Length >= UserIdentifierSize);

        var totalSize =
            Measure7BitEncodedStringLength(claim.Type) +
            Measure7BitEncodedStringLength(claim.Value) +
            Measure7BitEncodedStringLength(claim.Issuer);

        byte[]? rentedBuffer = null;
        var buffer = totalSize <= StackAllocThreshold
            ? stackalloc byte[StackAllocThreshold]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(totalSize));

        try
        {
            var span = buffer[..totalSize];
            var offset = Write7BitEncodedString(span, claim.Type);
            offset += Write7BitEncodedString(span[offset..], claim.Value);
            offset += Write7BitEncodedString(span[offset..], claim.Issuer);
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

    private static void ComputeSha256(ReadOnlySpan<Claim> claims, Span<byte> destination)
    {
        Debug.Assert(destination.Length >= UserIdentifierSize);

        var totalSize = 0;
        for (var i = 0; i < claims.Length; i++)
        {
            totalSize +=
                Measure7BitEncodedStringLength(claims[i].Type) +
                Measure7BitEncodedStringLength(claims[i].Value) +
                Measure7BitEncodedStringLength(claims[i].Issuer);
        }

        byte[]? rentedBuffer = null;
        var buffer = totalSize <= StackAllocThreshold
            ? stackalloc byte[StackAllocThreshold]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(totalSize));

        try
        {
            var span = buffer[..totalSize];
            var offset = 0;
            for (var i = 0; i < claims.Length; i++)
            {
                offset += Write7BitEncodedString(span[offset..], claims[i].Type);
                offset += Write7BitEncodedString(span[offset..], claims[i].Value);
                offset += Write7BitEncodedString(span[offset..], claims[i].Issuer);
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

    private static int Measure7BitEncodedStringLength(string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        return Measure7BitEncodedUIntLength(byteCount) + byteCount;
    }

    private static int Measure7BitEncodedUIntLength(int value)
        => ((31 - System.Numerics.BitOperations.LeadingZeroCount((uint)value | 1)) / 7) + 1;

    // Preserve BinaryWriter's string framing because antiforgery tokens persist the resulting hash.
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
