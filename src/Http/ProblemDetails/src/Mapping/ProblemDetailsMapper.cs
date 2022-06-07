// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

internal class ProblemDetailsMapper
{
    private readonly IProblemDetailsMapPolicy[] _matchPolicies;
    private readonly ProblemDetailsOptions _options;

    private static readonly MediaTypeHeaderValue _problemMediaType = new("application/problem+*");

    public ProblemDetailsMapper(
        IEnumerable<IProblemDetailsMapPolicy> matchPolicies,
        IOptions<ProblemDetailsOptions> options)
    {
        _matchPolicies = matchPolicies.ToArray();
        _options = options.Value;
    }

    public bool CanMap(
        HttpContext context,
        EndpointMetadataCollection? metadata = null,
        int? statusCode = null,
        bool isRouting = false)
    {
        metadata ??= context.GetEndpoint()?.Metadata;

        if (!_options.IsEnabled(statusCode ?? context.Response.StatusCode, isRouting))
        {
            return false;
        }

        var headers = context.Request.GetTypedHeaders();
        var acceptHeader = headers.Accept;

        if (acceptHeader != null &&
            !acceptHeader.Any(h => _problemMediaType.IsSubsetOf(h)))
        {
            return false;
        }

        // What if we don't have the endpoint (eg. Routing)

        var responseType = metadata?.GetMetadata<IProducesErrorResponseMetadata>();
        if (responseType == null || !typeof(ProblemDetails).IsAssignableFrom(responseType.Type))
        {
            return false;
        }

        for (var i = _matchPolicies.Length; i > 0; i--)
        {
            if (!_matchPolicies[i - 1].CanMap(context, metadata, statusCode, isRouting))
            {
                return false;
            }
        }

        return true;
    }
}
