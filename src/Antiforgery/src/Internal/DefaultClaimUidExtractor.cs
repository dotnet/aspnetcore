// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Default implementation of <see cref="IClaimUidExtractor"/>.
/// </summary>
internal sealed class DefaultClaimUidExtractor : IClaimUidExtractor
{
    private readonly ObjectPool<AntiforgerySerializationContext> _pool;

    public DefaultClaimUidExtractor(ObjectPool<AntiforgerySerializationContext> pool)
    {
        _pool = pool;
    }

    /// <inheritdoc />
    public void ExtractClaimUid(ClaimsPrincipal claimsPrincipal, Span<byte> destination, out int bytesWritten)
    {
        Debug.Assert(claimsPrincipal != null);

        var uniqueIdentifierParameters = GetUniqueIdentifierParameters(claimsPrincipal.Identities);
        if (uniqueIdentifierParameters == null)
        {
            // No authenticated identities containing claims found.
            bytesWritten = 0;
            return;
        }

        ComputeSha256(uniqueIdentifierParameters, destination, out bytesWritten);
    }

    /// <inheritdoc />
    public string? ExtractClaimUid(ClaimsPrincipal claimsPrincipal)
    {
        Debug.Assert(claimsPrincipal != null);

        var uniqueIdentifierParameters = GetUniqueIdentifierParameters(claimsPrincipal.Identities);
        if (uniqueIdentifierParameters == null)
        {
            // No authenticated identities containing claims found.
            return null;
        }

        // todo skip allocations here as well
        var claimUidBytes = ComputeSha256(uniqueIdentifierParameters);
        return Convert.ToBase64String(claimUidBytes);
    }

    public static IList<string>? GetUniqueIdentifierParameters(IEnumerable<ClaimsIdentity> claimsIdentities)
    {
        var identitiesList = claimsIdentities as List<ClaimsIdentity>;
        if (identitiesList == null)
        {
            identitiesList = new List<ClaimsIdentity>(claimsIdentities);
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
                return new string[]
                {
                        subClaim.Type,
                        subClaim.Value,
                        subClaim.Issuer
                };
            }

            var nameIdentifierClaim = identity.FindFirst(
                claim => string.Equals(ClaimTypes.NameIdentifier, claim.Type, StringComparison.Ordinal));
            if (nameIdentifierClaim != null && !string.IsNullOrEmpty(nameIdentifierClaim.Value))
            {
                return new string[]
                {
                        nameIdentifierClaim.Type,
                        nameIdentifierClaim.Value,
                        nameIdentifierClaim.Issuer
                };
            }

            var upnClaim = identity.FindFirst(
                claim => string.Equals(ClaimTypes.Upn, claim.Type, StringComparison.Ordinal));
            if (upnClaim != null && !string.IsNullOrEmpty(upnClaim.Value))
            {
                return new string[]
                {
                        upnClaim.Type,
                        upnClaim.Value,
                        upnClaim.Issuer
                };
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

    private static void ComputeSha256(IEnumerable<string> parameters, Span<byte> output, out int bytesWritten)
    {
        int estimatedSize = 0;
        foreach (var param in parameters)
        {
            int byteCount = Encoding.UTF8.GetByteCount(param);
            estimatedSize += 5 + byteCount; // max 5 bytes for 7-bit length + content
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(estimatedSize);

        try
        {
            int offset = 0;
            foreach (var param in parameters)
            {
                int byteCount = Encoding.UTF8.GetByteCount(param);

                // Write 7-bit encoded length prefix
                offset += buffer.AsSpan(offset).Write7BitEncodedInt(byteCount);

                // Write UTF-8 bytes
                Encoding.UTF8.GetBytes(param, buffer.AsSpan(offset, byteCount));
                offset += byteCount;
            }

            SHA256.TryHashData(buffer.AsSpan(0, offset), output, out bytesWritten);
            Debug.Assert(bytesWritten == 32);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private byte[] ComputeSha256(IEnumerable<string> parameters)
    {
        var serializationContext = _pool.Get();

        try
        {
            var writer = serializationContext.Writer;
            foreach (string parameter in parameters)
            {
                writer.Write(parameter); // also writes the length as a prefix; unambiguous
            }

            writer.Flush();

            bool success = serializationContext.Stream.TryGetBuffer(out ArraySegment<byte> buffer);
            if (!success)
            {
                throw new InvalidOperationException();
            }

            var bytes = SHA256.HashData(buffer);

            return bytes;
        }
        finally
        {
            _pool.Return(serializationContext);
        }
    }
}
