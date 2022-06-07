// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Core.Infrastructure;

internal class DefaultProblemDetailsWriter : IProblemDetailsEndpointWriter
{
    private readonly OutputFormatterSelector _formatterSelector;
    private readonly IHttpResponseStreamWriterFactory _writerFactory;
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ProblemDetailsOptions _options;
    private readonly ProblemDetailsMapper? _mapper;

    private static readonly MediaTypeCollection _problemContentTypes = new()
    {
        "application/problem+json",
        "application/problem+xml"
    };

    public DefaultProblemDetailsWriter(
        OutputFormatterSelector formatterSelector,
        IHttpResponseStreamWriterFactory writerFactory,
        ProblemDetailsFactory problemDetailsFactory,
        IOptions<ProblemDetailsOptions> options,
        ProblemDetailsMapper? mapper = null)
    {
        _formatterSelector = formatterSelector;
        _writerFactory = writerFactory;
        _problemDetailsFactory = problemDetailsFactory;
        _options = options.Value;
        _mapper = mapper;
    }

    public async Task<bool> WriteAsync(
        HttpContext context,
        EndpointMetadataCollection? metadata = null,
        bool isRouting = false,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null,
        Action<HttpContext, ProblemDetails>? configureDetails = null)
    {
        if (_mapper == null ||
            !_mapper.CanMap(context, metadata: metadata, isRouting: isRouting))
        {
            return false;
        }

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
            return false;
        }

        await selectedFormatter.WriteAsync(formatterContext);
        return true;
    }
}
