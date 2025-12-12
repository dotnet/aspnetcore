// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Default implementation of <see cref="IClaimUidExtractor"/>.
/// </summary>
internal sealed class DefaultClaimUidExtractor : IClaimUidExtractor
{
    public bool TryExtractClaimUidBytes(ClaimsPrincipal claimsPrincipal, Span<byte> destination)
    {
        Debug.Assert(claimsPrincipal != null);

        var uniqueIdentifierParameters = GetUniqueIdentifierParameters(claimsPrincipal.Identities);
        if (uniqueIdentifierParameters is null)
        {
            return false;
        }

        ComputeSha256(uniqueIdentifierParameters, destination);
        return true;
    }

    public static IList<string>? GetUniqueIdentifierParameters(IEnumerable<ClaimsIdentity> claimsIdentities)
    {
        var identitiesList = claimsIdentities as List<ClaimsIdentity>;
        if (identitiesList == null)
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
            if (subClaim != null && !string.IsNullOrEmpty(subClaim.Value))
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
            if (nameIdentifierClaim != null && !string.IsNullOrEmpty(nameIdentifierClaim.Value))
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
            if (upnClaim != null && !string.IsNullOrEmpty(upnClaim.Value))
            {
                return
                [
                    upnClaim.Type,
                    upnClaim.Value,
                    upnClaim.Issuer
                ];
            }
        }

        // We do not understand any of the ClaimsIdentity instances, fallback on serializing all claims in every claims Identity.
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
            // No authenticated identities containing claims found.
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

    private static void ComputeSha256(IList<string> parameters, Span<byte> destination)
    {
        // Calculate total size needed for serialization
        var totalSize = 0;
        for (var i = 0; i < parameters.Count; i++)
        {
            var byteCount = System.Text.Encoding.UTF8.GetByteCount(parameters[i]);
            totalSize += byteCount.Measure7BitEncodedUIntLength() + byteCount;
        }

        // Use stackalloc for small buffers, otherwise rent
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
                offset += span.Slice(offset).Write7BitEncodedString(parameters[i]);
            }

            // Hash directly into destination (SHA256 output is always 32 bytes)
            SHA256.HashData(span.Slice(0, offset), destination);
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }
}
