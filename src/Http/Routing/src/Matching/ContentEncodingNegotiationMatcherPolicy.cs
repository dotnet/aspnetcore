// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class ContentEncodingNegotiationMatcherPolicy : NegotiationMatcherPolicy<ContentEncodingMetadata>
{
    internal static string HeaderName => "Accept-Encoding";

    private protected override bool HasMetadata(Endpoint endpoint) => endpoint.Metadata.GetMetadata<ContentEncodingMetadata>() != null;

    private protected override string? GetMetadataValue(Endpoint endpoint) => endpoint.Metadata.GetMetadata<ContentEncodingMetadata>()?.Value;

    private protected override StringValues GetNegotiationHeader(HttpContext httpContext) => httpContext.Request.Headers[HeaderName];

    private protected override bool IsDefaultMetadataValue(ReadOnlySpan<char> candidate) =>
        MemoryExtensions.Equals("identity".AsSpan(), candidate, StringComparison.OrdinalIgnoreCase) ||
        MemoryExtensions.Equals("*".AsSpan(), candidate, StringComparison.OrdinalIgnoreCase);

    private protected override double? GetMetadataQuality(Endpoint endpoint)
    {
        var metadata = endpoint.Metadata.GetMetadata<ContentEncodingMetadata>();
        return metadata?.Quality;
    }

    private protected override NegotiationPolicyJumpTable CreateTable(int exitDestination, (string negotiationValue, double quality, int destination)[] destinations, int noNegotiationHeaderDestination) => new ContentEncodingPolicyJumpTable(exitDestination, noNegotiationHeaderDestination, new ContentEncodingDestinationsLookUp(destinations));

    internal sealed class ContentEncodingPolicyJumpTable(int anyContentEncodingDestination, int noContentEncodingDestination, ContentEncodingDestinationsLookUp destinations) : NegotiationPolicyJumpTable("Accept-Encoding", anyContentEncodingDestination, noContentEncodingDestination)
    {
        private readonly ContentEncodingDestinationsLookUp _destinations = destinations;

        protected override int GetDestination(string? value) => _destinations.GetDestination(value);

        protected override double GetQuality(string? value) => _destinations.GetValueQuality(value);
    }

    internal sealed class ContentEncodingDestinationsLookUp
    {
        private readonly int _brotliDestination = -1;
        private readonly double _brotliQuality;
        private readonly int _gzipDestination = -1;
        private readonly double _gzipQuality;
        private readonly int _identityDestination = -1;
        private readonly double _identityQuality;
        private readonly Dictionary<string, (int destination, double quality)>? _extraDestinations;

        public ContentEncodingDestinationsLookUp((string contentEncoding, double quality, int destination)[] destinations)
        {
            for (var i = 0; i < destinations.Length; i++)
            {
                var (contentEncoding, quality, destination) = destinations[i];
                switch (contentEncoding.ToLowerInvariant())
                {
                    case "br":
                        _brotliDestination = destination;
                        _brotliQuality = quality;
                        break;
                    case "gzip":
                        _gzipDestination = destination;
                        _gzipQuality = quality;
                        break;
                    case "identity":
                        _identityDestination = destination;
                        _identityQuality = quality;
                        break;
                    default:
                        _extraDestinations ??= new Dictionary<string, (int destination, double quality)>(StringComparer.OrdinalIgnoreCase);
                        _extraDestinations.Add(contentEncoding, (destination, quality));
                        break;
                }
            }
        }

        public int GetDestination(string? negotiationValue)
        {
            // Specialcase the lookup based on the length of the negotiation value
            // to reduce the number of required comparisons needed to find a match.
            // The match will be validated after this selection.
            var (matchedEncoding, destination) = negotiationValue?.Length switch
            {
                2 => ("br", _brotliDestination),
                4 => ("gzip", _gzipDestination),
                8 => ("identity", _identityDestination),
                _ => (null, -1)
            };

            if (matchedEncoding != null && string.Equals(negotiationValue, matchedEncoding, StringComparison.OrdinalIgnoreCase))
            {
                return destination;
            }

            if (_extraDestinations != null && negotiationValue != null && _extraDestinations.TryGetValue(negotiationValue, out var extraDestination))
            {
                return extraDestination.destination;
            }

            return -1;
        }

        public double GetValueQuality(string? negotiationValue)
        {
            var (matchedEncoding, quality) = negotiationValue?.Length switch
            {
                2 => ("br", _brotliQuality),
                4 => ("gzip", _gzipQuality),
                8 => ("identity", _identityQuality),
                _ => (null, -1)
            };

            if (matchedEncoding != null && string.Equals(negotiationValue, matchedEncoding, StringComparison.OrdinalIgnoreCase))
            {
                return quality;
            }

            if (_extraDestinations != null && negotiationValue != null && _extraDestinations.TryGetValue(negotiationValue, out var extraDestination))
            {
                return extraDestination.quality;
            }

            return -1;
        }
    }
}
