// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class DefaultApiProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly OutputFormatterSelector _formatterSelector;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private static readonly MediaTypeCollection _problemContentTypes = new()
    {
        "application/problem+json",
        "application/problem+xml"
    };

    public DefaultApiProblemDetailsWriter(
        OutputFormatterSelector formatterSelector,
        IHttpResponseStreamWriterFactory writerFactory,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _formatterSelector = formatterSelector;
        _writerFactory = writerFactory;
        _problemDetailsFactory = problemDetailsFactory;
    }

    public bool CanWrite(HttpContext context, EndpointMetadataCollection? metadata, bool isRouting)
    {
        static bool HasMetadata(EndpointMetadataCollection? metadata)
        {
            var responseType = metadata?.GetMetadata<ProducesErrorResponseTypeAttribute>();
            var apiControllerAttribute = metadata?.GetMetadata<IApiBehaviorMetadata>();

            if (apiControllerAttribute != null && responseType?.Type == typeof(ProblemDetails))
            {
                return true;
            }
            return false;

        }

        if (isRouting)
        {
            return false;
        }

        return context.Response.StatusCode switch
        {
            >= 400 and <= 499 => HasMetadata(metadata),
            _ => false,
        };
    }

    public Task WriteAsync(
        HttpContext context,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = _problemDetailsFactory.CreateProblemDetails(context, statusCode ?? context.Response.StatusCode, title, type, detail, instance);

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        var formatterContext = new OutputFormatterWriteContext(
            context,
            _writerFactory.CreateWriter,
            typeof(ProblemDetails),
            problemDetails);

        var selectedFormatter = _formatterSelector.SelectFormatter(
            formatterContext,
            Array.Empty<IOutputFormatter>(),
            _problemContentTypes);

        if (selectedFormatter == null)
        {
            return Task.CompletedTask;
        }

        return selectedFormatter.WriteAsync(formatterContext);
    }
}
