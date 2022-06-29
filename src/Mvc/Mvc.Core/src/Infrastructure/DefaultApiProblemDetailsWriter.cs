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

    public async ValueTask<bool> WriteAsync(ProblemDetailsContext context)
    {
        var apiControllerAttribute = context.AdditionalMetadata?.GetMetadata<IApiBehaviorMetadata>() ??
            context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IApiBehaviorMetadata>();

        if (apiControllerAttribute is null)
        {
            return false;
        }

        // Recreating the problem details to get all customizations
        // from the factory
        var problemDetails = _problemDetailsFactory.CreateProblemDetails(
            context.HttpContext,
            context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode,
            context.ProblemDetails.Title,
            context.ProblemDetails.Type,
            context.ProblemDetails.Detail,
            context.ProblemDetails.Instance);

        if (context.ProblemDetails?.Extensions is not null)
        {
            foreach (var extension in context.ProblemDetails.Extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        var formatterContext = new OutputFormatterWriteContext(
            context.HttpContext,
            _writerFactory.CreateWriter,
            typeof(ProblemDetails),
            problemDetails);

        var selectedFormatter = _formatterSelector.SelectFormatter(
            formatterContext,
            Array.Empty<IOutputFormatter>(),
            _problemContentTypes);

        if (selectedFormatter == null)
        {
            return context.HttpContext.Response.HasStarted;
        }

        await selectedFormatter.WriteAsync(formatterContext);
        return context.HttpContext.Response.HasStarted;
    }
}
