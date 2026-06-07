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
        // Match the metadata check performed in WriteAsync so this writer does not
        // claim endpoints it will silently drop. Without this, ProblemDetailsService
        // selects this writer for any controller endpoint and never falls back to
        // the next-registered writer when [ApiController] (IApiBehaviorMetadata) is
        // not present on the endpoint or SuppressMapClientErrors is enabled.
        var apiControllerAttribute = context.AdditionalMetadata?.GetMetadata<IApiBehaviorMetadata>() ??
            context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IApiBehaviorMetadata>();

        return apiControllerAttribute != null && !_apiBehaviorOptions.SuppressMapClientErrors;
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

        // Preserve the runtime ProblemDetails subtype. ValidationProblemDetails carries
        // an Errors dictionary as a structural member which the factory-created
        // ProblemDetails would otherwise drop silently.
        if (context.ProblemDetails is ValidationProblemDetails validationProblemDetails)
        {
            var validationDetails = new ValidationProblemDetails(validationProblemDetails.Errors)
            {
                Status = problemDetails.Status,
                Title = problemDetails.Title,
                Type = problemDetails.Type,
                Detail = problemDetails.Detail,
                Instance = problemDetails.Instance,
            };

            foreach (var extension in problemDetails.Extensions)
            {
                validationDetails.Extensions[extension.Key] = extension.Value;
            }

            problemDetails = validationDetails;
        }

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
            problemDetails.GetType(),
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
