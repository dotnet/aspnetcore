// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

internal sealed class EndpointNameAddressScheme : IEndpointAddressScheme<string>, IDisposable
{
    private readonly DataSourceDependentCache<FrozenDictionary<string, Endpoint[]>> _cache;

    public EndpointNameAddressScheme(EndpointDataSource dataSource)
    {
        _cache = new DataSourceDependentCache<FrozenDictionary<string, Endpoint[]>>(dataSource, Initialize);
    }

    // Internal for tests
    internal FrozenDictionary<string, Endpoint[]> Entries => _cache.EnsureInitialized();

    public IEnumerable<Endpoint> FindEndpoints(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Capture the current value of the cache
        var entries = Entries;

        entries.TryGetValue(address, out var result);
        return result ?? Array.Empty<Endpoint>();
    }

    private static FrozenDictionary<string, Endpoint[]> Initialize(IReadOnlyList<Endpoint> endpoints)
    {
        // Collect duplicates as we go, blow up on startup if we find any.
        var hasDuplicates = false;

        var entries = new Dictionary<string, Endpoint[]>(StringComparer.Ordinal);
        for (var i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];

            var endpointName = GetEndpointName(endpoint);
            if (endpointName == null)
            {
                continue;
            }

            if (!entries.TryGetValue(endpointName, out var existing))
            {
                // This isn't a duplicate (so far)
                entries[endpointName] = new[] { endpoint };
                continue;
            }
            else
            {
                // Ok this is a duplicate, because we have two endpoints with the same name. Collect all the data
                // so we can throw an exception. The extra allocations here don't matter since this is an exceptional case.
                hasDuplicates = true;

                var newEntry = new Endpoint[existing.Length + 1];
                Array.Copy(existing, newEntry, existing.Length);
                newEntry[existing.Length] = endpoint;
                entries[endpointName] = newEntry;
            }
        }

        if (!hasDuplicates)
        {
            // No duplicates, success!
            return entries.ToFrozenDictionary(StringComparer.Ordinal);
        }

        // OK we need to report some duplicates.
        var builder = new StringBuilder();
        builder.AppendLine(Resources.DuplicateEndpointNameHeader);

        foreach (var group in entries)
        {
            if (group.Key is not null && group.Value.Length > 1)
            {
                builder.AppendLine();
                builder.AppendLine(Resources.FormatDuplicateEndpointNameEntry(group.Key));

                foreach (var endpoint in group.Value)
                {
                    builder.AppendLine(endpoint.DisplayName);
                }
            }
        }

        throw new InvalidOperationException(builder.ToString());

        static string? GetEndpointName(Endpoint endpoint)
        {
            if (endpoint.Metadata.GetMetadata<ISuppressLinkGenerationMetadata>()?.SuppressLinkGeneration == true)
            {
                // Skip anything that's suppressed for linking.
                return null;
            }

            return endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}
