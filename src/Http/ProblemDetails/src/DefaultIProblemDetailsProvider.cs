// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultProblemDetailsEndpointProvider : IProblemDetailsProvider
{
    private readonly ProblemDetailsOptions _options;
    private readonly IProblemDetailsWriter[] _writers;

    public DefaultProblemDetailsEndpointProvider(
        IEnumerable<IProblemDetailsWriter> writers,
        IOptions<ProblemDetailsOptions> options)
    {
        _options = options.Value;
        _writers = writers.ToArray();
    }

    public IProblemDetailsWriter? GetWriter(HttpContext context, EndpointMetadataCollection? currentMetadata = null, bool isRouting = false)
    {
        if (IsEnabled(context.Response.StatusCode, isRouting))
        {
            currentMetadata ??= context.GetEndpoint()?.Metadata;

            for (var i = 0; i < _writers.Length; i++)
            {
                if (_writers[i].CanWrite(context, currentMetadata, isRouting: isRouting))
                {
                    return _writers[i];
                }
            }
        }

        return null;
    }

    public bool IsEnabled(int statusCode, bool isRouting = false)
    {
        if (_options.AllowedMapping == MappingOptions.Unspecified)
        {
            return false;
        }

        return isRouting ? _options.AllowedMapping.HasFlag(MappingOptions.RoutingFailures) : statusCode switch
        {
            >= 400 and <= 499 => _options.AllowedMapping.HasFlag(MappingOptions.ClientErrors),
            >= 500 => _options.AllowedMapping.HasFlag(MappingOptions.Exceptions),
            _ => false,
        };
    }
}
