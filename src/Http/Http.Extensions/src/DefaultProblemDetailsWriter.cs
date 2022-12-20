// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
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

    public bool CanWrite(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        var acceptHeader = httpContext.Request.Headers.Accept.GetList<MediaTypeHeaderValue>();

        // Based on https://www.rfc-editor.org/rfc/rfc7231#section-5.3.2 a request
        // without the Accept header implies that the user agent
        // will accept any media type in response
        if (acceptHeader.Count == 0)
        {
            return true;
        }

        for (var i = 0; i < acceptHeader.Count; i++)
        {
            var acceptHeaderValue = acceptHeader[i];

            if (_jsonMediaType.IsSubsetOf(acceptHeaderValue) ||
                _problemDetailsJsonMediaType.IsSubsetOf(acceptHeaderValue))
            {
                return true;
            }
        }

        return false;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "JSON serialization of ProblemDetails.Extensions might require types that cannot be statically analyzed. The property is annotated with a warning")]
    [UnconditionalSuppressMessage("Trimming", "IL3050",
        Justification = "JSON serialization of ProblemDetails.Extensions might require types that cannot be statically analyzed. The property is annotated with a warning")]
    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        ProblemDetailsDefaults.Apply(context.ProblemDetails, httpContext.Response.StatusCode);
        _options.CustomizeProblemDetails?.Invoke(context);

        // Use source generation serialization in two scenarios:
        // 1. There are no extensions. Source generation is faster and works well with trimming.
        // 2. Native AOT. In this case only the data types specified on ProblemDetailsJsonContext will work.
        if (context.ProblemDetails.Extensions is { Count: 0 } || !RuntimeFeature.IsDynamicCodeSupported)
        {
            return new ValueTask(httpContext.Response.WriteAsJsonAsync(
                context.ProblemDetails,
                ProblemDetailsJsonContext.Default.ProblemDetails,
                contentType: "application/problem+json"));
        }

        return new ValueTask(httpContext.Response.WriteAsJsonAsync(
                        context.ProblemDetails,
                        options: null,
                        contentType: "application/problem+json"));
    }

    // Additional values are specified on JsonSerializerContext to support some values for extensions.
    // For example, the DeveloperExceptionMiddleware serializes its complex type to JsonElement, which problem details then needs to serialize.
    [JsonSerializable(typeof(ProblemDetails))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(decimal))]
    [JsonSerializable(typeof(float))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(Uri))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(DateTimeOffset))]
    internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
    {
    }
}
