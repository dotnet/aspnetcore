// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class ProblemDetailsDefaultWriter : IProblemDetailsWriter
{
    private static readonly MediaTypeHeaderValue _jsonMediaType = new("application/json");
    private static readonly MediaTypeHeaderValue _problemDetailsJsonMediaType = new("application/problem+json");
    private readonly ProblemDetailsOptions _options;

    public ProblemDetailsDefaultWriter(IOptions<ProblemDetailsOptions> options)
    {
        _options = options.Value;
    }

    public async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var acceptHeader = httpContext.Request.GetTypedHeaders().Accept;

        if (acceptHeader == null ||
            !acceptHeader.Any(h => _jsonMediaType.IsSubsetOf(h) || _problemDetailsJsonMediaType.IsSubsetOf(h)))
        {
            return false;
        }

        ProblemDetailsDefaults.Apply(context.ProblemDetails, httpContext.Response.StatusCode);
        _options.CustomizeProblemDetails?.Invoke(context);

        await httpContext.Response.WriteAsJsonAsync(
            context.ProblemDetails,
            typeof(ProblemDetails),
            ProblemDetailsJsonContext.Default,
            contentType: "application/problem+json");

        return httpContext.Response.HasStarted;
    }

    [JsonSerializable(typeof(ProblemDetails))]
    internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
    { }
}
