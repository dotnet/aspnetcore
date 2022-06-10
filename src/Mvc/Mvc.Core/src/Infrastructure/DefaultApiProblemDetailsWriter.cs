// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal class DefaultApiProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly OutputFormatterSelector _formatterSelector;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ProblemDetailsOptions _options;

    private static readonly MediaTypeCollection _problemContentTypes = new()
    {
        "application/problem+json",
        "application/problem+xml"
    };

    public DefaultApiProblemDetailsWriter(
        OutputFormatterSelector formatterSelector,
        IHttpResponseStreamWriterFactory writerFactory,
        ProblemDetailsFactory problemDetailsFactory,
        IOptions<ProblemDetailsOptions> options)
    {
        _formatterSelector = formatterSelector;
        _writerFactory = writerFactory;
        _problemDetailsFactory = problemDetailsFactory;
        _options = options.Value;
    }

    public bool CanWrite(HttpContext context, EndpointMetadataCollection? metadata, bool isRouting)
    {
        if (isRouting || context.Response.StatusCode >= 500)
        {
            return true;
        }

        if (metadata != null)
        {
            var responseType = metadata.GetMetadata<ProducesErrorResponseTypeAttribute>();
            var apiControllerAttribute = metadata.GetMetadata<IApiBehaviorMetadata>();

            if (apiControllerAttribute != null && responseType?.Type == typeof(ProblemDetails))
            {
                return true;
            }
        }

        return false;

        //var headers = context.Request.GetTypedHeaders();
        //var acceptHeader = headers.Accept;
        //if (acceptHeader != null &&
        //    !acceptHeader.Any(h => _problemMediaType.IsSubsetOf(h)))
        //{
        //    return false;
        //}
    }

    public Task WriteAsync(
        HttpContext context,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null,
        Action<HttpContext, ProblemDetails>? configureDetails = null)
    {
        var problemDetails = _problemDetailsFactory.CreateProblemDetails(context, statusCode ?? context.Response.StatusCode, title, type, detail, instance);

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        _options.ConfigureDetails?.Invoke(context, problemDetails);
        configureDetails?.Invoke(context, problemDetails);

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
