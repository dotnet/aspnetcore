// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing
{
    internal sealed class HeaderMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, IEndpointSelectorPolicy
    {
        /// <inheritdoc/>
        // Run after HttpMethodMatcherPolicy (-1000) and HostMatcherPolicy (-100), but before default (0)
        public override int Order => -50;

        /// <inheritdoc/>
        public IComparer<Endpoint> Comparer => new HeaderMetadataEndpointComparer();

        /// <inheritdoc/>
        bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            _ = endpoints ?? throw new ArgumentNullException(nameof(endpoints));

            // When the node contains dynamic endpoints we can't make any assumptions.
            if (ContainsDynamicEndpoints(endpoints))
            {
                return true;
            }

            return AppliesToEndpointsCore(endpoints);
        }

        /// <inheritdoc/>
        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            _ = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            _ = candidates ?? throw new ArgumentNullException(nameof(candidates));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!candidates.IsValidCandidate(i))
                {
                    continue;
                }

                var metadata = candidates[i].Endpoint.Metadata.GetMetadata<IHeaderMetadata>();
                var metadataHeaderName = metadata?.HeaderName;
                if (string.IsNullOrEmpty(metadataHeaderName))
                {
                    // Can match any request
                    continue;
                }

                var metadataHeaderValues = metadata.HeaderValues;
                int maxValuesToInspect = metadata.MaximumValuesToInspect;

                var matched = false;
                if (httpContext.Request.Headers.TryGetValue(metadataHeaderName, out var requestHeaderValues))
                {
                    if (metadataHeaderValues?.Count == 0)
                    {
                        // We were asked to match as long as the header exists, and it *does* exist
                        matched = true;
                    }
                    else
                    {
                        var comparisonFunc = GetComparisonFunc(metadata.ValueMatchMode);
                        var stringComparison = metadata.ValueIgnoresCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                        for (int j = 0; j < metadataHeaderValues.Count; j++)
                        {
                            for (int k = 0; k < requestHeaderValues.Count && k < maxValuesToInspect; k++)
                            {
                                if (comparisonFunc(requestHeaderValues[k], metadataHeaderValues[j], stringComparison))
                                {
                                    matched = true;
                                    break;
                                }
                            }

                            if (matched)
                            {
                                break;
                            }
                        }
                    }
                }

                if (!matched)
                {
                    candidates.SetValidity(i, false);
                }
            }

            return Task.CompletedTask;
        }

        private static Func<string, string, StringComparison, bool> GetComparisonFunc(HeaderValueMatchMode matchMode)
        {
            return matchMode switch
            {
                HeaderValueMatchMode.Prefix => Prefix,
                _ => Exact,
            };

            static bool Exact(string a, string b, StringComparison comparison) => string.Equals(a, b, comparison);
            static bool Prefix(string a, string b, StringComparison comparison) => a != null && b != null && a.StartsWith(b, comparison);
        }

        private static bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
        {
            return endpoints.Any(e =>
            {
                var metadata = e.Metadata.GetMetadata<IHeaderMetadata>();
                return !string.IsNullOrEmpty(metadata?.HeaderName);
            });
        }

        private class HeaderMetadataEndpointComparer : EndpointMetadataComparer<IHeaderMetadata>
        {
            protected override int CompareMetadata(IHeaderMetadata x, IHeaderMetadata y)
            {
                var xPresent = !string.IsNullOrEmpty(x?.HeaderName);
                var yPresent = !string.IsNullOrEmpty(y?.HeaderName);

                // 1. First, sort by presence of metadata
                if (!xPresent && yPresent)
                {
                    // y is more specific
                    return 1;
                }
                else if (xPresent && !yPresent)
                {
                    // x is more specific
                    return -1;
                }
                else if (!xPresent && !yPresent)
                {
                    // None of the policies have any effect, so they have same specificity.
                    return 0;
                }

                // 2. Then, by whether we seek specific header values or just header presence
                var xCount = x.HeaderValues?.Count ?? 0;
                var yCount = y.HeaderValues?.Count ?? 0;

                if (xCount == 0 && yCount > 0)
                {
                    // y is more specific, as *only it* looks for specific header values
                    return 1;
                }
                else if (xCount > 0 && yCount == 0)
                {
                    // x is more specific, as *only it* looks for specific header values
                    return -1;
                }
                else if (xCount == 0 && yCount == 0)
                {
                    // Same specificity, they both only check eader presence
                    return 0;
                }

                // 3. Then, by value match mode (Exact Vs. Prefix)
                if (x.ValueMatchMode != HeaderValueMatchMode.Exact && y.ValueMatchMode == HeaderValueMatchMode.Exact)
                {
                    // y is more specific, as *only it* does exact match
                    return 1;
                }
                else if (x.ValueMatchMode == HeaderValueMatchMode.Exact && y.ValueMatchMode != HeaderValueMatchMode.Exact)
                {
                    // x is more specific, as *only it* does exact match
                    return -1;
                }

                // 4. Then, by case sensitivity
                if (x.ValueIgnoresCase && !y.ValueIgnoresCase)
                {
                    // y is more specific, as *only it* is case sensitive
                    return 1;
                }
                else if (!x.ValueIgnoresCase && y.ValueIgnoresCase)
                {
                    // x is more specific, as *only it* is case sensitive
                    return -1;
                }

                // 5. then, by how many headers are inspected
                if (x.MaximumValuesToInspect > y.MaximumValuesToInspect)
                {
                    // y is more specific, as it needs a match among a smaller number of incoming headers.
                    return 1;
                }
                else if (x.MaximumValuesToInspect < y.MaximumValuesToInspect)
                {
                    // x is more specific, as it needs a match among a smaller number of incoming headers.
                    return -1;
                }

                // They have equal specificity
                return 0;
            }
        }
    }
}
