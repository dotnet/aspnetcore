// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed class ProblemDetailsService : IProblemDetailsService
{
    private readonly IProblemDetailsWriter[] _writers;
    private readonly ProblemDetailsOptions _options;

    public ProblemDetailsService(
        IEnumerable<IProblemDetailsWriter> writers,
        IOptions<ProblemDetailsOptions> options)
    {
        _writers = writers.ToArray();
        _options = options.Value;
    }

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        if (_options.AllowedProblemTypes != ProblemDetailsTypes.None && _writers is { Length: > 0 })
        {
            var problemStatusCode = context.ProblemDetails?.Status ?? context.HttpContext.Response.StatusCode;
            var calculatedProblemType = CalculateProblemType(problemStatusCode, context.HttpContext.GetEndpoint()?.Metadata, context.AdditionalMetadata);

            if ((_options.AllowedProblemTypes & calculatedProblemType) != ProblemDetailsTypes.None)
            {
                var counter = 0;
                var responseHasStarted = context.HttpContext.Response.HasStarted;

                while (counter < _writers.Length && !responseHasStarted)
                {
                    responseHasStarted = await _writers[counter].WriteAsync(context);
                    counter++;
                }
            }
        }
    }

    // internal for testing
    internal static ProblemDetailsTypes CalculateProblemType(
        int statusCode,
        EndpointMetadataCollection? metadataCollection,
        EndpointMetadataCollection? additionalMetadata)
    {
        if (statusCode < 400)
        {
            return ProblemDetailsTypes.None;
        }

        ProblemDetailsTypes? statusCodeProblemType = null;
        ProblemDetailsTypes? generalProblemType = null;

        void SetProblemType(IProblemDetailsMetadata metadata)
        {
            if (!metadata.StatusCode.HasValue)
            {
                generalProblemType = metadata.ProblemType;
            }
            else if (statusCode == metadata.StatusCode)
            {
                statusCodeProblemType = metadata.ProblemType;
            }
        }

        if (metadataCollection?.GetOrderedMetadata<IProblemDetailsMetadata>() is { Count: > 0 } problemDetailsCollection)
        {
            for (var i = 0; i < problemDetailsCollection.Count; i++)
            {
                SetProblemType(problemDetailsCollection[i]);
            }
        }

        if (additionalMetadata?.GetOrderedMetadata<IProblemDetailsMetadata>() is { Count: > 0 } additionalProblemDetailsCollection)
        {
            for (var i = 0; i < additionalProblemDetailsCollection.Count; i++)
            {
                SetProblemType(additionalProblemDetailsCollection[i]);
            }
        }

        var problemTypeFromMetadata = statusCodeProblemType ?? generalProblemType ?? ProblemDetailsTypes.None;
        var expectedProblemType = statusCode >= 500 ? ProblemDetailsTypes.Server : ProblemDetailsTypes.Client;

        var problemType = problemTypeFromMetadata & expectedProblemType;
        return problemType != ProblemDetailsTypes.None ?
            problemType :
            // We need to special case Routing, since it could generate any status code
            problemTypeFromMetadata & ProblemDetailsTypes.Routing;
    }
}
