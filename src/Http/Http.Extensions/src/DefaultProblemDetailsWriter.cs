// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

internal sealed partial class DefaultProblemDetailsWriter : IProblemDetailsWriter
{
    private static readonly MediaTypeHeaderValue _jsonMediaType = new("application/json");
    private static readonly MediaTypeHeaderValue _problemDetailsJsonMediaType = new("application/problem+json");

    private readonly ProblemDetailsOptions _options;
    private readonly JsonSerializerOptions _serializerOptions;

    public DefaultProblemDetailsWriter(IOptions<ProblemDetailsOptions> options, IOptions<JsonOptions> jsonOptions)
    {
        _options = options.Value;
        _serializerOptions = jsonOptions.Value.SerializerOptions;
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
            // Check to see if the Accepted header values support `application/json` or `application/problem+json`
            // with  support for argument parameters. Support handling `*/*` and `application/*` as Accepts header values.
            // Application/json is a subset of */* but */* is not a subset of application/json
            if (acceptHeaderValue.IsSubsetOf(_jsonMediaType) || acceptHeaderValue.IsSubsetOf(_problemDetailsJsonMediaType) || _jsonMediaType.IsSubsetOf(acceptHeaderValue))
            {
                return true;
            }
        }

        return false;
    }

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var httpContext = context.HttpContext;
        ProblemDetailsDefaults.Apply(context.ProblemDetails, httpContext.Response.StatusCode);

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["traceId"] = traceId;

        _options.CustomizeProblemDetails?.Invoke(context);

        var problemDetailsType = context.ProblemDetails.GetType();

        return new ValueTask(httpContext.Response.WriteAsJsonAsync(
                        context.ProblemDetails,
                         _serializerOptions.GetTypeInfo(problemDetailsType),
                        contentType: "application/problem+json"));
    }
}
