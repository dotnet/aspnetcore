// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class DefaultProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly ProblemDetailsOptions _options;

    public DefaultProblemDetailsWriter(IOptions<ProblemDetailsOptions> options)
    {
        _options = options.Value;
    }

    public async ValueTask<bool> WriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;

        ProblemDetailsDefaults.Apply(context.ProblemDetails, httpContext.Response.StatusCode);
        _options.ConfigureDetails?.Invoke(httpContext, context.ProblemDetails);

        await httpContext.Response.WriteAsJsonAsync(context.ProblemDetails, typeof(ProblemDetails), ProblemDetailsJsonContext.Default, contentType: "application/problem+json");
        return httpContext.Response.HasStarted;
    }

    [JsonSerializable(typeof(ProblemDetails))]
    internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
    { }
}
