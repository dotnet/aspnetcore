// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Core.Infrastructure;

internal class DefaultHttpProblemDetailsFactory : IHttpProblemDetailsFactory
{
    private readonly OutputFormatterSelector _formatterSelector;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;

    public DefaultHttpProblemDetailsFactory(
        OutputFormatterSelector formatterSelector,
        IHttpResponseStreamWriterFactory writerFactory)
    {
        _formatterSelector = formatterSelector;
        _writerFactory = writerFactory;
    }

    public ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        ProblemDetailsOptions options,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance,
        };

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions.Add(extension);
            }
        }

        ProblemDetailsDefaults.Apply(httpContext, problemDetails, statusCode, options.ProblemDetailsErrorMapping);

        return problemDetails;
    }

    public Task WriteAsync(HttpContext context, ProblemDetails problemDetails)
    {
        var contentTypes = new MediaTypeCollection()
        {
            "application/problem+json",
            "application/problem+xml"
        };

        var formatterContext = new OutputFormatterWriteContext(
            context,
            _writerFactory.CreateWriter,
            typeof(ProblemDetails),
            problemDetails);

        var selectedFormatter = _formatterSelector.SelectFormatter(
            formatterContext,
            Array.Empty<IOutputFormatter>(),
            contentTypes);
        if (selectedFormatter == null)
        {
            return Task.CompletedTask;
        }

        return selectedFormatter.WriteAsync(formatterContext);
    }
}
