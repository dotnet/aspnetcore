// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultProblemDetailsEndpointWriter : IProblemDetailsEndpointWriter
{
    private readonly ProblemDetailsMapper? _mapper;
    private readonly ProblemDetailsOptions _options;

    public DefaultProblemDetailsEndpointWriter(
        IOptions<ProblemDetailsOptions> options,
        ProblemDetailsMapper? mapper = null)
    {
        _mapper = mapper;
        _options = options.Value;
    }

    public async Task<bool> WriteAsync(
        HttpContext context,
        EndpointMetadataCollection? metadata = null,
        bool isRouting = false,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null,
        Action<HttpContext, ProblemDetails>? configureDetails = null)
    {
        if (_mapper == null ||
            !_mapper.CanMap(context, metadata: metadata, isRouting: isRouting))
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance
        };

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        ProblemDetailsDefaults.Apply(problemDetails, context.Response.StatusCode);

        _options.ConfigureDetails?.Invoke(context, problemDetails);
        configureDetails?.Invoke(context, problemDetails);

        await context.Response.WriteAsJsonAsync<object>(problemDetails, options: null, "application/problem+json");
        return true;
    }
}
