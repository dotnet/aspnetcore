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

    public bool IsEnabled(ProblemTypes type)
    {
        if (_options.AllowedProblemTypes == ProblemTypes.Unspecified)
        {
            return false;
        }

        return _options.AllowedProblemTypes.HasFlag(type);
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
        static ProblemTypes CalculateProblemType(
            HttpContext context,
            EndpointMetadataCollection? metadataCollection,
            int statusCode)
        {
            var problemMetadata = metadataCollection?.GetMetadata<IProblemMetadata>() ??
                context.GetEndpoint()?.Metadata.GetMetadata<IProblemMetadata>();

            if (problemMetadata != null)
            {
                var expectedProblemType = statusCode >= 500 ? ProblemTypes.Server : ProblemTypes.Client;

                if (problemMetadata.StatusCode == statusCode)
                {
                    return problemMetadata.ProblemType;
                }
                else if (problemMetadata.StatusCode == null &&
                    problemMetadata.ProblemType.HasFlag(expectedProblemType))
                {
                    return expectedProblemType;
                }
            }

            return ProblemTypes.Unspecified;
        }

        var problemStatusCode = statusCode ?? context.Response.StatusCode;
        var problemType = CalculateProblemType(context, additionalMetadata, statusCode: problemStatusCode);

        if (IsEnabled(problemType) && GetWriter(context) is { } writer)
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

        return Task.CompletedTask;
    }

    // Internal for testing
    internal IProblemDetailsWriter? GetWriter(HttpContext context)
    {
        for (var i = 0; i < _writers.Length; i++)
        {
            if (_writers[i].CanWrite(context))
            {
                return _writers[i];
            }
        }

        return null;
    }
}
