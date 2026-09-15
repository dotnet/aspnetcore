// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Antiforgery.Microbenchmarks.Benchmarks;

[AspNetCoreBenchmark]
public class ClaimUidExtractorBenchmark
{
    private readonly DefaultClaimUidExtractor _extractor = new();
    private readonly byte[] _destination = new byte[SHA256.HashSizeInBytes];
    private ClaimsPrincipal _principal = null!;

    public IEnumerable<ClaimUidScenario> Scenarios =>
    [
        ClaimUidScenario.Subject,
        ClaimUidScenario.NameIdentifier,
        ClaimUidScenario.SmallClaimsFallback,
        ClaimUidScenario.MultipleIdentities,
        ClaimUidScenario.LargeClaimsFallback,
    ];

    [ParamsSource(nameof(Scenarios))]
    public ClaimUidScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _principal = Scenario switch
        {
            ClaimUidScenario.Subject => CreatePrincipal([new Claim("sub", "subject")]),
            ClaimUidScenario.NameIdentifier => CreatePrincipal(
            [
                new Claim("custom-1", "value-1"),
                new Claim("custom-2", "value-2"),
                new Claim("custom-3", "value-3"),
                new Claim(ClaimTypes.NameIdentifier, "name-identifier"),
                new Claim("custom-4", "value-4"),
                new Claim("custom-5", "value-5"),
                new Claim("custom-6", "value-6"),
                new Claim("custom-7", "value-7"),
            ]),
            ClaimUidScenario.SmallClaimsFallback => CreatePrincipal(
            [
                new Claim("custom-4", "value-4"),
                new Claim("custom-3", "value-3"),
                new Claim("custom-2", "value-2"),
                new Claim("custom-1", "value-1"),
            ]),
            ClaimUidScenario.MultipleIdentities => new ClaimsPrincipal(
            [
                new ClaimsIdentity([new Claim("sub", "ignored")]),
                new ClaimsIdentity([new Claim(ClaimTypes.Upn, "user@example.com")], "Test"),
            ]),
            ClaimUidScenario.LargeClaimsFallback => CreatePrincipal(CreateLargeClaims()),
            _ => throw new InvalidOperationException(),
        };

        var previousIdentifier = new byte[SHA256.HashSizeInBytes];
        var currentIdentifier = new byte[SHA256.HashSizeInBytes];
        var previousResult = PreviousClaimUidExtractor.TryExtractClaimUidBytes(_principal, previousIdentifier);
        var currentResult = _extractor.TryExtractClaimUidBytes(_principal, currentIdentifier);
        if (previousResult != currentResult || !previousIdentifier.AsSpan().SequenceEqual(currentIdentifier))
        {
            throw new InvalidOperationException("The benchmark implementations produced different identifiers.");
        }
    }

    [Benchmark(Baseline = true)]
    public bool Previous()
        => PreviousClaimUidExtractor.TryExtractClaimUidBytes(_principal, _destination);

    [Benchmark]
    public bool Current()
        => _extractor.TryExtractClaimUidBytes(_principal, _destination);

    private static ClaimsPrincipal CreatePrincipal(Claim[] claims)
        => new(new ClaimsIdentity(claims, "Test"));

    private static Claim[] CreateLargeClaims()
    {
        var claims = new Claim[64];
        for (var i = 0; i < claims.Length; i++)
        {
            claims[i] = new Claim($"custom-{claims.Length - i:D2}", new string((char)('a' + (i % 26)), 1024));
        }

        return claims;
    }

    public enum ClaimUidScenario
    {
        Subject,
        NameIdentifier,
        SmallClaimsFallback,
        MultipleIdentities,
        LargeClaimsFallback,
    }

    private static class PreviousClaimUidExtractor
    {
        public static bool TryExtractClaimUidBytes(ClaimsPrincipal principal, Span<byte> destination)
        {
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
                    return [subClaim.Type, subClaim.Value, subClaim.Issuer];
                }

                var nameIdentifierClaim = identity.FindFirst(
                    claim => string.Equals(ClaimTypes.NameIdentifier, claim.Type, StringComparison.Ordinal));
                if (nameIdentifierClaim is not null && !string.IsNullOrEmpty(nameIdentifierClaim.Value))
                {
                    return [nameIdentifierClaim.Type, nameIdentifierClaim.Value, nameIdentifierClaim.Issuer];
                }

                var upnClaim = identity.FindFirst(
                    claim => string.Equals(ClaimTypes.Upn, claim.Type, StringComparison.Ordinal));
                if (upnClaim is not null && !string.IsNullOrEmpty(upnClaim.Value))
                {
                    return [upnClaim.Type, upnClaim.Value, upnClaim.Issuer];
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
            var totalSize = 0;
            for (var i = 0; i < parameters.Count; i++)
            {
                var byteCount = Encoding.UTF8.GetByteCount(parameters[i]);
                totalSize += byteCount.Measure7BitEncodedUIntLength() + byteCount;
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
                    offset += span[offset..].Write7BitEncodedString(parameters[i]);
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
    }
}
