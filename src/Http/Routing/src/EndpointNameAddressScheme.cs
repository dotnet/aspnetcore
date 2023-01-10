// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

internal sealed class EndpointNameAddressScheme : IEndpointAddressScheme<string>, IDisposable
{
    private readonly DataSourceDependentCache<Dictionary<string, Endpoint[]>> _cache;

    public EndpointNameAddressScheme(EndpointDataSource dataSource)
    {
        _cache = new DataSourceDependentCache<Dictionary<string, Endpoint[]>>(dataSource, Initialize);
    }

    // Internal for tests
    internal Dictionary<string, Endpoint[]> Entries => _cache.EnsureInitialized();

    public IEnumerable<Endpoint> FindEndpoints(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Capture the current value of the cache
        var entries = Entries;

        entries.TryGetValue(address, out var result);
        return result ?? Array.Empty<Endpoint>();
    }

    private static Dictionary<string, Endpoint[]> Initialize(IReadOnlyList<Endpoint> endpoints)
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

            // Ok this is a duplicate, because we have two endpoints with the same name. Bail out, because we
            // are just going to throw, we don't need to finish collecting data.
            hasDuplicates = true;
            break;
        }

        if (!hasDuplicates)
        {
            // No duplicates, success!
            return entries;
        }

        // OK we need to report some duplicates.
        var duplicates = endpoints
            .GroupBy(GetEndpointName)
            .Where(g => g.Key != null && g.Count() > 1);

        var builder = new StringBuilder();
        builder.AppendLine(Resources.DuplicateEndpointNameHeader);

        foreach (var group in duplicates)
        {
            builder.AppendLine();
            builder.AppendLine(Resources.FormatDuplicateEndpointNameEntry(group.Key));

            foreach (var endpoint in group)
            {
                builder.AppendLine(endpoint.DisplayName);
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
