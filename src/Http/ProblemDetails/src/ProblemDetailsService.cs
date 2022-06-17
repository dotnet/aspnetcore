// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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
        EndpointMetadataCollection? currentMetadata = null,
        bool isRouting = false,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        if (!_options.IsEnabled(context.Response.StatusCode, isRouting))
        {
            return Task.CompletedTask;
        }

        currentMetadata ??= context.GetEndpoint()?.Metadata;
        var writer = GetWriter(context, currentMetadata, isRouting);

        return writer != null ?
            writer.WriteAsync(context, statusCode, title, type, detail, instance, extensions) :
            Task.CompletedTask;
    }

    private IProblemDetailsWriter? GetWriter(
        HttpContext context,
        EndpointMetadataCollection? currentMetadata,
        bool isRouting)
    {
        for (var i = 0; i < _writers.Length; i++)
        {
            if (_writers[i].CanWrite(context, currentMetadata, isRouting: isRouting))
            {
                return _writers[i];
            }
        }

        return null;
    }
}
