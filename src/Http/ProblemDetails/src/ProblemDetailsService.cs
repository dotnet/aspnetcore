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

    public Task WriteAsync(
        HttpContext context,
        EndpointMetadataCollection? additionalMetadata = null,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        if (_options.AllowedProblemTypes != ProblemDetailsTypes.None && _writers is { Length: > 0 })
        {
            var problemStatusCode = statusCode ?? context.Response.StatusCode;
            var calculatedProblemType = CalculateProblemType(problemStatusCode, context.GetEndpoint()?.Metadata, additionalMetadata);

            if ((_options.AllowedProblemTypes & calculatedProblemType) != ProblemDetailsTypes.None &&
                GetWriter(context, additionalMetadata) is { } writer)
            {
                return writer.WriteAsync(
                    context,
                    statusCode,
                    title,
                    type,
                    detail,
                    instance,
                    extensions);
            }
        }

        return Task.CompletedTask;
    }

    // Internal for testing
    internal IProblemDetailsWriter? GetWriter(HttpContext context, EndpointMetadataCollection? additionalMetadata)
    {
        for (var i = 0; i < _writers.Length; i++)
        {
            if (_writers[i].CanWrite(context, additionalMetadata))
            {
                return _writers[i];
            }
        }

        return null;
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
