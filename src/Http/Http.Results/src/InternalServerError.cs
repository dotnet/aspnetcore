// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Internal Server Error (500) status code.
/// </summary>
public sealed class InternalServerError : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InternalServerError"/> class with the values
    /// provided.
    /// </summary>
    internal InternalServerError()
    {
    }

    /// <summary>
    /// Gets the HTTP status code: <see cref="StatusCodes.Status500InternalServerError"/>
    /// </summary>
    public int StatusCode => StatusCodes.Status500InternalServerError;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.InternalServerErrorObjectResult");

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status500InternalServerError, typeof(void)));
    }
}
