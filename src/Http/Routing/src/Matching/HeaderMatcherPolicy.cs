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
    public sealed class HeaderMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, IEndpointSelectorPolicy
    {
        private readonly IOptionsMonitor<HeaderMatcherPolicyOptions> options;

        public HeaderMatcherPolicy(IOptionsMonitor<HeaderMatcherPolicyOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.options = options;
        }

        /// <inheritdoc/>
        // Run after HttpMethodMatcherPolicy (-1000) and HostMatcherPolicy (-100), but before default (0)
        public override int Order => -50;

        /// <inheritdoc/>
        public IComparer<Endpoint> Comparer => new HeaderMetadataEndpointComparer();

        /// <inheritdoc/>
        bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            // When the node contains dynamic endpoints we can't make any assumptions.
            if (MatcherPolicy.ContainsDynamicEndpoints(endpoints))
            {
                return true;
            }

            return AppliesToEndpointsCore(endpoints);
        }

        /// <inheritdoc/>
        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!candidates.IsValidCandidate(i))
                {
                    continue;
                }

                var metadata = candidates[i].Endpoint.Metadata.GetMetadata<IHeaderMetadata>();
                string metadataHeaderName = metadata?.HeaderName;
                if (string.IsNullOrEmpty(metadataHeaderName))
                {
                    // Can match any request
                    continue;
                }

                var metadataHeaderValues = metadata.HeaderValues;

                bool matched = false;
                if (httpContext.Request.Headers.TryGetValue(metadataHeaderName, out var requestHeaderValues))
                {
                    if (metadataHeaderValues.Count == 0)
                    {
                        // Match as long as header exists, and header *does* exist
                        matched = true;
                    }
                    else
                    {
                        var comparisonFunc = GetComparisonFunc(metadata.HeaderValueMatchMode);
                        var stringComparison = metadata.HeaderValueStringComparison;
                        int maxRequestHeadersToInspect = this.options.CurrentValue.MaximumRequestHeaderValuesToInspect;
                        for (int j = 0; j < metadataHeaderValues.Count; j++)
                        {
                            for (int k = 0; k < requestHeaderValues.Count && k < maxRequestHeadersToInspect; k++)
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
                bool xPresent = !string.IsNullOrEmpty(x?.HeaderName);
                bool yPresent = !string.IsNullOrEmpty(y?.HeaderName);
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

                int xCount = x.HeaderValues?.Count ?? 0;
                int yCount = y.HeaderValues?.Count ?? 0;

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

                // They have equal specificity
                return 0;
            }
        }
    }
}
