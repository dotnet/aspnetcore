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

    public bool CanWrite(HttpContext context) => true;

    public Task WriteAsync(
        HttpContext context,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        var problemResult = TypedResults.Problem(detail, instance, statusCode, title, type, extensions);
        _options.ConfigureDetails?.Invoke(context, problemResult.ProblemDetails);

        return problemResult.ExecuteAsync(context);
    }

    [JsonSerializable(typeof(ProblemDetails))]
    internal sealed partial class ProblemDetailsJsonContext : JsonSerializerContext
    { }
}
