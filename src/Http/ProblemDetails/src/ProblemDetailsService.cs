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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    public bool IsEnabled(int statusCode, bool isRouting = false)
    {
        if (_options.AllowedMapping == MappingOptions.Unspecified)
        {
            return false;
        }

        return isRouting ?
            _options.AllowedMapping.HasFlag(MappingOptions.RoutingFailures) :
            statusCode switch
            {
                >= 400 and <= 499 => _options.AllowedMapping.HasFlag(MappingOptions.ClientErrors),
                >= 500 and <= 599 => _options.AllowedMapping.HasFlag(MappingOptions.Exceptions),
                _ => false,
            };
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
        var writer = GetWriter(context, currentMetadata, isRouting);
        return writer != null ?
            writer.WriteAsync(context, statusCode, title, type, detail, instance, extensions) :
            Task.CompletedTask;
    }

    // Internal for testing
    internal IProblemDetailsWriter? GetWriter(
        HttpContext context,
        EndpointMetadataCollection? currentMetadata,
        bool isRouting)
    {
        if (!IsEnabled(context.Response.StatusCode, isRouting))
        {
            return null;
        }

        currentMetadata ??= context.GetEndpoint()?.Metadata;

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
