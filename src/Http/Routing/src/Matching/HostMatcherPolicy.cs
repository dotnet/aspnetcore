// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// A <see cref="MatcherPolicy"/> that implements filtering and selection by
    /// the host header of a request.
    /// </summary>
    public sealed class HostMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy, IEndpointSelectorPolicy
    {
        private const string WildcardHost = "*";
        private const string WildcardPrefix = "*.";

        // Run after HTTP methods, but before 'default'.
        /// <inheritdoc />
        public override int Order { get; } = -100;

        /// <inheritdoc />
        public IComparer<Endpoint> Comparer { get; } = new HostMetadataEndpointComparer();

        bool INodeBuilderPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            return !ContainsDynamicEndpoints(endpoints) && AppliesToEndpointsCore(endpoints);
        }

        bool IEndpointSelectorPolicy.AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            // When the node contains dynamic endpoints we can't make any assumptions.
            var applies = ContainsDynamicEndpoints(endpoints);
            if (applies)
            {
                // Run for the side-effect of validating metadata.
                AppliesToEndpointsCore(endpoints);
            }

            return applies;
        }

        private static bool AppliesToEndpointsCore(IReadOnlyList<Endpoint> endpoints)
        {
            return endpoints.Any(e =>
            {
                var hosts = e.Metadata.GetMetadata<IHostMetadata>()?.Hosts;
                if (hosts == null || hosts.Count == 0)
                {
                    return false;
                }

                foreach (var host in hosts)
                {
                    // Don't run policy on endpoints that match everything
                    var key = CreateEdgeKey(host);
                    if (!key.MatchesAll)
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        /// <inheritdoc />
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

            for (var i = 0; i < candidates.Count; i++)
            {
                if (!candidates.IsValidCandidate(i))
                {
                    continue;
                }

                var hosts = candidates[i].Endpoint.Metadata.GetMetadata<IHostMetadata>()?.Hosts;
                if (hosts == null || hosts.Count == 0)
                {
                    // Can match any host.
                    continue;
                }

                var matched = false;
                var (requestHost, requestPort) = GetHostAndPort(httpContext);
                for (var j = 0; j < hosts.Count; j++)
                {
                    var host = hosts[j].AsSpan();
                    var port = ReadOnlySpan<char>.Empty;

                    // Split into host and port
                    var pivot = host.IndexOf(':');
                    if (pivot >= 0)
                    {
                        port = host.Slice(pivot + 1);
                        host = host.Slice(0, pivot);
                    }

                    if (host == null || MemoryExtensions.Equals(host, WildcardHost, StringComparison.OrdinalIgnoreCase))
                    {
                        // Can match any host
                    }
                    else if (
                        host.StartsWith(WildcardPrefix) &&

                        // Note that we only slice off the `*`. We want to match the leading `.` also.
                        MemoryExtensions.EndsWith(requestHost, host.Slice(WildcardHost.Length), StringComparison.OrdinalIgnoreCase))
                    {
                        // Matches a suffix wildcard.
                    }
                    else if (MemoryExtensions.Equals(requestHost, host, StringComparison.OrdinalIgnoreCase))
                    {
                        // Matches exactly
                    }
                    else
                    {
                        // If we get here then the host doesn't match.
                        continue;
                    }

                    if (MemoryExtensions.Equals(port, WildcardHost, StringComparison.OrdinalIgnoreCase))
                    {
                        // Port is a wildcard, we allow any port.
                    }
                    else if (port.Length > 0 && (!int.TryParse(port, out var parsed) || parsed != requestPort))
                    {
                        // If we get here then the port doesn't match.
                        continue;
                    }

                    matched = true;
                    break;
                }

                if (!matched)
                {
                    candidates.SetValidity(i, false);
                }
            }

            return Task.CompletedTask;
        }

        private static EdgeKey CreateEdgeKey(string host)
        {
            if (host == null)
            {
                return EdgeKey.WildcardEdgeKey;
            }

            var hostParts = host.Split(':');
            if (hostParts.Length == 1)
            {
                if (!string.IsNullOrEmpty(hostParts[0]))
                {
                    return new EdgeKey(hostParts[0], null);
                }
            }
            if (hostParts.Length == 2)
            {
                if (!string.IsNullOrEmpty(hostParts[0]))
                {
                    if (int.TryParse(hostParts[1], out var port))
                    {
                        return new EdgeKey(hostParts[0], port);
                    }
                    else if (string.Equals(hostParts[1], WildcardHost, StringComparison.Ordinal))
                    {
                        return new EdgeKey(hostParts[0], null);
                    }
                }
            }

            throw new InvalidOperationException($"Could not parse host: {host}");
        }

        /// <inheritdoc />
        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            // The algorithm here is designed to be preserve the order of the endpoints
            // while also being relatively simple. Preserving order is important.

            // First, build a dictionary of all of the hosts that are included
            // at this node.
            //
            // For now we're just building up the set of keys. We don't add any endpoints
            // to lists now because we don't want ordering problems.
            var edges = new Dictionary<EdgeKey, List<Endpoint>>();
            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];
                var hosts = endpoint.Metadata.GetMetadata<IHostMetadata>()?.Hosts.Select(h => CreateEdgeKey(h)).ToArray();
                if (hosts == null || hosts.Length == 0)
                {
                    hosts = new[] { EdgeKey.WildcardEdgeKey };
                }

                for (var j = 0; j < hosts.Length; j++)
                {
                    var host = hosts[j];
                    if (!edges.ContainsKey(host))
                    {
                        edges.Add(host, new List<Endpoint>());
                    }
                }
            }

            // Now in a second loop, add endpoints to these lists. We've enumerated all of
            // the states, so we want to see which states this endpoint matches.
            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];

                var endpointKeys = endpoint.Metadata.GetMetadata<IHostMetadata>()?.Hosts.Select(h => CreateEdgeKey(h)).ToArray() ?? Array.Empty<EdgeKey>();
                if (endpointKeys.Length == 0)
                {
                    // OK this means that this endpoint matches *all* hosts.
                    // So, loop and add it to all states.
                    foreach (var kvp in edges)
                    {
                        kvp.Value.Add(endpoint);
                    }
                }
                else
                {
                    // OK this endpoint matches specific hosts
                    foreach (var kvp in edges)
                    {
                        // The edgeKey maps to a possible request header value
                        var edgeKey = kvp.Key;

                        for (var j = 0; j < endpointKeys.Length; j++)
                        {
                            var endpointKey = endpointKeys[j];

                            if (edgeKey.Equals(endpointKey))
                            {
                                kvp.Value.Add(endpoint);
                                break;
                            }
                            else if (edgeKey.HasHostWildcard && endpointKey.HasHostWildcard &&
                                edgeKey.Port == endpointKey.Port && edgeKey.MatchHost(endpointKey.Host))
                            {
                                kvp.Value.Add(endpoint);
                                break;
                            }
                        }
                    }
                }
            }

            return edges
                .Select(kvp => new PolicyNodeEdge(kvp.Key, kvp.Value))
                .ToArray();
        }

        /// <inheritdoc />
        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            if (edges == null)
            {
                throw new ArgumentNullException(nameof(edges));
            }

            // Since our 'edges' can have wildcards, we do a sort based on how wildcard-ey they
            // are then then execute them in linear order.
            var ordered = edges
                .Select(e => (host: (EdgeKey)e.State, destination: e.Destination))
                .OrderBy(e => GetScore(e.host))
                .ToArray();

            return new HostPolicyJumpTable(exitDestination, ordered);
        }

        private static int GetScore(in EdgeKey key)
        {
            // Higher score == lower priority.
            if (key.MatchesHost && !key.HasHostWildcard && key.MatchesPort)
            {
                return 1; // Has host AND port, e.g. www.consoto.com:8080
            }
            else if (key.MatchesHost && !key.HasHostWildcard)
            {
                return 2; // Has host, e.g. www.consoto.com
            }
            else if (key.MatchesHost && key.MatchesPort)
            {
                return 3; // Has wildcard host AND port, e.g. *.consoto.com:8080
            }
            else if (key.MatchesHost)
            {
                return 4; // Has wildcard host, e.g. *.consoto.com
            }
            else if (key.MatchesPort)
            {
                return 5; // Has port, e.g. *:8080
            }
            else
            {
                return 6; // Has neither, e.g. *:* (or no metadata)
            }
        }

        private static (string host, int? port) GetHostAndPort(HttpContext httpContext)
        {
            var hostString = httpContext.Request.Host;
            if (hostString.Port != null)
            {
                return (hostString.Host, hostString.Port);
            }
            else if (string.Equals("https", httpContext.Request.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return (hostString.Host, 443);
            }
            else if (string.Equals("http", httpContext.Request.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return (hostString.Host, 80);
            }
            else
            {
                return (hostString.Host, null);
            }
        }

        private class HostMetadataEndpointComparer : EndpointMetadataComparer<IHostMetadata>
        {
            protected override int CompareMetadata(IHostMetadata? x, IHostMetadata? y)
            {
                // Ignore the metadata if it has an empty list of hosts.
                return base.CompareMetadata(
                    x?.Hosts.Count > 0 ? x : null,
                    y?.Hosts.Count > 0 ? y : null);
            }
        }

        private class HostPolicyJumpTable : PolicyJumpTable
        {
            private readonly (EdgeKey host, int destination)[] _destinations;
            private readonly int _exitDestination;

            public HostPolicyJumpTable(int exitDestination, (EdgeKey host, int destination)[] destinations)
            {
                _exitDestination = exitDestination;
                _destinations = destinations;
            }

            public override int GetDestination(HttpContext httpContext)
            {
                // HostString can allocate when accessing the host or port
                // Store host and port locally and reuse
                var (host, port) = GetHostAndPort(httpContext);

                var destinations = _destinations;
                for (var i = 0; i < destinations.Length; i++)
                {
                    var destination = destinations[i];

                    if ((!destination.host.MatchesPort || destination.host.Port == port) &&
                        destination.host.MatchHost(host))
                    {
                        return destination.destination;
                    }
                }

                return _exitDestination;
            }
        }

        private readonly struct EdgeKey : IEquatable<EdgeKey>, IComparable<EdgeKey>, IComparable
        {
            internal static readonly EdgeKey WildcardEdgeKey = new EdgeKey(null, null);

            public readonly int? Port;
            public readonly string Host;

            private readonly string? _wildcardEndsWith;

            public EdgeKey(string? host, int? port)
            {
                Host = host ?? WildcardHost;
                Port = port;

                HasHostWildcard = Host.StartsWith(WildcardPrefix, StringComparison.Ordinal);
                _wildcardEndsWith = HasHostWildcard ? Host.Substring(1) : null;
            }

            public bool HasHostWildcard { get; }

            public bool MatchesHost => !string.Equals(Host, WildcardHost, StringComparison.Ordinal);

            public bool MatchesPort => Port != null;

            public bool MatchesAll => !MatchesHost && !MatchesPort;

            public int CompareTo(EdgeKey other)
            {
                var result = Comparer<string>.Default.Compare(Host, other.Host);
                if (result != 0)
                {
                    return result;
                }

                return Comparer<int?>.Default.Compare(Port, other.Port);
            }

            public int CompareTo(object? obj)
            {
                return CompareTo((EdgeKey)obj!);
            }

            public bool Equals(EdgeKey other)
            {
                return string.Equals(Host, other.Host, StringComparison.Ordinal) && Port == other.Port;
            }

            public bool MatchHost(string host)
            {
                if (MatchesHost)
                {
                    if (HasHostWildcard)
                    {
                        return host.EndsWith(_wildcardEndsWith!, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        return string.Equals(host, Host, StringComparison.OrdinalIgnoreCase);
                    }
                }

                return true;
            }


            public override int GetHashCode()
            {
                return (Host?.GetHashCode() ?? 0) ^ (Port?.GetHashCode() ?? 0);
            }

            public override bool Equals(object? obj)
            {
                if (obj is EdgeKey key)
                {
                    return Equals(key);
                }

                return false;
            }

            public override string ToString()
            {
                return $"{Host}:{Port?.ToString(CultureInfo.InvariantCulture) ?? WildcardHost}";
            }
        }
    }
}
