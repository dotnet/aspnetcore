// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

internal sealed class DefaultApiProblemDetailsWriter : IProblemDetailsWriter
{
    private readonly OutputFormatterSelector _formatterSelector;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ApiBehaviorOptions _apiBehaviorOptions;

    private static readonly MediaTypeCollection _problemContentTypes = new()
    {
        "application/problem+json",
        "application/problem+xml"
    };

    public DefaultApiProblemDetailsWriter(
        OutputFormatterSelector formatterSelector,
        IHttpResponseStreamWriterFactory writerFactory,
        ProblemDetailsFactory problemDetailsFactory,
        IOptions<ApiBehaviorOptions> apiBehaviorOptions)
    {
        _formatterSelector = formatterSelector;
        _writerFactory = writerFactory;
        _problemDetailsFactory = problemDetailsFactory;
        _apiBehaviorOptions = apiBehaviorOptions.Value;
    }

    public bool CanWrite(ProblemDetailsContext context)
    {
        var controllerAttribute = context.AdditionalMetadata?.GetMetadata<ControllerAttribute>() ??
            context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ControllerAttribute>();

        return controllerAttribute != null;
    }

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        var apiControllerAttribute = context.AdditionalMetadata?.GetMetadata<IApiBehaviorMetadata>() ??
            context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IApiBehaviorMetadata>();

        if (apiControllerAttribute is null || _apiBehaviorOptions.SuppressMapClientErrors)
        {
            // In this case we don't want to write
            return ValueTask.CompletedTask;
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
            return ValueTask.CompletedTask;
        }

        return new ValueTask(selectedFormatter.WriteAsync(formatterContext));
    }
}
