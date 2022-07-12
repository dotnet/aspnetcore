// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class DefaultProblemDetailsWriter : IProblemDetailsWriter
{
    private static readonly MediaTypeHeaderValue _jsonMediaType = new("application/json");
    private static readonly MediaTypeHeaderValue _problemDetailsJsonMediaType = new("application/problem+json");
    private readonly ProblemDetailsOptions _options;

    public DefaultProblemDetailsWriter(IOptions<ProblemDetailsOptions> options)
    {
        _options = options.Value;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JSON serialization of ProblemDetails.Extensions might require types that cannot be statically analyzed and we need to fallback" +
        "to reflection-based. The ProblemDetailsConverter is marked as RequiresUnreferencedCode already.")]
    public async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var acceptHeader = httpContext.Request.Headers.Accept.GetList<MediaTypeHeaderValue>();

        if (acceptHeader == null ||
            !acceptHeader.Any(h => _jsonMediaType.IsSubsetOf(h) || _problemDetailsJsonMediaType.IsSubsetOf(h)))
        {
            return false;
        }

        ProblemDetailsDefaults.Apply(context.ProblemDetails, httpContext.Response.StatusCode);
        _options.CustomizeProblemDetails?.Invoke(context);

        if (context.ProblemDetails.Extensions is { Count: 0 })
        {
            // We can use the source generation in this case
            await httpContext.Response.WriteAsJsonAsync(
                context.ProblemDetails,
                ProblemDetailsJsonContext.Default.ProblemDetails,
                contentType: "application/problem+json");

            return true;
        }

        await httpContext.Response.WriteAsJsonAsync(
                        context.ProblemDetails,
                        options: null,
                        contentType: "application/problem+json");

        return true;
    }

    [JsonSerializable(typeof(ProblemDetails))]
    internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
    { }
}
